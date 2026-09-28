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
using Minditful.Integrations.Llm;

namespace Minditful.App.Services;

/// <summary>
/// Một ngày làm việc thật (Sandbox hoặc Production): đồng hồ thật, tín hiệu Windows,
/// dữ liệu Graph/Azure Boards làm mới định kỳ, hành động của Milo đi ra Teams/Outlook.
/// </summary>
internal sealed class LiveSession : IMiloSession
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    private readonly WorkDayOptions _day;
    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly DispatcherTimer _refresh = new();
    private readonly DispatcherTimer _presence = new();
    private readonly HashSet<string> _pinned = [];
    private readonly SecretStore _secrets;
    private readonly SecretStore _claudeKey;
    private readonly PresenceWatcher? _presenceWatcher;
    private DateTime _lastPoll;
    private string? _lastNote;
    private DateTime? _offlineSince;
    private DateTime? _offlineLogged;
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
    public ClaudeLineWriter Writer { get; }
    public OutcomeStore Outcomes { get; }
    public LlmOptions Llm { get; }

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
        var ov = Conn.BehaviorOverrides;
        if (ov?.EmailMinBusinessDaysWaiting is { } mailDays) Conn.MailMinWaitDays = mailDays;
        _secrets = new SecretStore(env);
        _claudeKey = new SecretStore(env, "claude-api-key");
        Llm = opt.Llm;
        Writer = new ClaudeLineWriter(opt.Llm, () => _claudeKey.Read() ?? Environment.GetEnvironmentVariable(opt.Llm.ApiKeyEnvVar));
        var dir = AppPaths.For(env);
        Outcomes = new OutcomeStore(Path.Combine(dir, "outcomes.tsv"));

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
        var std = new EngineConfig();
        var cfg = new EngineConfig
        {
            Start = Tm.T(_day.Start), End = Tm.T(_day.End), FragThreshold = _day.FragmentationPerHour,
            MorningHelloUntil = Tm.T("12:00"), Scripted = false, Seed = (uint)now.Ticks,
            // Sandbox: ngưỡng rút gọn (docs/KET-NOI-SANDBOX.md mục 3.2); Prod không có khối này → ngưỡng chuẩn
            StuckMinDays = ov?.StuckTaskMinBusinessDays ?? std.StuckMinDays,
            EmailNotBefore = ov?.EmailNotBefore is { } nb ? Tm.T(nb) : std.EmailNotBefore,
            NoBreakMin = ov?.NoBreakStreakMin ?? std.NoBreakMin,
            OverloadMinChain = ov?.OverloadMinChainCount ?? std.OverloadMinChain,
            GapBudget = ov?.BudgetGapMin is { } gap ? gap * 60 : std.GapBudget,
            VisitMinMinutes = ov?.VisitEveryMin is [var vmin, _] ? vmin : std.VisitMinMinutes,
            VisitMaxMinutes = ov?.VisitEveryMin is [_, var vmax] ? vmax : std.VisitMaxMinutes,
            ParkTtl = ov?.ParkedReminderTtlMin is { } park ? park * 60 : std.ParkTtl,
        };
        Engine = new MiloEngine(cfg, new WorkSnapshot(), null, DateOnly.FromDateTime(now), now.TimeOfDay.TotalSeconds);
        Engine.SetAuto(false);
        Engine.ActionRequested += a => _ = Sink.HandleAsync(a);
        Engine.OutcomeRecorded += (c, o) => Outcomes.Append(new OutcomeEvent(Engine.Day, Engine.S.T, c, o, Engine.S.Score));
        LlmBridge.Attach(Engine, Writer, Application.Current.Dispatcher, opt.Llm.Features);
        LlmBridge.ApplyModes(Engine, Writer, opt.Llm);
        ApplyTuning();
        if (ov is not null) Engine.LogExternal("Ngưỡng rút gọn cho Sandbox: " + ov.Describe(), LogKind.Sig);

        Monitor = new WindowsActivityMonitor(_day);
        Monitor.LockChanged += locked =>
        {
            Engine.AdvanceTo(DateTime.Now.TimeOfDay.TotalSeconds);
            Engine.SetLocked(locked);
            if (!locked) _ = RefreshAsync();
        };
        Monitor.AppSwitched += () => Engine.RecordSwitch();

        _tick.Tick += (_, _) => Tick();
        // Mỗi 30 giây hỏi provider; provider tự quyết nguồn nào đã tới hạn đọc lại (lịch 2', mail 5', Boards 3')
        _refresh.Interval = TimeSpan.FromSeconds(30);
        _refresh.Tick += (_, _) => _ = RefreshAsync(force: false);
        _presence.Interval = TimeSpan.FromSeconds(Math.Max(10, Conn.Polling.PresenceSeconds));
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
        // Lần đầu trên máy này (chưa có tài khoản trong cache): mở trình duyệt đăng nhập + màn hình xin quyền.
        // Các lần sau lấy token im lặng; hết hạn thì chỉ báo trong dashboard, không tự bật trình duyệt giữa giờ làm.
        await SignInAsync(interactive: !await Auth.HasCachedAccountAsync());
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
            ApplyTuning();
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

    /// <summary>Luật cá nhân hoá 7 ngày (§14) tính lại mỗi đầu ngày từ log phản hồi local.</summary>
    private void ApplyTuning()
    {
        Outcomes.Trim(Engine.Day);
        Engine.Tuning = Personalizer.Compute(Outcomes.Load(), Engine.Day);
        Engine.LogExternal("Cá nhân hoá 7 ngày: " + Personalizer.Describe(Engine.Tuning), LogKind.Sig);
    }

    public string LlmStatus =>
        (!Llm.Enabled ? "Đã tắt (Llm.Enabled = false)."
            : !Writer.Available ? $"Chưa có API key (nhập bên dưới hoặc đặt {Llm.ApiKeyEnvVar} trong .env)."
            : $"Đang dùng {Llm.Model} · effort {Llm.Effort}.")
        + "\n" + LlmBridge.Describe(Engine, Llm, Llm.Enabled && Writer.Available)
        + (Writer.LastError is { } e ? "\nLần gọi gần nhất: " + e : "");

    public void SaveClaudeKey(string? key)
    {
        _claudeKey.Write(key);
        LlmBridge.ApplyModes(Engine, Writer, Llm); // có key rồi thì bật ngay các chế độ Claude đã cấu hình
        Changed?.Invoke();
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
            GraphStatus = $"Đã đăng nhập {Auth.UserName} · quyền: {string.Join(", ", Auth.GrantedScopes.Where(s => s is not ("openid" or "profile" or "offline_access" or "email")))}"
                + (Auth.MissingScopes.Count > 0 ? $" · THIẾU: {string.Join(", ", Auth.MissingScopes)} (tính năng liên quan tự tắt; cần admin consent)" : "");
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

    /// <param name="force">Đọc lại mọi nguồn ngay (mở khoá, đăng nhập, bấm Làm mới); false = chỉ nguồn đã tới chu kỳ.</param>
    public async Task RefreshAsync(bool force = true)
    {
        if (_refreshing) return;
        _refreshing = true;
        try
        {
            var snap = await Provider.LoadAsync(DateTime.Now, force);
            Engine.ApplySnapshot(snap);
            LastRefresh = DateTime.Now;
            BoardsStatus = Boards is null ? "Chưa cấu hình (appsettings.json)"
                : snap.BoardsAvailable ? $"{snap.Tasks.Count} work item đang làm" + (snap.Sprint is { } sp ? $" · {sp.Name} {sp.Done:0.#}/{sp.Total:0.#}" : "")
                : "Lỗi — xem dòng trạng thái";
            if (snap.StatusNote is { } note && note != _lastNote) Engine.LogExternal("Dữ liệu: " + note, LogKind.Error);
            _lastNote = snap.StatusNote;
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
            Engine.SetInCallOverride(p.InCall);
            if (p.InCall is null) NoteOffline(p.Activity);
            else
            {
                _offlineSince = null;
                if (!_pinned.Contains("dnd")) Engine.SetUserDnd(p.Dnd && !Sink.OwnsDnd);
            }
        }
        catch (Exception ex) when (ex is GraphException or HttpRequestException or MsalException)
        {
            Engine.SetInCallOverride(null); // mất presence → cổng họp suy ra từ lịch (mục 14)
            NoteOffline("lỗi");
        }
    }

    /// <summary>Presence Offline suốt 10 phút trong giờ làm → ghi 1 dòng log (docs/KET-NOI-SANDBOX.md mục 5.2).</summary>
    private void NoteOffline(string activity)
    {
        var now = DateTime.Now;
        _offlineSince ??= now;
        var t = now.TimeOfDay.TotalSeconds;
        if ((now - _offlineSince.Value).TotalMinutes < 10 || t < Engine.Cfg.Start || t > Engine.Cfg.End || _offlineLogged == now.Date) return;
        _offlineLogged = now.Date;
        Engine.LogExternal($"Teams chưa đăng nhập (presence: {(activity.Length > 0 ? activity : "không có")}), đang dùng lịch để đoán cuộc họp", LogKind.Sig);
    }

    // ---------- công cụ test (bảng điều khiển) ----------
    /// <summary>Bắt đầu lại ngày như vừa mở máy lần đầu → chạy lại Chào sáng (checklist #1).</summary>
    public void ResetDay()
    {
        var now = DateTime.Now;
        Engine.StartNewDay(DateOnly.FromDateTime(now), now.TimeOfDay.TotalSeconds, Engine.Snap);
        ApplyTuning();
        Engine.SetLocked(false);
        Engine.LogExternal("Reset ngày → như vừa mở máy lần đầu", LogKind.User);
        _ = RefreshAsync();
        Changed?.Invoke();
    }

    /// <summary>Đổi giờ kết thúc khung làm việc lúc đang chạy (checklist #12: bây giờ + 2 phút để test Tan tầm).</summary>
    public bool SetWorkEnd(string hhmm)
    {
        if (!TimeOnly.TryParse(hhmm, out var t)) return false;
        Engine.Cfg.End = t.ToTimeSpan().TotalSeconds;
        Engine.LogExternal($"Giờ kết thúc khung làm việc → {Tm.Hm(Engine.Cfg.End)}", LogKind.User);
        Changed?.Invoke();
        return true;
    }

    public void ClearPat()
    {
        _secrets.Write(null);
        Engine.LogExternal("Đã xoá PAT đã lưu trên máy", LogKind.User);
        _ = RefreshAsync();
    }

    public string? OverridesText => Conn.BehaviorOverrides?.Describe();

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

    public bool ContentProtection => Conn.ContentProtection;

    public void Pump(double realDt) => Engine.AdvanceTo(DateTime.Now.TimeOfDay.TotalSeconds);

    public void Dispose()
    {
        if (Engine.S.DayStarted && !Engine.S.OffDuty) History.Save(Engine.BuildDayRecord());
        _tick.Stop();
        _refresh.Stop();
        _presence.Stop();
        Monitor.Dispose();
    }
}
