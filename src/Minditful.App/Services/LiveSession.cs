using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Minditful.Core.Engine;
using Minditful.Integrations;
using Minditful.Integrations.AzureDevOps;
using Minditful.Integrations.Graph;
using Minditful.Integrations.Live;

namespace Minditful.App.Services;

/// <summary>
/// Một ngày làm việc thật (Sandbox hoặc Production): đồng hồ thật, tín hiệu Windows,
/// dữ liệu Graph/Azure Boards làm mới định kỳ, hành động của Milo đi ra Teams/Outlook.
/// </summary>
internal sealed class LiveSession : IDisposable
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    private readonly WorkDayOptions _day;
    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly DispatcherTimer _refresh = new();
    private readonly DispatcherTimer _presence = new();
    private readonly HashSet<string> _pinned = [];
    private readonly SecretStore _secrets;
    private readonly PresenceWatcher? _presenceWatcher;
    private DateTime _lastPoll;
    private bool _refreshing;

    public AppEnvironment Env { get; }
    public ConnectionOptions Conn { get; }
    public MiloEngine Engine { get; }
    public MicrosoftAuth Auth { get; }
    public GraphClient? Graph { get; }
    public AzureBoardsClient? Boards { get; }
    public LiveWorkDataProvider Provider { get; }
    public LiveActionSink Sink { get; }
    public DayHistoryStore History { get; }
    public SandboxSeeder Seeder { get; }
    public WindowsActivityMonitor Monitor { get; }

    public string GraphStatus { get; private set; } = "Chưa đăng nhập";
    public string BoardsStatus { get; private set; } = "Chưa kết nối";
    public DateTime? LastRefresh { get; private set; }

    /// <summary>Có thay đổi đáng vẽ lại (dữ liệu mới, trạng thái kết nối).</summary>
    public event Action? Changed;

    public LiveSession(AppEnvironment env, MinditfulOptions opt)
    {
        Env = env;
        Conn = opt.For(env);
        _day = opt.WorkDay;
        _secrets = new SecretStore(env);
        var dir = AppPaths.For(env);

        Auth = new MicrosoftAuth(Conn.Graph, dir, Conn.Graph.UseBroker ? ConfigureBroker : null);
        Graph = Auth.IsConfigured ? new GraphClient(Auth, Http) : null;
        var ado = Conn.AzureDevOps;
        if (ado.Enabled && !ado.Organization.StartsWith('<'))
        {
            var header = ado.Auth.Equals("Entra", StringComparison.OrdinalIgnoreCase)
                ? AzureBoardsClient.EntraAuth(Auth)
                : AzureBoardsClient.PatAuth(() => _secrets.Read() ?? Environment.GetEnvironmentVariable(ado.PatEnvVar));
            Boards = new AzureBoardsClient(ado, Http, header);
        }
        History = new DayHistoryStore(Path.Combine(dir, "history.json"));
        Provider = new LiveWorkDataProvider(Conn, _day, Auth, Graph, Boards, History);
        Sink = new LiveActionSink(Conn, Auth, Graph, History, () => Engine!.Day, LogOnUi, Shell.Open);
        Seeder = new SandboxSeeder(Conn, Auth, Graph, Boards);
        if (Graph is not null && Conn.PresenceMode.Equals("Graph", StringComparison.OrdinalIgnoreCase))
            _presenceWatcher = new PresenceWatcher(Graph, Auth);

        var now = DateTime.Now;
        var cfg = new EngineConfig
        {
            Start = Tm.T(_day.Start), End = Tm.T(_day.End), FragThreshold = _day.FragmentationPerHour,
            MorningHelloUntil = Tm.T("12:00"), Scripted = false, Seed = (uint)now.Ticks,
        };
        Engine = new MiloEngine(cfg, new WorkSnapshot(), null, DateOnly.FromDateTime(now), now.TimeOfDay.TotalSeconds);
        Engine.SetAuto(false);
        Engine.ActionRequested += a => _ = Sink.HandleAsync(a);

        Monitor = new WindowsActivityMonitor(_day);
        Monitor.LockChanged += locked =>
        {
            Engine.AdvanceTo(DateTime.Now.TimeOfDay.TotalSeconds);
            Engine.SetLocked(locked);
            if (!locked) _ = RefreshAsync();
        };
        Monitor.AppSwitched += () => Engine.RecordSwitch();

        _tick.Tick += (_, _) => Tick();
        _refresh.Interval = TimeSpan.FromMinutes(Math.Max(1, _day.RefreshMinutes));
        _refresh.Tick += (_, _) => _ = RefreshAsync();
        _presence.Interval = TimeSpan.FromSeconds(Math.Max(10, _day.PresencePollSeconds));
        _presence.Tick += (_, _) => _ = PollPresenceAsync();
    }

    private static PublicClientApplicationBuilder ConfigureBroker(PublicClientApplicationBuilder b) =>
        b.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows) { Title = "Minditful" })
         .WithParentActivityOrWindow(() => Application.Current.Dispatcher.Invoke(() =>
             Application.Current.MainWindow is { } w ? new WindowInteropHelper(w).Handle : IntPtr.Zero));

    public async Task StartAsync()
    {
        // Mở app lúc máy đang mở khoá = lần mở máy đầu ngày.
        Engine.SetLocked(false);
        _tick.Start();
        _refresh.Start();
        _presence.Start();
        await SignInAsync(interactive: false);
    }

    private void LogOnUi(string text, LogKind kind) =>
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            Engine.LogExternal(text, kind);
            Changed?.Invoke();
        });

    private void Tick()
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        if (today != Engine.Day)
        {
            // Qua ngày mà chưa "Về thôi": lưu im lặng (mục 6.4 · Tắt máy ngang)
            if (Engine.S.DayStarted && !Engine.S.OffDuty) History.Save(Engine.BuildDayRecord());
            Engine.StartNewDay(today, now.TimeOfDay.TotalSeconds, Engine.Snap);
            if (!Monitor.Locked) Engine.SetLocked(false);
            _ = RefreshAsync();
        }

        if ((now - _lastPoll).TotalSeconds >= 1)
        {
            _lastPoll = now;
            Monitor.Poll();
            if (!_pinned.Contains("away")) Engine.SetAway(Monitor.Away, Monitor.Away ? _day.AwayAfterMinutes : 0);
            if (!_pinned.Contains("typing")) Engine.SetTyping(Monitor.Typing);
            if (!_pinned.Contains("fullscreen")) Engine.SetFullscreen(Monitor.Fullscreen);
        }
        Engine.AdvanceTo(now.TimeOfDay.TotalSeconds);
    }

    public async Task SignInAsync(bool interactive)
    {
        if (!Auth.IsConfigured)
        {
            GraphStatus = "Chưa cấu hình ClientId trong appsettings.json";
            await RefreshAsync();
            return;
        }
        try
        {
            await Auth.GraphTokenAsync(interactive);
            GraphStatus = $"Đã đăng nhập {Auth.UserName} · quyền: {string.Join(", ", Auth.GrantedScopes.Where(s => s is not ("openid" or "profile" or "offline_access" or "email")))}";
            await RefreshAsync();
            await PollPresenceAsync();
        }
        catch (MsalUiRequiredException)
        {
            GraphStatus = "Chưa đăng nhập — bấm \"Đăng nhập Microsoft\"";
            await RefreshAsync();
        }
        catch (Exception ex) when (ex is MsalException or HttpRequestException or InvalidOperationException)
        {
            GraphStatus = "Lỗi đăng nhập: " + LiveWorkDataProvider.Describe(ex);
            Changed?.Invoke();
        }
    }

    public async Task SignOutAsync()
    {
        if (Auth.IsConfigured) await Auth.SignOutAsync();
        GraphStatus = "Đã đăng xuất";
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (_refreshing) return;
        _refreshing = true;
        try
        {
            var snap = await Provider.LoadAsync(DateTime.Now);
            Engine.ApplySnapshot(snap);
            LastRefresh = DateTime.Now;
            BoardsStatus = Boards is null ? "Chưa cấu hình (appsettings.json)"
                : snap.BoardsAvailable ? $"{snap.Tasks.Count} work item đang làm" + (snap.Sprint is { } sp ? $" · {sp.Name} {sp.Done:0.#}/{sp.Total:0.#}" : "")
                : "Lỗi — xem dòng trạng thái";
            if (snap.StatusNote is { } note) Engine.LogExternal("Dữ liệu: " + note, LogKind.Error);
        }
        finally
        {
            _refreshing = false;
            Changed?.Invoke();
        }
    }

    private async Task PollPresenceAsync()
    {
        if (_presenceWatcher is null || !Auth.IsConfigured) return;
        try
        {
            var p = await _presenceWatcher.PollAsync();
            if (p is not { } v)
            {
                Engine.SetInCallOverride(null);
                return;
            }
            Engine.SetInCallOverride(v.InCall);
            if (!_pinned.Contains("dnd")) Engine.SetUserDnd(v.Dnd && !Sink.OwnsDnd);
        }
        catch (Exception ex) when (ex is GraphException or HttpRequestException or MsalException)
        {
            Engine.SetInCallOverride(null); // mất presence → cổng họp suy ra từ lịch (mục 14)
        }
    }

    // ---------- Sandbox: giả lập tín hiệu đè lên tín hiệu thật ----------
    public bool IsPinned(string signal) => _pinned.Contains(signal);

    public void Toggle(string signal)
    {
        var s = Engine.S;
        var on = signal switch
        {
            "away" => !s.Away, "typing" => !s.Typing, "fullscreen" => !s.Fullscreen, "dnd" => !s.UserDnd, _ => false,
        };
        switch (signal)
        {
            case "away": Engine.SetAway(on); break;
            case "typing": Engine.SetTyping(on); break;
            case "fullscreen": Engine.SetFullscreen(on); break;
            case "dnd": Engine.SetUserDnd(on); break;
        }
        if (on) _pinned.Add(signal);
        else _pinned.Remove(signal);
        Changed?.Invoke();
    }

    public void SavePat(string? pat)
    {
        _secrets.Write(pat);
        _ = RefreshAsync();
    }

    public bool HasPat => _secrets.Read() is not null || Environment.GetEnvironmentVariable(Conn.AzureDevOps.PatEnvVar) is not null;

    public void Dispose()
    {
        if (Engine.S.DayStarted && !Engine.S.OffDuty) History.Save(Engine.BuildDayRecord());
        _tick.Stop();
        _refresh.Stop();
        _presence.Stop();
        Monitor.Dispose();
    }
}
