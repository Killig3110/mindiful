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
        CaseId.FocusPlan, CaseId.WeekReport, CaseId.MicroBreak,
    ];

    private readonly LiveSession _session;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(400) };

    internal ControlCenterWindow(LiveSession session)
    {
        InitializeComponent();
        _session = session;
        var sandbox = session.Env == AppEnvironment.Sandbox;
        Title = $"Minditful · {(sandbox ? "Sandbox" : "Production")} · Bảng điều khiển";
        EnvEyebrow.Text = sandbox ? "MINDITFUL · MÔI TRƯỜNG SANDBOX" : "MINDITFUL · MÔI TRƯỜNG PRODUCTION";
        EnvTitle.Text = sandbox ? "Milo chạy với tenant sandbox (Teams + Outlook + Azure Boards thật)" : "Milo đang chạy với Teams + Azure Boards";
        EnvHint.Text = (sandbox
            ? "Dữ liệu thật của tenant sandbox, ngưỡng hành vi được rút gọn để test trong 1 buổi. Hướng dẫn và checklist: docs/KET-NOI-SANDBOX.md. "
            : "Ngưỡng hành vi chuẩn theo tài liệu. ")
            + "Milo ở góc màn hình; đóng cửa sổ này thì Milo vẫn chạy (biểu tượng ở khay hệ thống).";
        WorkEndBox.Text = Tm.Hm(session.Engine.Cfg.End);
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
        OverridesText.Text = _session.OverridesText is { } ov ? "Ngưỡng rút gọn: " + ov : "Ngưỡng chuẩn theo tài liệu.";
        LlmStatus.Text = _session.LlmStatus;
        TuningText.Text = Personalizer.Describe(e.Tuning);
        StorageText.Text = "Chỉ lưu số liệu, không lưu tiêu đề email/họp/task. " + _session.StorageText;
        BoardsStatus.Text = _session.BoardsStatus;
        PatHint.Text = _session.Conn.AzureDevOps.Auth.Equals("Pat", StringComparison.OrdinalIgnoreCase)
            ? (_session.HasPat ? "Đã có PAT (lưu mã hoá trên máy này)." : $"Chưa có PAT. Nhập ở trên hoặc đặt biến môi trường {_session.Conn.AzureDevOps.PatEnvVar}.")
            : "Azure DevOps dùng chung đăng nhập Microsoft (Entra).";
        var snap = e.Snap;
        SnapSummary.Text = _session.WorkHoursText + "\n" +
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
                "typing" => s.Typing, "away" => s.Away, "fullscreen" => s.Fullscreen, "dnd" => s.UserDnd, "stress" => s.Stress > 0,
                "presenting" => s.Presenting, _ => false,
            };
            b.Background = on ? (Brush)FindResource("Cream") : (Brush)FindResource("Panel2");
            b.Foreground = on ? (Brush)FindResource("Ink") : (Brush)FindResource("Text");
            if (tag == "leave") b.IsEnabled = e.InCall();
        }
        var wb = _session.Wellbeing;
        WellbeingText.Text =
            $"Giữ giờ tập trung: {(wb.FocusPlan ? $"bật (khoảng trống ≥ {wb.FocusPlanMinMinutes}')" : "tắt")} · " +
            $"Báo cáo tuần sáng thứ Hai: {(wb.WeekReport ? "bật" : "tắt")} · " +
            $"Uống nước / 20-20-20: {(wb.MicroBreakEveryMinutes > 0 ? $"mỗi {wb.MicroBreakEveryMinutes}' ngồi máy, tối đa {wb.MicroBreakMaxPerDay} lần/ngày (hôm nay {s.MicroCount})" : "tắt")} · " +
            $"Hỏi \"Hôm nay thấy sao?\" + nghỉ giữa chuỗi họp ngày mai: {(wb.EveningCheck ? "bật" : "tắt")} · " +
            $"Trốn khi trình chiếu: {(wb.HideWhenPresenting ? "bật" : "tắt")}" + (s.Presenting ? " (đang trốn)" : "");
        WardrobeText.Text = wb.Wardrobe ? _session.WardrobeText : "Tủ đồ đang tắt (Wellbeing.Wardrobe = false).";
        var best = e.Snap.Wardrobe?.Best ?? 0;
        var choice = UiSettings.LoadAccessory(_session.Env);
        foreach (var b in Wardrobe.Children.OfType<Button>())
        {
            var tag = (string)b.Tag;
            b.IsEnabled = wb.Wardrobe && (tag is "auto" or "none" || Core.Engine.Wardrobe.Find(tag) is { } item && best >= item.Streak);
            b.Background = tag == choice ? (Brush)FindResource("Cream") : (Brush)FindResource("Panel2");
            b.Foreground = tag == choice ? (Brush)FindResource("Ink") : (Brush)FindResource("Text");
        }
        Brain.Render(e);
    }

    private void Accessory_Click(object sender, RoutedEventArgs e)
    {
        ((App)Application.Current).Companion?.SetAccessory((string)((Button)sender).Tag);
        Render();
    }

    private void ClearPat_Click(object sender, RoutedEventArgs e) => _session.ClearPat();

    private void Wipe_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Xoá toàn bộ điểm mỗi ngày, mẫu mood, log phản hồi và đánh giá cuộc họp của môi trường này?\n(Không đụng tới đăng nhập, PAT hay API key.)",
                "Minditful", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            _session.WipeLocalData();
    }

    private void ResetDay_Click(object sender, RoutedEventArgs e)
    {
        _session.ResetDay();
        ((App)Application.Current).Companion?.Refresh();
    }

    private void WorkEnd_Click(object sender, RoutedEventArgs e)
    {
        if (!_session.SetWorkEnd(WorkEndBox.Text)) MessageBox.Show("Nhập giờ dạng HH:mm, ví dụ 17:30.", "Minditful");
    }

    private void WorkEndSoon_Click(object sender, RoutedEventArgs e)
    {
        WorkEndBox.Text = DateTime.Now.AddMinutes(2).ToString("HH:mm");
        _session.SetWorkEnd(WorkEndBox.Text);
    }

    private void SaveClaudeKey_Click(object sender, RoutedEventArgs e)
    {
        _session.SaveClaudeKey(ClaudeKeyBox.Password);
        ClaudeKeyBox.Clear();
        Render();
    }

    private void Corner_Click(object sender, RoutedEventArgs e)
    {
        if (Enum.TryParse<Corner>((string)((Button)sender).Tag, out var c)) ((App)Application.Current).Companion?.SetCorner(c);
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
            case "typing" or "away" or "fullscreen" or "dnd" or "presenting": _session.Toggle((string)((Button)sender).Tag); break;
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
