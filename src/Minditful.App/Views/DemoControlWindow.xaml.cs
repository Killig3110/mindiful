using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Minditful.App.Rendering;
using Minditful.App.Services;
using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;
using Minditful.Integrations;

namespace Minditful.App.Views;

/// <summary>
/// Bảng điều khiển của môi trường Demo: điều khiển đồng hồ kịch bản, nhảy mốc, bật từng case, bẻ kịch bản.
/// Milo không nằm trong cửa sổ này — Milo ở trên desktop thật như Sandbox/Production.
/// </summary>
public partial class DemoControlWindow : Window
{
    private static readonly (string Group, CaseId[] Cases)[] CaseGroups =
    [
        ("Xã giao", [CaseId.MorningHello, CaseId.CheckIn, CaseId.EodWrapup, CaseId.EodNudge]),
        ("Hỗ trợ công việc", [CaseId.MeetingSoon, CaseId.CalendarPacked, CaseId.EmailWaiting, CaseId.StuckTask, CaseId.TaskDone, CaseId.FocusDone]),
        ("Chăm sóc", [CaseId.MeetingOverload, CaseId.Overtime, CaseId.LunchMissed, CaseId.NoBreak, CaseId.LowRest, CaseId.HighFragmentation]),
        ("Người dùng mở", [CaseId.Dashboard]),
        ("Mở rộng", [CaseId.FocusPlan, CaseId.WeekReport, CaseId.MicroBreak]),
    ];

    private readonly DemoSession _session;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private int _mileIdx = -2;

    internal DemoControlWindow(DemoSession session)
    {
        InitializeComponent();
        _session = session;
        LlmLine.Text = session.LlmStatus;
        BuildMilestones();
        BuildCaseList();
        HighlightSpeed();
        // Nằm bên trái màn hình để không che góc của Milo
        var wa = SystemParameters.WorkArea;
        Left = wa.Left + 24;
        Top = wa.Top + Math.Max(0, (wa.Height - Height) / 2);
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

    private void BuildMilestones()
    {
        for (var i = 0; i < DemoScenario.Milestones.Length; i++)
        {
            var m = DemoScenario.Milestones[i];
            var name = new TextBlock { Text = m.Label, TextWrapping = TextWrapping.Wrap };
            Grid.SetColumn(name, 1);
            var b = new Button
            {
                Style = (Style)FindResource("Flat"), Padding = new Thickness(8, 5, 8, 5), Tag = i,
                Content = new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(44) }, new ColumnDefinition() },
                    Children = { new TextBlock { Text = m.Time, FontSize = 11.5 }, name },
                },
            };
            b.Click += (_, _) =>
            {
                _session.JumpTo(DemoScenario.Milestones[(int)b.Tag]);
                RefreshMilo();
            };
            Miles.Children.Add(b);
        }
    }

    private void BuildCaseList()
    {
        foreach (var (group, cases) in CaseGroups)
        {
            Cases.Children.Add(new TextBlock { Text = group, FontSize = 11, Foreground = (Brush)FindResource("Muted"), Margin = new Thickness(8, 6, 0, 2) });
            foreach (var c in cases)
            {
                var def = Catalog.Def(c);
                var dot = Ui.Dot(def.Color, 9);
                dot.Margin = new Thickness(0, 0, 8, 0);
                var name = new TextBlock { Text = def.Name, VerticalAlignment = VerticalAlignment.Center };
                var src = new TextBlock { Text = def.Ref, FontSize = 10.5, Foreground = (Brush)FindResource("Muted"), VerticalAlignment = VerticalAlignment.Center };
                var b = new Button
                {
                    Style = (Style)FindResource("Flat"), Padding = new Thickness(8, 5, 8, 5), Tag = c,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch, Content = Ui.Columns((dot, Ui.Auto), (name, Ui.Star), (src, Ui.Auto)),
                };
                b.Click += (_, _) =>
                {
                    _session.RunCase((CaseId)b.Tag);
                    RefreshMilo();
                };
                Cases.Children.Add(b);
            }
        }
    }

    private static void RefreshMilo() => ((App)Application.Current).Companion?.Refresh();

    private void Render()
    {
        var e = _session.Engine;
        var s = e.S;
        Clock.Text = Tm.Hm(s.T);
        ClockState.Text = Present.ClockState(e);
        PlayBtn.Content = _session.Playing ? "Tạm dừng" : s.Ended ? "Hết ngày" : "Phát tiếp";
        var cap = Present.Caption(e);
        CapTag.Text = cap.Tag.ToUpperInvariant();
        CapText.Text = cap.Text;
        CapRef.Text = cap.Ref;
        Brain.Render(e);

        SetAct("typing", s.Typing, "Dừng gõ phím", "Đang gõ phím");
        SetAct("away", s.Away, "Quay lại máy", "Rời khỏi máy");
        SetAct("lock", s.Locked, "Mở khoá máy", "Khoá máy");
        SetAct("fullscreen", s.Fullscreen, "Thoát toàn màn hình", "Mở toàn màn hình");
        SetAct("dnd", s.UserDnd, "Tắt Không làm phiền", "Teams: Không làm phiền");
        SetAct("stress", s.Stress > 0, "Bỏ giả lập ngày căng", "Giả lập ngày căng (−30)");
        SetAct("presenting", s.Presenting, "Thôi trình chiếu", "Teams: đang trình chiếu");
        Act("wardrobe").Content = "Tủ đồ: " + (UiSettings.LoadAccessory(AppEnvironment.Demo) switch
        {
            "none" => "không mặc", "auto" => "tự chọn", var id => Wardrobe.Find(id)?.Name ?? id,
        }) + " → đổi";
        Act("leave").IsEnabled = e.InCall();

        var cur = -1;
        for (var i = 0; i < DemoScenario.Milestones.Length; i++)
            if (Tm.T(DemoScenario.Milestones[i].Time) <= s.T) cur = i;
        if (cur == _mileIdx) return;
        _mileIdx = cur;
        for (var i = 0; i < Miles.Children.Count; i++)
        {
            var b = (Button)Miles.Children[i];
            b.Background = i == cur ? (Brush)FindResource("Amber") : Brushes.Transparent;
            b.Foreground = i == cur ? Ui.Br("#2B211A") : i < cur ? (Brush)FindResource("Muted") : (Brush)FindResource("Soft");
        }
    }

    private Button Act(string tag) => Acts.Children.OfType<Button>().First(b => (string)b.Tag == tag);

    private void SetAct(string tag, bool on, string onLabel, string offLabel)
    {
        var b = Act(tag);
        b.Content = on ? onLabel : offLabel;
        b.Background = on ? (Brush)FindResource("Cream") : (Brush)FindResource("Panel2");
        b.Foreground = on ? (Brush)FindResource("Ink") : (Brush)FindResource("Text");
    }

    private void HighlightSpeed()
    {
        foreach (Button b in SpeedSeg.Children)
        {
            var on = int.Parse((string)b.Tag) == _session.Speed;
            b.Background = on ? (Brush)FindResource("Cream") : Brushes.Transparent;
            b.Foreground = on ? (Brush)FindResource("Ink") : (Brush)FindResource("Soft");
        }
    }

    private void Play_Click(object sender, RoutedEventArgs e) => _session.TogglePlay();

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _session.Restart();
        RefreshMilo();
    }

    private void Speed_Click(object sender, RoutedEventArgs e)
    {
        _session.Speed = int.Parse((string)((Button)sender).Tag);
        HighlightSpeed();
    }

    private void Auto_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        _session.Engine.SetAuto(AutoChk.IsChecked == true);
    }

    private void Act_Click(object sender, RoutedEventArgs e)
    {
        var eng = _session.Engine;
        var s = eng.S;
        switch ((string)((Button)sender).Tag)
        {
            case "typing": eng.SetTyping(!s.Typing); break;
            case "away": eng.SetAway(!s.Away); break;
            case "lock": eng.SetLocked(!s.Locked); break;
            case "fullscreen": eng.SetFullscreen(!s.Fullscreen); break;
            case "dnd": eng.SetUserDnd(!s.UserDnd); break;
            case "leave": eng.LeaveMeeting(); break;
            case "frag": eng.SimulateFragmentation(); break;
            case "done": eng.MarkTaskDone(); break;
            case "stress": eng.ToggleStress(); break;
            case "dash": ((App)Application.Current).Companion?.OpenDashboard(); break;
            case "presenting": eng.SetPresenting(!s.Presenting); break;
            case "wardrobe":
                // Ngày mẫu có sẵn chuỗi 10 ngày về đúng giờ nên mặc thử được cả 3 món
                string[] cycle = ["auto", "scarf", "flower", "beret", "none"];
                var cur = Array.IndexOf(cycle, UiSettings.LoadAccessory(AppEnvironment.Demo));
                ((App)Application.Current).Companion?.SetAccessory(cycle[(cur + 1) % cycle.Length]);
                break;
        }
        RefreshMilo();
        Render();
    }
}
