using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Minditful.App.Services;
using Minditful.App.Views;
using Minditful.Integrations;
using WinForms = System.Windows.Forms;

namespace Minditful.App;

public partial class App : Application
{
    private IMiloSession? _session;
    private WinForms.NotifyIcon? _tray;
    private Window? _control;
    private Mutex? _single;

    internal CompanionWindow? Companion { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnCrash;

        var opt = LoadOptions(e.Args);
        var env = ResolveEnvironment(opt);
        if (env is null)
        {
            Shutdown();
            return;
        }

        _single = new Mutex(true, $"Minditful.{env}", out var first);
        if (!first)
        {
            MessageBox.Show($"Milo ({env}) đang chạy rồi — xem biểu tượng ở khay hệ thống.", "Minditful");
            Shutdown();
            return;
        }

        Rendering.MiloSkin.Prewarm(Dispatcher);
        if (env == AppEnvironment.Demo) StartDemo(opt);
        else _ = StartLiveAsync(env.Value, opt);
    }

    private static MinditfulOptions LoadOptions(string[] args)
    {
        var baseDir = AppContext.BaseDirectory;
        // .env (không commit) → biến môi trường, trước khi đọc cấu hình. Mẫu: .env.sample ở gốc repo.
        DotEnv.Load(DotEnv.DefaultCandidates(baseDir, AppPaths.Root));
        var cfg = new ConfigurationBuilder()
            .SetBasePath(baseDir)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.local.json", optional: true) // bí mật/ClientId riêng, không commit
            .AddJsonFile(Path.Combine(AppPaths.Root, "appsettings.json"), optional: true)
            .AddEnvironmentVariables("MINDITFUL__")
            // Thứ tự chọn môi trường: --env > MINDITFUL_ENV > Minditful:Environment trong appsettings/.env
            .AddInMemoryCollection(Environment.GetEnvironmentVariable("MINDITFUL_ENV") is { Length: > 0 } envVar
                ? new Dictionary<string, string?> { ["Minditful:Environment"] = envVar }
                : [])
            .AddCommandLine(args, new Dictionary<string, string> { ["--env"] = "Minditful:Environment" })
            .Build();
        return cfg.GetSection("Minditful").Get<MinditfulOptions>() ?? new MinditfulOptions();
    }

    private static AppEnvironment? ResolveEnvironment(MinditfulOptions opt)
    {
        if (AppEnvironments.Parse(opt.Environment) is { } fromConfig) return fromConfig;
        var shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        if (!shift && File.Exists(AppPaths.RememberedEnvFile)
            && Enum.TryParse<AppEnvironment>(File.ReadAllText(AppPaths.RememberedEnvFile).Trim(), out var remembered))
            return remembered;

        var launcher = new LauncherWindow(opt);
        if (launcher.ShowDialog() != true || launcher.Choice is not { } env) return null;
        Directory.CreateDirectory(AppPaths.Root);
        if (launcher.RememberChoice) File.WriteAllText(AppPaths.RememberedEnvFile, env.ToString());
        else if (File.Exists(AppPaths.RememberedEnvFile)) File.Delete(AppPaths.RememberedEnvFile);
        return env;
    }

    /// <summary>
    /// Demo cũng là app thật: Milo trên desktop + khay hệ thống, chỉ khác đồng hồ/dữ liệu chạy theo kịch bản ngày mẫu.
    /// Bảng điều khiển kịch bản mở sẵn để nhảy mốc và bật từng case.
    /// </summary>
    private void StartDemo(MinditfulOptions opt)
    {
        _session = new DemoSession(opt);
        ShowMilo();
        CreateTray(AppEnvironment.Demo);
        ShowControlCenter();
    }

    private async Task StartLiveAsync(AppEnvironment env, MinditfulOptions opt)
    {
        var live = new LiveSession(env, opt);
        _session = live;
        ShowMilo();
        CreateTray(env);
        // Sandbox ở chế độ test mở sẵn bảng điều khiển như Demo; chạy như Production thì chỉ có Milo ở góc màn hình
        if (live.Conn.ShowControlCenter || live.TestMode || !live.Auth.IsConfigured) ShowControlCenter();
        await live.StartAsync();
    }

    private void ShowMilo()
    {
        Companion = new CompanionWindow(_session!);
        MainWindow = Companion;
        Companion.Show();
    }

    private void ShowControlCenter()
    {
        if (_session is null) return;
        if (_control is { IsLoaded: true })
        {
            _control.Activate();
            return;
        }
        _control = _session switch
        {
            DemoSession demo => new DemoControlWindow(demo),
            LiveSession live => new ControlCenterWindow(live),
            _ => null,
        };
        _control?.Show();
    }

    private void CreateTray(AppEnvironment env)
    {
        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Mở dashboard của Milo", null, (_, _) => Companion?.OpenDashboard());
        if (_session is DemoSession demo)
        {
            menu.Items.Add("Mở bảng điều khiển", null, (_, _) => ShowControlCenter());
            menu.Items.Add("Phát / tạm dừng ngày mẫu", null, (_, _) => demo.TogglePlay());
            menu.Items.Add("Làm lại ngày mẫu từ 08:50", null, (_, _) =>
            {
                demo.Restart();
                Companion?.Refresh();
            });
        }
        else if (_session is LiveSession live)
        {
            menu.Items.Add("Mở bảng điều khiển", null, (_, _) => ShowControlCenter());
            if (live.CanTest)
            {
                var test = new WinForms.ToolStripMenuItem("Chế độ test (ép Milo làm như Demo)") { CheckOnClick = true, Checked = live.TestMode };
                test.CheckedChanged += (_, _) =>
                {
                    if (test.Checked == live.TestMode) return; // đồng bộ lúc mở menu, không phải người dùng bấm
                    live.SetTestMode(test.Checked);
                    if (test.Checked) ShowControlCenter();
                    Companion?.Refresh();
                };
                menu.Opening += (_, _) => test.Checked = live.TestMode;
                menu.Items.Add(test);
            }
            menu.Items.Add("Đăng nhập Microsoft", null, async (_, _) => await live.SignInAsync(true));
            menu.Items.Add("Làm mới dữ liệu", null, async (_, _) => await live.RefreshAsync());
        }
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Thoát Milo", null, (_, _) => Shutdown());
        _tray = new WinForms.NotifyIcon
        {
            Text = $"Milo · Minditful ({env})",
            Icon = TailIcon(),
            ContextMenuStrip = menu,
            Visible = true,
        };
        _tray.DoubleClick += (_, _) => ShowControlCenter();
    }

    /// <summary>Icon khay vẽ từ chóp đuôi Milo (không cần file .ico).</summary>
    private static System.Drawing.Icon TailIcon()
    {
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(32 / 34.0, 32 / 34.0));
            var pen = new Pen(new SolidColorBrush(Color.FromRgb(0x45, 0x23, 0x1F)), 2);
            dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(0xE8, 0x77, 0x2E)), pen,
                Geometry.Parse("M6 34 C4 24 8 14 16 9 C20 6 24 4 27 5 C24 8 23 11 24 14 C26 11 29 10 31 11 C27 15 24 22 24 34 Z"));
            dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(0xFF, 0xF7, 0xEC)), pen,
                Geometry.Parse("M16 9 C20 6 24 4 27 5 C24 8 23 11 24 14 C26 11 29 10 31 11 C28 14 26 17 25 20 C22 17 18 13 16 9 Z"));
            dc.Pop();
        }
        var rtb = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(dv);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(rtb));
        using var ms = new MemoryStream();
        enc.Save(ms);
        ms.Position = 0;
        using var bmp = new System.Drawing.Bitmap(ms);
        return System.Drawing.Icon.FromHandle(bmp.GetHicon());
    }

    private void OnCrash(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var log = Path.Combine(AppPaths.Root, "crash.log");
        Directory.CreateDirectory(AppPaths.Root);
        File.AppendAllText(log, $"[{DateTime.Now:O}] {e.Exception}\n\n");
        _session?.Engine.LogExternal("Lỗi: " + e.Exception.Message, Core.Engine.LogKind.Error);
        e.Handled = true; // Milo không được làm sập máy người dùng; lỗi ghi vào crash.log
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _session?.Dispose();
        // File đính kèm tải về cho nút "Mở slide" là dữ liệu công việc → không để lại trên máy
        try { Directory.Delete(Path.Combine(Path.GetTempPath(), "Minditful"), recursive: true); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        if (_tray is not null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }
        _single?.Dispose();
        base.OnExit(e);
    }
}
