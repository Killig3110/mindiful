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

    public DemoControlWindow(DemoSession session)
        : base("Minditful · Demo", "DEMO · NGÀY MẪU", "#7261B0",
            "Milo chạy theo 1 ngày làm việc mẫu, không cần tài khoản hay mạng. Dùng để xem thử và present.")
    {
        _session = session;
        AddPage("home", IcHome, "Bắt đầu", Home);
        AddPage("day", IcTimeline, "Ngày mẫu", Day, "nhảy tới từng mốc");
        AddPage("try", IcTry, "Thử tình huống", Try, "cho Milo làm ngay");
        AddPage("milo", IcMilo, "Milo của bạn", Milo, "tủ đồ, góc màn hình");
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
            TipBox(
                "Rê chuột lên chóp đuôi cam ở góc màn hình khoảng nửa giây: Milo ló đầu thì thầm điểm mood.",
                "Bấm chóp đuôi: mở dashboard 4 quả. Bấm \"Chi tiết\" để xem bảng nhỏ hôm nay / tuần này.",
                "Kéo chóp đuôi sang góc khác để đổi chỗ Milo. Esc để đóng dashboard.",
                "Thẻ của Milo có ô chat: gõ \"mệt quá\", \"đang bận\", \"về thôi\" để xem Milo hiểu ý."),
            llm);
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
        "Đổi phụ kiện và chỗ đứng của Milo. Ngày mẫu có sẵn chuỗi 10 ngày về đúng giờ nên mặc thử được cả 3 món.",
        WardrobeCard(AppEnvironment.Demo, () => Engine.Snap.Wardrobe?.Best ?? 0,
            () => $"Chuỗi về đúng giờ (mẫu): {Engine.Snap.Wardrobe?.Streak ?? 0} ngày."),
        CornerCard(AppEnvironment.Demo));

    private static Border Spacer(double h) => new() { Height = h };
}
