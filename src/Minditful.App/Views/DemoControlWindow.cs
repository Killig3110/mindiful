using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Minditful.App.Services;
using Minditful.App.Views.Panel;
using Minditful.Core.Engine;
using Minditful.Core.Scenario;
using Minditful.Integrations;
using static Minditful.App.Rendering.Ui;

namespace Minditful.App.Views;

/// <summary>
/// Bảng điều khiển của môi trường Demo: phát/tua ngày mẫu, nhảy tới từng mốc, cho Milo làm từng tình huống,
/// giả vờ bạn đang làm gì, tủ đồ. Milo không nằm trong cửa sổ này — Milo ở góc desktop như Sandbox/Production.
/// </summary>
internal sealed class DemoControlWindow : ControlShell
{
    private readonly DemoSession _session;

    protected override MiloEngine Engine => _session.Engine;
    protected override string ClockNote => "Thứ Năm 24/9 · giờ trong kịch bản";
    protected override Minditful.Integrations.Llm.IMiloLlm? Llm => _session.Llm;
    protected override LlmOptions? LlmOpts => _session.LlmOptions;

    public DemoControlWindow(DemoSession session)
        : base("Minditful · Demo", "DEMO · NGÀY MẪU", "#7261B0",
            "Milo chạy theo 1 ngày làm việc mẫu, không cần tài khoản hay mạng. Dùng để xem thử và present.")
    {
        _session = session;
        AddPage("home", IcHome, "Bắt đầu", Home);
        AddPage("tour", IcPlay, "Kịch bản trình diễn", Tour, "28 bước · ~21 phút");
        AddPage("day", IcTimeline, "Ngày mẫu", Day, "nhảy tới từng mốc");
        AddPage("try", IcTry, "Thử tình huống", Try, "cho Milo làm ngay");
        AddPage("milo", IcMilo, "Milo của bạn", Milo, "tủ đồ, góc màn hình");
        AddPage("engine", IcEngine, "Mood Engine", MoodEnginePage, "luật ↔ AI · kiểm chứng");
        AddPage("brain", IcBrain, "Bộ não Milo", BrainPage, "nâng cao");
        // Nằm bên trái màn hình để không che góc của Milo
        var wa = SystemParameters.WorkArea;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Width = Math.Min(Width, wa.Width - 520);
        Left = wa.Left + 16;
        Top = wa.Top + Math.Max(0, (wa.Height - Height) / 2);
        _session.Changed += Refresh;
        Closed += (_, _) => _session.Changed -= Refresh;
        Show("home");
    }

    // ================= Bắt đầu =================
    private FrameworkElement Home()
    {
        var play = Btn("Tạm dừng", () => _session.TogglePlay(), BtnKind.Primary, IcPause);
        Tick(() =>
        {
            var s = Engine.S;
            var (label, icon) = _session.Playing ? ("Tạm dừng", IcPause) : s.Ended ? ("Hết ngày mẫu", IcPlay) : ("Phát tiếp", IcPlay);
            ((TextBlock)((StackPanel)play.Content).Children[0]).Text = icon;
            ((TextBlock)((StackPanel)play.Content).Children[1]).Text = label;
            play.IsEnabled = !s.Ended;
        });
        var restart = Btn("Làm lại từ 08:50", () =>
        {
            _session.Restart();
            RefreshMilo();
        }, BtnKind.Ghost, IcRestart);
        var speed = Segmented([("Chậm 60×", "60"), ("Vừa 120×", "120"), ("Nhanh 300×", "300")],
            () => _session.Speed.ToString(), v => _session.Speed = int.Parse(v));
        var player = Card(new StackPanel
        {
            Children =
            {
                Row(play, restart, speed),
                Text("Lúc Milo ẩn, đồng hồ kịch bản tua nhanh theo tốc độ đã chọn. Lúc Milo xuất hiện, thời gian chạy thật để bạn xem trọn hoạt ảnh.", 12, P.Ink2),
                Spacer(12),
                Switch("Người dùng mẫu tự bấm nút", "Bật: kịch bản tự trả lời mọi thẻ như trong tài liệu. Tắt: bạn tự bấm nút trên thẻ của Milo.",
                    () => Engine.S.Auto, () => Engine.SetAuto(!Engine.S.Auto)),
            },
        }, "Phát ngày mẫu", "Ngày mẫu Thứ Năm 24/9 từ 08:50 tới 18:45: 4 cuộc họp, 5 email chờ, 6 task. Milo làm đúng như kịch bản hành vi.");

        var e = Engine;
        var tiles = Row(
            Tile(() => e.S.Score.ToString(), "điểm mood", P.Accent, () => e.CurrentBand.Label),
            Tile(() => $"{e.Meetings.Count(m => m.End <= e.S.T)}/{e.Meetings.Count}", "cuộc họp đã xong"),
            Tile(() => e.WaitingEmails().Count.ToString(), "email đang chờ bạn"),
            Tile(() => e.InProgress().ToString(), "task đang làm", note: () => e.StuckTasks().Count > 0 ? $"{e.StuckTasks().Count} task kẹt" : ""),
            Tile(() => e.S.AcceptedBreaks.ToString(), "lần nghỉ cùng Milo"));
        var today = Card(tiles, "Hôm nay theo kịch bản");

        var llm = Text(_session.LlmStatus, 11.5, P.Muted);
        return Page("Chào bạn!",
            "Milo đang sống ở góc phải dưới màn hình của bạn. Cửa sổ này để điều khiển ngày mẫu và thử từng tình huống. Nhìn góc màn hình để xem Milo phản ứng.",
            player, today,
            MoodCard(),
            TipBox(
                "Đi present? Mở trang \"Kịch bản trình diễn\": 28 bước đi qua đủ 22 tình huống, 9 động tác hài, tủ đồ, có gợi ý câu nói cho từng bước. Bản 13 phút cho ngày present: docs/KICH-BAN-DEMO.md mục 0.",
                "Rê chuột lên chóp đuôi cam ở góc màn hình khoảng nửa giây: Milo ló đầu thì thầm điểm mood.",
                "Bấm chóp đuôi: mở dashboard 4 quả. Bấm \"Chi tiết\" để xem bảng nhỏ hôm nay / tuần này.",
                "Kéo chóp đuôi sang góc khác để đổi chỗ Milo. Esc để đóng dashboard.",
                "Thẻ của Milo có ô chat: gõ \"mệt quá\", \"đang bận\", \"về thôi\" để xem Milo hiểu ý."),
            llm);
    }

    // ================= Mood realtime =================
    /// <summary>Kéo mức căng thẳng / bấm nghỉ, xong task → điểm tính lại ngay, Milo đứng ở góc đổi dáng và màu theo.</summary>
    private Border MoodCard()
    {
        var e = Engine;
        var slider = new Slider
        {
            Minimum = 0, Maximum = 60, Width = 320, TickFrequency = 10, IsSnapToTickEnabled = false, SmallChange = 5, LargeChange = 10,
            Foreground = Br(P.Accent), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 8),
        };
        System.Windows.Automation.AutomationProperties.SetName(slider, "Mức căng thẳng giả lập");
        var syncing = false;
        slider.ValueChanged += (_, ev) =>
        {
            if (syncing) return;
            e.SetStressLevel(ev.NewValue);
            RefreshMilo();
        };
        var value = Text("", 12.5, P.Ink2, FontWeights.SemiBold, false);
        value.VerticalAlignment = VerticalAlignment.Center;
        Tick(() =>
        {
            value.Text = $"−{e.S.Stress:0} điểm";
            if (slider.IsMouseCaptureWithin || Math.Abs(slider.Value - e.S.Stress) < .5) return;
            syncing = true;
            slider.Value = e.S.Stress;
            syncing = false;
        });
        var look = Text("", 12.5, P.Ink);
        Tick(() => look.Text = e.S.BandIdx switch
        {
            0 => "Milo tươi tắn, màu cam đậm, dáng khoẻ.",
            1 => "Milo cân bằng, màu bình thường.",
            2 => "Milo nhạt màu, dáng uể oải, đuôi cụp.",
            _ => "Milo kiệt sức: nhạt hẳn màu, dáng mệt, có chữ z bay.",
        });
        return Card(new StackPanel
        {
            Children =
            {
                Row(Tile(() => e.S.Score.ToString(), "điểm mood lúc này", P.Accent, () => e.CurrentBand.Label),
                    Switch("Gọi Milo ra đứng ở góc", "Milo ở ngoài để người xem thấy dáng và màu đổi theo điểm. Tắt để Milo đi.",
                        () => e.S.HoldVisit, () => e.CallMilo(!e.S.HoldVisit))),
                Label("Căng thẳng giả lập (kéo sang phải = ngày nặng hơn)"),
                Spacer(4),
                Row(slider, value),
                Row(Btn("Nghỉ cùng Milo (+3)", () =>
                {
                    e.SimulateBreak();
                    RefreshMilo();
                }), Btn("Xong 1 task (+2)", () =>
                {
                    e.MarkTaskDone();
                    RefreshMilo();
                }), Btn("Về 0", () =>
                {
                    e.SetStressLevel(0);
                    RefreshMilo();
                }, BtnKind.Ghost)),
                look,
            },
        }, "Mood realtime", "Điểm mood tính lại ngay khi có gì thay đổi. Milo không bật popup mà đổi dáng để bạn tự nhận ra. Chi tiết từng khoản ở trang Bộ não Milo.");
    }

    // ================= Kịch bản trình diễn =================
    private int _tourIndex = -1;
    private bool _tourAuto, _sawBusy;
    private DateTime _stepStarted;
    private DateTime? _quietSince;

    private void RunStep(int i)
    {
        if (i < 0 || i >= DemoTour.Steps.Length)
        {
            _tourAuto = false;
            return;
        }
        _tourIndex = i;
        var step = DemoTour.Steps[i];
        if (_tourAuto && !Engine.S.Auto) Engine.SetAuto(true); // tự chạy thì người dùng mẫu tự bấm nút trên thẻ
        _session.RunTourStep(step);
        ScheduleStep(i, step);
        _stepStarted = DateTime.Now;
        _sawBusy = false;
        _quietSince = null;
        RefreshMilo();
        Refresh();
    }

    /// <summary>Việc host làm tiếp trong 1 bước (gõ chat, thêm "Có mới", bấm dashboard, diễn 9 động tác, phối đồ). Rời bước thì thôi.</summary>
    private void ScheduleStep(int index, TourStep step)
    {
        var companion = ((App)Application.Current).Companion;
        void At(double sec, Action act)
        {
            var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(sec) };
            t.Tick += (_, _) =>
            {
                t.Stop();
                if (_tourIndex != index) return;
                act();
                companion?.Refresh();
                RefreshMilo();
            };
            t.Start();
        }
        bool Showing(CaseId c) => Engine.S.Ep is { } ep && ep.C == c && ep.Phase == Phase.Show;
        switch (step.Kind)
        {
            case TourKind.Case when step.Case == CaseId.Talk:
                At(3.5, () => { if (Showing(CaseId.Talk)) Engine.UserReply("chat", "tui cũng khá mệt mà còn nhiều task quá"); });
                At(14, () => { if (Showing(CaseId.Talk)) Engine.UserReply("close"); });
                break;
            case TourKind.Case when step.Case == CaseId.Incoming:
                At(1.2, () => Engine.SimulateIncoming("meeting"));
                At(2.4, () => Engine.SimulateIncoming("task"));
                break;
            case TourKind.Dashboard:
                At(3, () => { if (Showing(CaseId.Dashboard)) Engine.UserReply("detail"); });
                At(7.5, () => { if (Showing(CaseId.Dashboard)) Engine.UserReply("week"); });
                At(12, () => { if (Showing(CaseId.Dashboard)) Engine.UserReply("close"); });
                break;
            case TourKind.Focus:
                At(9, Engine.StopFocusNow);
                break;
            case TourKind.Meme:
            {
                var t = 0.0;
                foreach (var (clip, sec) in DemoTour.MemeReel.Skip(1))
                {
                    t += DemoTour.MemeReel.TakeWhile(m => m.Clip != clip).Last().Seconds + .4;
                    var (c, s) = (clip, sec);
                    At(t, () => Engine.PlayMeme(c, s));
                }
                break;
            }
            case TourKind.Wardrobe:
                for (var k = 0; k < DemoTour.OutfitReel.Length; k++)
                {
                    var outfit = DemoTour.OutfitReel[k];
                    At(1.5 + k * 2.6, () => companion?.SetAccessory(outfit));
                }
                At(1.5 + DemoTour.OutfitReel.Length * 2.6 + 1, () => { if (Showing(CaseId.Dashboard)) Engine.UserReply("close"); });
                break;
        }
    }

    /// <summary>Bước không chờ Milo xong việc: tự chạy chờ bao lâu rồi sang bước kế (giây).</summary>
    private static double StepSeconds(TourStep s) => s.Kind switch
    {
        TourKind.Meme => DemoTour.MemeReel.Sum(m => m.Seconds + .4) + 3,
        TourKind.Wardrobe => DemoTour.OutfitReel.Length * 2.6 + 5,
        TourKind.Dashboard => 15,
        TourKind.Focus => 14,
        _ => 12,
    };

    /// <summary>Tự chạy: bước có Milo thì chờ Milo xong việc rồi 2,5 giây sau sang bước kế; bước không có Milo thì 12 giây.</summary>
    protected override void OnRefresh()
    {
        if (!_tourAuto || _tourIndex < 0) return;
        var step = DemoTour.Steps[_tourIndex];
        var now = DateTime.Now;
        var elapsed = (now - _stepStarted).TotalSeconds;
        if (step.Kind == TourKind.Mood)
        {
            // Tự kéo mức căng thẳng lên rồi xuống để người xem thấy Milo đổi dáng
            var target = elapsed < 3 ? 0 : elapsed < 6 ? 30 : elapsed < 9 ? 60 : 0;
            if (Math.Abs(Engine.S.Stress - target) > .5) Engine.SetStressLevel(target);
        }
        bool done;
        if (!step.ExpectMilo) done = elapsed > StepSeconds(step);
        else if (Engine.Busy)
        {
            _sawBusy = true;
            _quietSince = null;
            done = elapsed > 180;
        }
        else
        {
            if (_sawBusy) _quietSince ??= now;
            done = (_quietSince is { } q && (now - q).TotalSeconds > 2.5) || elapsed > 180;
        }
        if (!done) return;
        if (_tourIndex + 1 < DemoTour.Steps.Length) RunStep(_tourIndex + 1);
        else _tourAuto = false;
    }

    private FrameworkElement Tour()
    {
        var steps = DemoTour.Steps;
        var title = Text("", 20, P.Ink, FontWeights.Bold);
        var show = Text("", 13, P.Ink);
        var say = Text("", 13, "#3B2E66");
        var sayBox = new Border { Background = Br("#F3EDFF"), CornerRadius = new CornerRadius(4, 14, 14, 14), Padding = new Thickness(12, 9, 12, 9), Margin = new Thickness(0, 8, 0, 12), Child = say };
        var prev = Btn("‹ Bước trước", () => RunStep(Math.Max(0, _tourIndex - 1)), BtnKind.Ghost);
        var run = Btn("Bắt đầu kịch bản", () => RunStep(Math.Max(0, _tourIndex)), BtnKind.Soft, IcPlay);
        var next = Btn("Bước tiếp ›", () => RunStep(_tourIndex + 1), BtnKind.Primary);
        Tick(() =>
        {
            var i = _tourIndex;
            var s = i >= 0 ? steps[i] : null;
            title.Text = s is null ? "Chưa bắt đầu" : $"Bước {i + 1}/{steps.Length} · {s.Title}";
            show.Text = s is null ? "Bấm \"Bắt đầu kịch bản\" rồi nhìn góc phải dưới màn hình. Mỗi bước Milo làm 1 tình huống."
                : "Người xem thấy: " + s.Show;
            say.Text = s is null ? "Gợi ý mở đầu: “Milo là chú cáo sống ở góc màn hình, chỉ ló ra đúng lúc để giúp bạn làm việc khoẻ hơn.”" : "Bạn nói: “" + s.Say + "”";
            ((TextBlock)((StackPanel)run.Content).Children[1]).Text = i < 0 ? "Bắt đầu kịch bản" : "Chạy lại bước này";
            prev.IsEnabled = i > 0;
            next.IsEnabled = i < steps.Length - 1;
        });
        var current = Card(new StackPanel
        {
            Children =
            {
                title, Spacer(6), show, sayBox,
                Row(prev, run, next),
                Switch("Tự chạy qua các bước", "Milo xong việc ở bước này thì tự sang bước sau (bước không có Milo chờ theo độ dài bước). Tắt để tự bấm từng bước.",
                    () => _tourAuto, () =>
                    {
                        _tourAuto = !_tourAuto;
                        if (_tourAuto && _tourIndex < 0) RunStep(0);
                    }),
            },
        }, padding: new Thickness(20, 18, 20, 18));

        var list = new StackPanel();
        var rows = new List<Border>();
        for (var i = 0; i < steps.Length; i++)
        {
            var s = steps[i];
            var kind = s.Kind switch
            {
                TourKind.Milestone => $"ngày mẫu {s.Time}",
                TourKind.Case => "cho Milo làm",
                TourKind.Presenting => "giả vờ trình chiếu",
                TourKind.Mood => "mood realtime",
                TourKind.Meme => "Milo hài hước",
                TourKind.Dashboard => "dashboard",
                TourKind.Focus => "tập trung",
                _ => "tủ đồ",
            };
            var num = Text($"{i + 1}", 13, P.Accent, FontWeights.Bold, false);
            var name = Text(s.Title, 13, P.Ink, FontWeights.SemiBold, false);
            name.Margin = new Thickness(10, 0, 10, 0);
            var tag = Text(kind, 11, P.Muted, null, false);
            var row = new Border
            {
                CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 0, 3), Cursor = Cursors.Hand,
                BorderThickness = new Thickness(1), Focusable = true, Child = Columns((num, Px(26)), (name, Star), (tag, Auto)),
            };
            System.Windows.Automation.AutomationProperties.SetName(row, $"Bước {i + 1}: {s.Title}");
            var idx = i;
            row.MouseLeftButtonUp += (_, _) => RunStep(idx);
            row.KeyDown += (_, ev) =>
            {
                if (ev.Key is Key.Enter or Key.Space) RunStep(idx);
            };
            row.MouseEnter += (_, _) => row.BorderBrush = Br(P.Accent);
            row.MouseLeave += (_, _) => row.BorderBrush = null;
            rows.Add(row);
            list.Children.Add(row);
        }
        Tick(() =>
        {
            for (var i = 0; i < rows.Count; i++)
            {
                rows[i].Background = Br(i == _tourIndex ? "#FFF1DE" : "#00000000");
                rows[i].Opacity = _tourIndex >= 0 && i < _tourIndex ? .6 : 1;
            }
        });

        return Page("Kịch bản trình diễn",
            "28 bước đi qua đủ 22 tình huống của Milo cùng các tính năng mới, khoảng 21 phút. 12 bước đầu theo ngày mẫu Thứ Năm 24/9, các bước sau cho Milo làm từng tình huống còn lại. Kịch bản lời nói đầy đủ: docs/KICH-BAN-DEMO.md.",
            current, MoodCard(), Card(list, "Tất cả bước", "Bấm 1 bước để chạy ngay bước đó."));
    }

    // ================= Ngày mẫu =================
    private FrameworkElement Day()
    {
        var list = new StackPanel();
        var rows = new List<(Border Row, TextBlock State, double T)>();
        foreach (var m in DemoScenario.Milestones)
        {
            var time = Text(m.Time, 14, P.Ink, FontWeights.Bold, false);
            var label = Text(m.Label, 13, P.Ink);
            label.Margin = new Thickness(12, 0, 12, 0);
            var state = Text("", 11.5, P.Muted, FontWeights.SemiBold, false);
            var row = new Border
            {
                CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 9, 12, 9), Margin = new Thickness(0, 0, 0, 4), Cursor = Cursors.Hand,
                Child = Columns((time, Px(52)), (label, Star), (state, Auto)), Focusable = true,
            };
            System.Windows.Automation.AutomationProperties.SetName(row, $"{m.Time} {m.Label}");
            var mile = m;
            void Go()
            {
                _session.JumpTo(mile);
                RefreshMilo();
                Refresh();
            }
            row.MouseLeftButtonUp += (_, _) => Go();
            row.KeyDown += (_, ev) =>
            {
                if (ev.Key is Key.Enter or Key.Space) Go();
            };
            row.MouseEnter += (_, _) => row.BorderBrush = Br(P.Accent);
            row.MouseLeave += (_, _) => row.BorderBrush = null;
            row.BorderThickness = new Thickness(1);
            rows.Add((row, state, Tm.T(m.Time)));
            list.Children.Add(row);
        }
        Tick(() =>
        {
            var t = Engine.S.T;
            var cur = rows.FindLastIndex(r => r.T <= t);
            for (var i = 0; i < rows.Count; i++)
            {
                var (row, state, _) = rows[i];
                row.Background = Br(i == cur ? "#FFF1DE" : i < cur ? "#00000000" : P.Card);
                row.Opacity = i < cur ? .6 : 1;
                state.Text = i == cur ? "● đang ở đây" : i < cur ? "đã qua" : "";
                state.Foreground = Br(i == cur ? P.Accent : P.Muted);
            }
        });
        return Page("Ngày mẫu Thứ Năm 24/9",
            "17 mốc của ngày mẫu. Bấm 1 mốc để tua tới ngay trước lúc đó rồi xem Milo làm gì ở góc màn hình.",
            Card(list));
    }

    // ================= Thử tình huống =================
    private FrameworkElement Try()
    {
        var e = Engine;
        var cases = Card(CaseList(c => _session.RunCase(c)), "Cho Milo làm ngay",
            "Bấm 1 thẻ là Milo làm tình huống đó ngay, bỏ qua điều kiện. Nếu lúc đó máy đang khoá hoặc đang họp theo kịch bản, đồng hồ tự tua tới 13:02.");

        var you = new WrapPanel
        {
            Children =
            {
                Switch("Đang gõ phím", "Milo chờ bạn dừng tay. Gõ liền 5 phút thì chỉ hiện nhãn gọn.", () => e.S.Typing, () => e.SetTyping(!e.S.Typing)),
                Switch("Rời khỏi máy", "Milo không nói với màn hình trống. Quay lại được tính là 1 lần nghỉ.", () => e.S.Away, () => e.SetAway(!e.S.Away)),
                Switch("Khoá máy", "Milo nghỉ hẳn. Mở khoá lần đầu trong ngày thì Milo chào sáng.", () => e.S.Locked, () => e.SetLocked(!e.S.Locked)),
                Switch("Mở app toàn màn hình", "Milo im lặng, lời nhắc thành chấm chờ ở góc.", () => e.S.Fullscreen, () => e.SetFullscreen(!e.S.Fullscreen)),
                Switch("Teams: Không làm phiền", "Milo im lặng như trên.", () => e.S.UserDnd, () => e.SetUserDnd(!e.S.UserDnd)),
                Switch("Teams: đang trình chiếu", "Milo trốn hẳn, kể cả chóp đuôi.", () => e.S.Presenting, () => e.SetPresenting(!e.S.Presenting)),
                Switch("Ngày căng thẳng", "Trừ 30 điểm mood để xem Milo mệt, nhạt màu, có chữ z bay.", () => e.S.Stress > 0, e.ToggleStress),
            },
        };
        var once = new WrapPanel
        {
            Children =
            {
                ActionTile("Rời cuộc họp", "Chỉ dùng được khi đang trong giờ họp của kịch bản.", e.LeaveMeeting, e.InCall),
                ActionTile("Nhảy việc 12 lần", "Chuyển qua lại giữa các app → Milo đề nghị gom việc.", e.SimulateFragmentation),
                ActionTile("Xong 1 task", "Kéo 1 task sang Done → Milo nhảy tưng ăn mừng.", () => e.MarkTaskDone()),
                ActionTile("Mở dashboard", "Như bấm vào chóp đuôi Milo.", () => ((App)Application.Current).Companion?.OpenDashboard()),
            },
        };
        return Page("Thử tình huống",
            "Hai cách để xem Milo phản ứng: bảo Milo làm 1 tình huống, hoặc giả vờ bạn đang làm gì đó rồi xem Milo cư xử ra sao.",
            cases,
            Card(new StackPanel { Children = { you, Spacer(6), Label("Làm 1 lần"), Spacer(6), once } }, "Giả vờ bạn đang…",
                "Bật công tắc để giả vờ, tắt để thôi. Kịch bản vẫn chạy tiếp."));
    }

    // ================= Milo của bạn =================
    private FrameworkElement Milo() => Page("Milo của bạn",
        "Phối đồ và chỗ đứng của Milo. Ngày mẫu mở full tủ đồ (14/14 món): 15 ngày về đúng giờ, 24 lần nghỉ, 5h20 tập trung, và đã giữ đồ của mọi mùa (Tết, Trung thu, Halloween, Noel).",
        WardrobeCard(AppEnvironment.Demo, () => Engine.Snap.Wardrobe, () => Engine.Day),
        PersonalityCard(AppEnvironment.Demo, Engine, preview: true),
        CornerCard(AppEnvironment.Demo));

}
