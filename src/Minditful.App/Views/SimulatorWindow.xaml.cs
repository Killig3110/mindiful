using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Minditful.App.Rendering;
using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;

namespace Minditful.App.Views;

/// <summary>Môi trường Demo: chạy ngày mẫu 24/9 đúng như prototype, có tua, nhảy mốc và "Bạn thử làm".</summary>
public partial class SimulatorWindow : Window
{
    private readonly MiloEngine _engine = DemoScenario.CreateEngine();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _last, _brainAt;
    private bool _playing = true;
    private int _speed = 120;
    private int _mileIdx = -2;

    public SimulatorWindow()
    {
        InitializeComponent();
        Scene.Layer.Engine = _engine;
        Scene.Layer.Interacted += RenderPanels;
        for (var i = 0; i < DemoScenario.Milestones.Length; i++)
        {
            var m = DemoScenario.Milestones[i];
            var b = new Button { Style = (Style)FindResource("Flat"), Padding = new Thickness(8, 5, 8, 5), Tag = i };
            b.Content = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(44) }, new ColumnDefinition() },
                Children = { new TextBlock { Text = m.Time, FontSize = 11.5 }, Col1(new TextBlock { Text = m.Label, TextWrapping = TextWrapping.Wrap }) },
            };
            b.Click += Mile_Click;
            Miles.Children.Add(b);
        }
        BuildCaseList();
        HighlightSpeed();
        KeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape) return;
            _engine.Escape();
            RenderPanels();
        };
        Loaded += (_, _) => CompositionTarget.Rendering += OnFrame;
        Closed += (_, _) => CompositionTarget.Rendering -= OnFrame;
        RenderPanels();
    }

    private static readonly (string Group, CaseId[] Cases)[] CaseGroups =
    [
        ("Xã giao", [CaseId.MorningHello, CaseId.CheckIn, CaseId.EodWrapup, CaseId.EodNudge]),
        ("Hỗ trợ công việc", [CaseId.MeetingSoon, CaseId.CalendarPacked, CaseId.EmailWaiting, CaseId.StuckTask, CaseId.TaskDone, CaseId.FocusDone]),
        ("Chăm sóc", [CaseId.MeetingOverload, CaseId.Overtime, CaseId.LunchMissed, CaseId.NoBreak, CaseId.LowRest, CaseId.HighFragmentation]),
        ("Người dùng mở", [CaseId.Dashboard]),
    ];

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
                var row = Ui.Columns((dot, Ui.Auto), (name, Ui.Star), (src, Ui.Auto));
                var b = new Button { Style = (Style)FindResource("Flat"), Padding = new Thickness(8, 5, 8, 5), Content = row, Tag = c, HorizontalContentAlignment = HorizontalAlignment.Stretch };
                b.Click += Case_Click;
                Cases.Children.Add(b);
            }
        }
    }

    private void Case_Click(object sender, RoutedEventArgs e)
    {
        var c = (CaseId)((Button)sender).Tag;
        // Case cần Milo được phép nói: khoá máy/họp/tập trung thì nhảy tới 13:02 (khoảng trống giữa Lịch kín và Email chờ)
        if (_engine.HardGate() is not null || _engine.S.Ended) _engine.RunTo(Tm.T("13:02"));
        _engine.ForceCase(c);
        _playing = true;
        Scene.Layer.Engine = _engine;
        RenderPanels();
    }

    private static T Col1<T>(T el) where T : UIElement
    {
        Grid.SetColumn(el, 1);
        return el;
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        var now = _clock.Elapsed.TotalSeconds;
        var dt = Math.Min(0.1, now - _last);
        _last = now;
        if (_playing && !_engine.S.Ended)
        {
            _engine.Advance(dt * (_engine.Busy ? 1 : _speed));
            if (_engine.S.Ended) _playing = false;
        }
        Scene.Render(_engine);
        if (now - _brainAt > .15)
        {
            _brainAt = now;
            RenderPanels();
        }
    }

    private void RenderPanels()
    {
        var s = _engine.S;
        Clock.Text = Tm.Hm(s.T);
        ClockState.Text = Present.ClockState(_engine);
        PlayBtn.Content = _playing ? "Tạm dừng" : s.Ended ? "Hết ngày" : "Phát tiếp";
        var cap = Present.Caption(_engine);
        CapTag.Text = cap.Tag.ToUpperInvariant();
        CapText.Text = cap.Text;
        CapRef.Text = cap.Ref;
        Brain.Render(_engine);

        SetAct("typing", s.Typing, "Dừng gõ phím", "Đang gõ phím");
        SetAct("away", s.Away, "Quay lại máy", "Rời khỏi máy");
        SetAct("lock", s.Locked, "Mở khoá máy", "Khoá máy");
        SetAct("fullscreen", s.Fullscreen, "Thoát toàn màn hình", "Mở toàn màn hình");
        SetAct("dnd", s.UserDnd, "Tắt Không làm phiền", "Teams: Không làm phiền");
        SetAct("stress", s.Stress > 0, "Bỏ giả lập ngày căng", "Giả lập ngày căng (−30 điểm)");
        Act("leave").IsEnabled = _engine.InCall();

        var cur = -1;
        for (var i = 0; i < DemoScenario.Milestones.Length; i++)
            if (Tm.T(DemoScenario.Milestones[i].Time) <= s.T) cur = i;
        if (cur != _mileIdx)
        {
            _mileIdx = cur;
            for (var i = 0; i < Miles.Children.Count; i++)
            {
                var b = (Button)Miles.Children[i];
                b.Background = i == cur ? (Brush)FindResource("Amber") : Brushes.Transparent;
                b.Foreground = i == cur ? Ui.Br("#2B211A") : i < cur ? (Brush)FindResource("Muted") : (Brush)FindResource("Soft");
            }
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
            var on = int.Parse((string)b.Tag) == _speed;
            b.Background = on ? (Brush)FindResource("Cream") : Brushes.Transparent;
            b.Foreground = on ? (Brush)FindResource("Ink") : (Brush)FindResource("Soft");
        }
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        if (_engine.S.Ended) return;
        _playing = !_playing;
        RenderPanels();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _engine.Reset();
        _playing = true;
        Scene.Layer.Engine = _engine;
        RenderPanels();
    }

    private void Speed_Click(object sender, RoutedEventArgs e)
    {
        _speed = int.Parse((string)((Button)sender).Tag);
        HighlightSpeed();
    }

    private void Auto_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        _engine.SetAuto(AutoChk.IsChecked == true);
    }

    private void Mile_Click(object sender, RoutedEventArgs e)
    {
        var m = DemoScenario.Milestones[(int)((Button)sender).Tag];
        _engine.RunTo(Math.Max(_engine.Cfg.DayOpen, Tm.T(m.Time) - m.LeadSeconds));
        _playing = true;
        Scene.Layer.Engine = _engine;
        RenderPanels();
    }

    private void Act_Click(object sender, RoutedEventArgs e)
    {
        var s = _engine.S;
        switch ((string)((Button)sender).Tag)
        {
            case "typing": _engine.SetTyping(!s.Typing); break;
            case "away": _engine.SetAway(!s.Away); break;
            case "lock": _engine.SetLocked(!s.Locked); break;
            case "fullscreen": _engine.SetFullscreen(!s.Fullscreen); break;
            case "dnd": _engine.SetUserDnd(!s.UserDnd); break;
            case "leave": _engine.LeaveMeeting(); break;
            case "frag": _engine.SimulateFragmentation(); break;
            case "done": _engine.MarkTaskDone(); break;
            case "stress": _engine.ToggleStress(); break;
        }
        RenderPanels();
    }
}
