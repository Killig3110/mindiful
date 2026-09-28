using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using Minditful.App.Services;

namespace Minditful.App.Views;

/// <summary>
/// Cửa sổ overlay trong suốt ở góc màn hình, ngay trên taskbar — nơi Milo sống trên desktop thật.
/// Dùng chung cho cả 3 môi trường; Demo chỉ khác ở chỗ thời gian và dữ liệu đến từ kịch bản.
/// </summary>
public partial class CompanionWindow : Window
{
    private readonly IMiloSession _session;
    private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
    private double _lastPump;
    private bool _animating;
    private Rect? _area;

    internal CompanionWindow(IMiloSession session)
    {
        InitializeComponent();
        _session = session;
        Layer.Engine = session.Engine;
        Layer.Corner = UiSettings.LoadCorner(session.Env);
        Layer.TailDropped += OnTailDropped;
        Layer.Interacted += () => Animate(true);
        SourceInitialized += (_, _) => ApplyWindowStyles();
        Loaded += (_, _) => PlaceInCorner();
        SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
        Closed += (_, _) =>
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
            CompositionTarget.Rendering -= OnFrame;
        };
        KeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape) return;
            _session.Engine.Escape();
            Layer.Render();
        };
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        timer.Tick += (_, _) =>
        {
            Pump();
            Animate(_session.Engine.Busy);
            if (!_animating) Layer.Render();
        };
        timer.Start();
    }

    private void OnDisplayChanged(object? sender, EventArgs e) => Dispatcher.BeginInvoke(() =>
    {
        _area = null; // cấu hình màn hình đổi → về màn hình chính
        PlaceInCorner();
    });

    private void PlaceInCorner()
    {
        var wa = _area ?? SystemParameters.WorkArea;
        var left = Layer.Corner is Corner.BottomLeft or Corner.TopLeft;
        var top = Layer.Corner is Corner.TopRight or Corner.TopLeft;
        Left = left ? wa.Left : wa.Right - Width;
        Top = top ? wa.Top : wa.Bottom - Height;
    }

    /// <summary>Thả chóp đuôi ở đâu thì neo vào góc gần nhất của màn hình đó (§9.1).</summary>
    private void OnTailDropped(Point screenPx)
    {
        var scr = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)screenPx.X, (int)screenPx.Y));
        var wa = scr.WorkingArea;
        var corner = (screenPx.X < wa.Left + wa.Width / 2.0, screenPx.Y < wa.Top + wa.Height / 2.0) switch
        {
            (true, true) => Corner.TopLeft,
            (false, true) => Corner.TopRight,
            (true, false) => Corner.BottomLeft,
            _ => Corner.BottomRight,
        };
        if (PresentationSource.FromVisual(this)?.CompositionTarget is { } ct)
        {
            var m = ct.TransformFromDevice;
            _area = new Rect(m.Transform(new Point(wa.Left, wa.Top)), m.Transform(new Point(wa.Right, wa.Bottom)));
        }
        SetCorner(corner);
    }

    internal void SetCorner(Corner corner)
    {
        Layer.Corner = corner;
        UiSettings.SaveCorner(_session.Env, corner);
        PlaceInCorner();
        _session.Engine.LogExternal("Đổi góc neo của Milo → " + corner switch
        {
            Corner.TopLeft => "trên trái", Corner.TopRight => "trên phải", Corner.BottomLeft => "dưới trái", _ => "dưới phải",
        }, Core.Engine.LogKind.User);
    }

    private void ApplyWindowStyles()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        // Không hiện trong Alt+Tab
        var ex = (long)Native.GetWindowLongPtr(hwnd, Native.GWL_EXSTYLE);
        ex = (ex | Native.WS_EX_TOOLWINDOW) & ~Native.WS_EX_APPWINDOW;
        Native.SetWindowLongPtr(hwnd, Native.GWL_EXSTYLE, (IntPtr)ex);
        // Share màn hình không thấy Milo hay chấm chờ (mục 3)
        if (_session.ContentProtection) Native.SetWindowDisplayAffinity(hwnd, Native.WDA_EXCLUDEFROMCAPTURE);
    }

    /// <summary>Chỉ vẽ 60fps khi Milo đang hiện; lúc ẩn chỉ cập nhật 4 lần/giây cho nhẹ máy.</summary>
    private void Animate(bool on)
    {
        if (on == _animating) return;
        _animating = on;
        if (on) CompositionTarget.Rendering += OnFrame;
        else CompositionTarget.Rendering -= OnFrame;
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        Pump();
        Layer.Render();
    }

    /// <summary>Đẩy thời gian của phiên theo giây thật đã trôi kể từ lần trước (timer và khung hình dùng chung).</summary>
    private void Pump()
    {
        var now = _clock.Elapsed.TotalSeconds;
        var dt = Math.Min(0.5, now - _lastPump);
        _lastPump = now;
        if (dt > 0) _session.Pump(dt);
    }

    /// <summary>Vẽ lại ngay (sau khi bảng điều khiển đổi trạng thái).</summary>
    public void Refresh()
    {
        Animate(_session.Engine.Busy);
        Layer.Render();
    }

    public void OpenDashboard()
    {
        _session.Engine.TailClick();
        Animate(true);
        Layer.Render();
    }
}
