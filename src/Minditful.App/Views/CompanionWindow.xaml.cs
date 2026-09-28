using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using Minditful.App.Services;

namespace Minditful.App.Views;

/// <summary>Cửa sổ overlay trong suốt ở góc phải dưới, ngay trên taskbar — nơi Milo sống trên desktop thật.</summary>
public partial class CompanionWindow : Window
{
    private readonly LiveSession _session;
    private bool _animating;

    internal CompanionWindow(LiveSession session)
    {
        InitializeComponent();
        _session = session;
        Layer.Engine = session.Engine;
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
            Animate(_session.Engine.Busy);
            if (!_animating) Layer.Render();
        };
        timer.Start();
    }

    private void OnDisplayChanged(object? sender, EventArgs e) => Dispatcher.BeginInvoke(PlaceInCorner);

    private void PlaceInCorner()
    {
        var wa = SystemParameters.WorkArea;
        Left = wa.Right - Width;
        Top = wa.Bottom - Height;
    }

    private void ApplyWindowStyles()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        // Không hiện trong Alt+Tab
        var ex = (long)Native.GetWindowLongPtr(hwnd, Native.GWL_EXSTYLE);
        ex = (ex | Native.WS_EX_TOOLWINDOW) & ~Native.WS_EX_APPWINDOW;
        Native.SetWindowLongPtr(hwnd, Native.GWL_EXSTYLE, (IntPtr)ex);
        // Share màn hình không thấy Milo hay chấm chờ (mục 3)
        if (_session.Conn.ContentProtection) Native.SetWindowDisplayAffinity(hwnd, Native.WDA_EXCLUDEFROMCAPTURE);
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
        _session.Engine.AdvanceTo(DateTime.Now.TimeOfDay.TotalSeconds);
        Layer.Render();
    }

    public void OpenDashboard()
    {
        _session.Engine.TailClick();
        Animate(true);
        Layer.Render();
    }
}
