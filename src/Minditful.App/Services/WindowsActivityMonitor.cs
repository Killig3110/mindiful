using System.Diagnostics;
using Microsoft.Win32;
using Minditful.Integrations;

namespace Minditful.App.Services;

/// <summary>
/// Tín hiệu từ Windows (mục 2): khoá máy, idle/rời máy, đang gõ, toàn màn hình, chuyển app.
/// Không đọc nội dung phím hay tiêu đề cửa sổ — chỉ thời điểm và tên tiến trình.
/// </summary>
internal sealed class WindowsActivityMonitor : IDisposable
{
    private readonly WorkDayOptions _opt;
    private readonly Native.WinEventDelegate _hookProc;
    private readonly IntPtr _hook;
    private readonly int _ownPid = Environment.ProcessId;
    private uint _lastPid;
    private DateTime _lastSwitch = DateTime.MinValue;
    private int _activeRun;
    private bool _resumePending;

    public bool Locked { get; private set; }
    public bool Away { get; private set; }
    public bool Typing { get; private set; }
    public bool Fullscreen { get; private set; }
    public double Idle { get; private set; }

    public event Action<bool>? LockChanged;
    /// <summary>Người dùng đổi sang một app khác (tính "Chuyển việc/giờ").</summary>
    public event Action? AppSwitched;

    private static readonly HashSet<string> ShellProcesses = new(StringComparer.OrdinalIgnoreCase)
        { "explorer", "ShellExperienceHost", "StartMenuExperienceHost", "SearchHost", "LockApp", "TextInputHost", "ApplicationFrameHost" };

    public WindowsActivityMonitor(WorkDayOptions opt)
    {
        _opt = opt;
        SystemEvents.SessionSwitch += OnSessionSwitch;
        SystemEvents.PowerModeChanged += OnPower;
        _hookProc = OnForeground;
        _hook = Native.SetWinEventHook(Native.EVENT_SYSTEM_FOREGROUND, Native.EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _hookProc, 0, 0, Native.WINEVENT_OUTOFCONTEXT);
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        switch (e.Reason)
        {
            case SessionSwitchReason.SessionLock or SessionSwitchReason.ConsoleDisconnect or SessionSwitchReason.RemoteDisconnect:
                SetLocked(true);
                break;
            case SessionSwitchReason.SessionUnlock or SessionSwitchReason.ConsoleConnect or SessionSwitchReason.RemoteConnect or SessionSwitchReason.SessionLogon:
                _resumePending = false;
                SetLocked(false);
                break;
        }
    }

    private void OnPower(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend) SetLocked(true);
        else if (e.Mode == PowerModes.Resume) _resumePending = true; // mở lại khi có thao tác (nếu máy không đặt khoá khi ngủ)
    }

    private void SetLocked(bool v)
    {
        if (Locked == v) return;
        Locked = v;
        LockChanged?.Invoke(v);
    }

    private void OnForeground(IntPtr hook, uint type, IntPtr hwnd, int obj, int child, uint thread, uint time)
    {
        if (hwnd == IntPtr.Zero) return;
        Native.GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0 || pid == _ownPid || pid == _lastPid) return;
        string name;
        try { name = Process.GetProcessById((int)pid).ProcessName; }
        catch (ArgumentException) { return; }
        if (ShellProcesses.Contains(name)) return;
        _lastPid = pid;
        // chống đếm trùng khi Alt+Tab lướt qua nhiều cửa sổ
        if ((DateTime.Now - _lastSwitch).TotalSeconds < 2) return;
        _lastSwitch = DateTime.Now;
        AppSwitched?.Invoke();
    }

    /// <summary>Gọi mỗi giây từ vòng lặp chính.</summary>
    public void Poll()
    {
        Idle = Native.IdleSeconds();
        if (_resumePending && Idle < 3)
        {
            _resumePending = false;
            SetLocked(false);
        }

        // Đang gõ: idle < 3s liên tục đủ lâu (spec: getSystemIdleTime < 3s)
        if (Idle < _opt.TypingIdleSeconds) _activeRun++;
        else _activeRun = 0;
        Typing = !Locked && _activeRun >= _opt.TypingSustainSeconds;

        Away = !Locked && Idle >= _opt.AwayAfterMinutes * 60;
        Fullscreen = !Locked && DetectFullscreen();
    }

    private static bool DetectFullscreen()
    {
        // 2 = QUNS_BUSY (app toàn màn hình), 3 = D3D toàn màn hình, 4 = chế độ trình chiếu
        if (Native.SHQueryUserNotificationState(out var st) == 0 && st is 2 or 3 or 4) return true;
        var fg = Native.GetForegroundWindow();
        if (fg == IntPtr.Zero || fg == Native.GetShellWindow()) return false;
        var cls = Native.ClassName(fg);
        if (cls is "Progman" or "WorkerW" or "Shell_TrayWnd") return false;
        Native.GetWindowThreadProcessId(fg, out var pid);
        if (pid == Environment.ProcessId) return false;
        if (!Native.GetWindowRect(fg, out var r)) return false;
        var mon = Native.MonitorFromWindow(fg, Native.MONITOR_DEFAULTTONEAREST);
        var mi = new Native.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<Native.MONITORINFO>() };
        if (!Native.GetMonitorInfo(mon, ref mi)) return false;
        var m = mi.rcMonitor;
        return r.Left <= m.Left && r.Top <= m.Top && r.Right >= m.Right && r.Bottom >= m.Bottom;
    }

    public void Dispose()
    {
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        SystemEvents.PowerModeChanged -= OnPower;
        if (_hook != IntPtr.Zero) Native.UnhookWinEvent(_hook);
    }
}
