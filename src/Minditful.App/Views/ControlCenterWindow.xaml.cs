using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Minditful.App.Services;
using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Integrations;
using Minditful.Integrations.Live;

namespace Minditful.App.Views;

/// <summary>Bảng điều khiển của Sandbox/Production: kết nối, dữ liệu, bộ não Milo; Sandbox có thêm công cụ thử.</summary>
public partial class ControlCenterWindow : Window
{
    private static readonly CaseId[] Forceable =
    [
        CaseId.MorningHello, CaseId.MeetingSoon, CaseId.CalendarPacked, CaseId.EmailWaiting, CaseId.StuckTask, CaseId.TaskDone,
        CaseId.MeetingOverload, CaseId.NoBreak, CaseId.LunchMissed, CaseId.LowRest, CaseId.HighFragmentation, CaseId.Overtime,
        CaseId.CheckIn, CaseId.EodWrapup, CaseId.EodNudge, CaseId.FocusDone, CaseId.Dashboard,
    ];

    private readonly LiveSession _session;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(400) };

    internal ControlCenterWindow(LiveSession session)
    {
        InitializeComponent();
        _session = session;
        var sandbox = session.Env == AppEnvironment.Sandbox;
        Title = $"Minditful · {(sandbox ? "Sandbox" : "Production")} · Bảng điều khiển";
        EnvEyebrow.Text = sandbox ? "MINDITFUL · MÔI TRƯỜNG SANDBOX (TÀI KHOẢN CÁ NHÂN)" : "MINDITFUL · MÔI TRƯỜNG PRODUCTION";
        EnvTitle.Text = sandbox ? "Milo chạy thật trên tài khoản cá nhân" : "Milo đang chạy với Teams + Azure Boards";
        EnvHint.Text = sandbox
            ? "Tài khoản Microsoft cá nhân không có Teams presence nên cổng Đang họp suy ra từ lịch, Không làm phiền được giả lập. Mọi thứ khác gọi API thật."
            : "Milo ở góc phải dưới màn hình. Cửa sổ này chỉ để xem trạng thái; đóng lại thì Milo vẫn chạy (biểu tượng ở khay hệ thống).";
        SandboxTools.Visibility = sandbox ? Visibility.Visible : Visibility.Collapsed;
        foreach (var c in Forceable) CaseBox.Items.Add(new ComboBoxItem { Content = Catalog.Def(c).Name, Tag = c });
        CaseBox.SelectedIndex = 0;
        _timer.Tick += (_, _) => Render();
        _session.Changed += Render;
        Loaded += (_, _) => _timer.Start();
        Closed += (_, _) =>
        {
            _timer.Stop();
            _session.Changed -= Render;
        };
        Render();
    }

    private void Render()
    {
        var e = _session.Engine;
        var s = e.S;
        Clock.Text = Tm.Hm(s.T);
        DayLabel.Text = e.Day.ToDateTime(TimeOnly.MinValue).ToString("dddd · dd/MM", new CultureInfo("vi-VN"));
        ClockState.Text = Present.ClockState(e);
        GraphStatus.Text = _session.GraphStatus;
        BoardsStatus.Text = _session.BoardsStatus;
        PatHint.Text = _session.Conn.AzureDevOps.Auth.Equals("Pat", StringComparison.OrdinalIgnoreCase)
            ? (_session.HasPat ? "Đã có PAT (lưu mã hoá trên máy này)." : $"Chưa có PAT. Nhập ở trên hoặc đặt biến môi trường {_session.Conn.AzureDevOps.PatEnvVar}.")
            : "Azure DevOps dùng chung đăng nhập Microsoft (Entra).";
        var snap = e.Snap;
        SnapSummary.Text =
            $"{snap.Calendar.Count} cuộc họp hôm nay" + (e.NextMeeting() is { } n ? $" · kế tiếp {Tm.Hm(n.Start)} {n.Subject}" : "") + "\n" +
            (snap.MailAvailable ? $"{snap.Unread} email chưa đọc · {e.WaitingEmails().Count} email đang chờ bạn trả lời" : "Email: chưa có") + "\n" +
            (snap.BoardsAvailable ? $"{e.InProgress()} task đang làm · {e.StuckTasks().Count} task kẹt (≥ 3 ngày) · baseline {snap.AvgInProgress:0.#}" : "Azure Boards: chưa có") + "\n" +
            $"Tín hiệu Windows: idle {_session.Monitor.Idle:0}s · {(s.Typing ? "đang gõ" : "không gõ")} · {(s.Fullscreen ? "toàn màn hình" : "cửa sổ thường")}" +
            (_session.LastRefresh is { } lr ? $"\nLàm mới lúc {lr:HH:mm:ss}" : "") +
            (snap.StatusNote is { } note ? "\n⚠ " + note : "");

        foreach (var b in Acts.Children.OfType<Button>())
        {
            var tag = (string)b.Tag;
            var on = tag switch
            {
                "typing" => s.Typing, "away" => s.Away, "fullscreen" => s.Fullscreen, "dnd" => s.UserDnd, "stress" => s.Stress > 0, _ => false,
            };
            b.Background = on ? (Brush)FindResource("Cream") : (Brush)FindResource("Panel2");
            b.Foreground = on ? (Brush)FindResource("Ink") : (Brush)FindResource("Text");
            if (tag == "leave") b.IsEnabled = e.InCall();
        }
        Brain.Render(e);
    }

    private async void SignIn_Click(object sender, RoutedEventArgs e) => await _session.SignInAsync(interactive: true);
    private async void SignOut_Click(object sender, RoutedEventArgs e) => await _session.SignOutAsync();
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await _session.RefreshAsync();

    private void SavePat_Click(object sender, RoutedEventArgs e)
    {
        _session.SavePat(PatBox.Password);
        PatBox.Clear();
    }

    private async void Seed_Click(object sender, RoutedEventArgs e)
    {
        SeedBtn.IsEnabled = false;
        SeedLog.Text = "Đang tạo…";
        var lines = new List<string>();
        try
        {
            await _session.Seeder.SeedAsync(DateTime.Now, new Progress<string>(l =>
            {
                lines.Add("✓ " + l);
                SeedLog.Text = string.Join("\n", lines);
            }));
            lines.Add("Xong. Đang làm mới dữ liệu…");
            SeedLog.Text = string.Join("\n", lines);
            await _session.RefreshAsync();
        }
        catch (Exception ex)
        {
            lines.Add("✗ " + LiveWorkDataProvider.Describe(ex));
            SeedLog.Text = string.Join("\n", lines);
        }
        finally
        {
            SeedBtn.IsEnabled = true;
        }
    }

    private void Act_Click(object sender, RoutedEventArgs e)
    {
        var eng = _session.Engine;
        switch ((string)((Button)sender).Tag)
        {
            case "typing" or "away" or "fullscreen" or "dnd": _session.Toggle((string)((Button)sender).Tag); break;
            case "leave": eng.LeaveMeeting(); break;
            case "frag": eng.SimulateFragmentation(); break;
            case "stress": eng.ToggleStress(); break;
            case "dash": ((App)Application.Current).Companion?.OpenDashboard(); break;
        }
        Render();
    }

    private void Force_Click(object sender, RoutedEventArgs e)
    {
        if (CaseBox.SelectedItem is ComboBoxItem { Tag: CaseId c }) _session.Engine.ForceCase(c);
        Render();
    }
}
