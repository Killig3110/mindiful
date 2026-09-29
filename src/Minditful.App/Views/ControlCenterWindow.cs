using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Minditful.App.Services;
using Minditful.App.Views.Panel;
using Minditful.Core.Engine;
using Minditful.Integrations;
using Minditful.Integrations.Live;
using static Minditful.App.Rendering.Ui;

namespace Minditful.App.Views;

/// <summary>
/// Bảng điều khiển của Sandbox/Production: kết nối Microsoft 365, Azure Boards, Claude; Milo đang thấy dữ liệu gì;
/// tủ đồ, góc neo, tính năng chăm sóc, dữ liệu trên máy. Sandbox có thêm trang "Thử tình huống".
/// </summary>
internal sealed class ControlCenterWindow : ControlShell
{
    private readonly LiveSession _session;
    private readonly bool _sandbox;

    protected override MiloEngine Engine => _session.Engine;
    protected override string ClockNote =>
        Engine.Day.ToDateTime(TimeOnly.MinValue).ToString("dddd · dd/MM", new CultureInfo("vi-VN")) + $" · giờ làm {Tm.Hm(Engine.Cfg.Start)}–{Tm.Hm(Engine.Cfg.End)}";

    public ControlCenterWindow(LiveSession session)
        : base($"Minditful · {(session.Env == AppEnvironment.Sandbox ? "Sandbox" : "Production")}",
            session.Env == AppEnvironment.Sandbox ? "SANDBOX · TENANT THỬ" : "PRODUCTION · BOSCH",
            session.Env == AppEnvironment.Sandbox ? "#2E7D6B" : "#0F6CBD",
            session.Env == AppEnvironment.Sandbox
                ? "Tài khoản và dữ liệu thật của tenant thử. Ngưỡng được rút ngắn để test trong 1 buổi."
                : "Teams, Outlook, Azure Boards thật của bạn. Ngưỡng chuẩn theo tài liệu.")
    {
        _session = session;
        _sandbox = session.Env == AppEnvironment.Sandbox;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        AddPage("home", IcHome, "Tổng quan", Home);
        AddPage("connect", IcLink, "Kết nối", Connect, "Microsoft 365, Azure Boards, Claude");
        if (_sandbox) AddPage("try", IcTry, "Thử tình huống", Try, "cho Milo làm ngay");
        AddPage("milo", IcMilo, "Milo của bạn", Milo, "tủ đồ, chăm sóc, dữ liệu");
        AddPage("brain", IcBrain, "Bộ não Milo", BrainPage, "nâng cao");
        _session.Changed += OnChanged;
        Closed += (_, _) => _session.Changed -= OnChanged;
        Show("home");
    }

    private void OnChanged() => Dispatcher.BeginInvoke(Refresh);

    // ================= trạng thái kết nối =================
    private (string, string) GraphState()
    {
        var s = _session.GraphStatus;
        return s.StartsWith("Đã đăng nhập", StringComparison.Ordinal) ? (s.Contains("THIẾU") ? "warn" : "ok", s)
            : s.StartsWith("Lỗi", StringComparison.Ordinal) ? ("bad", s) : ("warn", s);
    }

    private (string, string) BoardsState() =>
        _session.Boards is null ? ("idle", "Chưa cấu hình Azure DevOps (organization/project trong .env hoặc appsettings.json).")
        : Engine.Snap.BoardsAvailable ? ("ok", _session.BoardsStatus)
        : _session.HasPat ? ("bad", "Chưa đọc được Azure Boards: " + (Engine.Snap.StatusNote ?? _session.BoardsStatus))
        : ("warn", "Chưa có PAT. Dán PAT ở trang Kết nối hoặc điền trong .env.");

    private (string, string) ClaudeState()
    {
        var first = _session.LlmStatus.Split('\n')[0];
        return first.StartsWith("Đang dùng", StringComparison.Ordinal) ? ("ok", first + " Milo viết lời thoại và chấm mood bằng Claude.")
            : ("idle", first + " Không bắt buộc: không có key Milo dùng câu mẫu và luật.");
    }

    // ================= Tổng quan =================
    private FrameworkElement Home()
    {
        var e = Engine;
        Border ConnCard(string title, Func<(string, string)> state, Button action) =>
            new()
            {
                Width = 290, Margin = new Thickness(0, 0, 10, 10), Padding = new Thickness(14, 12, 14, 12), CornerRadius = new CornerRadius(12),
                Background = Br(P.Fill),
                Child = new StackPanel { Children = { Text(title, 13.5, P.Ink, FontWeights.Bold), Spacer(6), Status(state), Spacer(10), action } },
            };
        var conns = new WrapPanel
        {
            Children =
            {
                ConnCard("Microsoft 365 · Teams, Outlook", GraphState, Btn("Đăng nhập Microsoft", async () => await _session.SignInAsync(true), BtnKind.Primary)),
                ConnCard("Azure Boards · task, sprint", BoardsState, Btn("Nhập PAT", () => Show("connect"))),
                ConnCard("Claude · lời thoại (tuỳ chọn)", ClaudeState, Btn("Nhập API key", () => Show("connect"), BtnKind.Ghost)),
            },
        };

        var next = Text("", 12.5, P.Ink2);
        Tick(() => next.Text = e.NextMeeting() is { } n ? $"Cuộc họp kế tiếp: {Tm.Hm(n.Start)} · {n.Subject}" : "Không còn cuộc họp nào hôm nay.");
        var seen = new StackPanel
        {
            Children =
            {
                Row(
                    Tile(() => $"{e.Meetings.Count(m => m.End <= e.S.T)}/{e.Meetings.Count}", "cuộc họp hôm nay"),
                    Tile(() => e.Snap.MailAvailable ? e.WaitingEmails().Count.ToString() : "—", "email đang chờ bạn", note: () => e.Snap.MailAvailable ? $"{e.Snap.Unread} chưa đọc" : "chưa có quyền đọc mail"),
                    Tile(() => e.Snap.BoardsAvailable ? e.InProgress().ToString() : "—", "task đang làm", note: () => e.StuckTasks().Count > 0 ? $"{e.StuckTasks().Count} task kẹt" : ""),
                    Tile(() => e.Snap.Sprint is { Total: > 0 } sp ? $"{sp.Done:0.#}/{sp.Total:0.#}" : "—", "điểm sprint", note: () => e.Snap.Sprint?.Name ?? ""),
                    Tile(() => e.S.Score.ToString(), "điểm mood", P.Accent, () => e.CurrentBand.Label)),
                next,
                Status(() => e.Snap.StatusNote is { } note ? ("warn", note) : ("ok", _session.LastRefresh is { } lr ? $"Dữ liệu cập nhật lúc {lr:HH:mm:ss}. Lịch làm mới mỗi 2 phút, mail 5 phút, Boards 3 phút." : "Đang tải dữ liệu…")),
                Row(Btn("Làm mới ngay", async () => await _session.RefreshAsync(), BtnKind.Ghost, IcRestart)),
            },
        };
        foreach (var c in seen.Children.OfType<FrameworkElement>().Skip(1)) c.Margin = new Thickness(0, 0, 0, 8);

        return Page(_sandbox ? "Milo đang chạy trên Sandbox" : "Milo đang làm việc cùng bạn",
            "Milo ở góc phải dưới màn hình. Trang này cho biết Milo đã kết nối được những gì và đang nhìn thấy dữ liệu nào. Chấm xanh là ổn, vàng là cần làm thêm 1 bước, đỏ là lỗi.",
            Card(conns, "Kết nối"),
            Card(seen, "Milo đang thấy", _session.WorkHoursText),
            TipBox(
                "Rê chuột lên chóp đuôi ở góc màn hình: Milo ló đầu. Bấm: mở dashboard 4 quả, bấm \"Chi tiết\" để xem thêm.",
                "Milo tự im lặng khi bạn đang họp, trình chiếu, toàn màn hình hoặc bật Không làm phiền.",
                "Đóng cửa sổ này Milo vẫn chạy. Muốn tắt hẳn: chuột phải biểu tượng chóp đuôi ở khay → Thoát Milo."));
    }

    // ================= Kết nối =================
    private FrameworkElement Connect()
    {
        var ms = Card(new StackPanel
        {
            Children =
            {
                Status(GraphState),
                Spacer(10),
                Row(Btn("Đăng nhập Microsoft", async () => await _session.SignInAsync(true), BtnKind.Primary),
                    Btn("Đăng xuất", async () => await _session.SignOutAsync(), BtnKind.Ghost),
                    Btn("Làm mới dữ liệu", async () => await _session.RefreshAsync(), BtnKind.Ghost, IcRestart)),
                Text(_sandbox ? "Đăng nhập bằng tài khoản thulu@mindiful.onmicrosoft.com. Lần đầu trình duyệt sẽ hỏi cấp quyền."
                    : "Đăng nhập bằng tài khoản Bosch. Nếu báo cần admin duyệt, gửi IT phần Production trong README.", 11.5, P.Muted),
            },
        }, "Microsoft 365", "Đọc lịch Teams/Outlook, email và trạng thái Teams. Giữ chỗ nghỉ / tập trung sẽ ghi vào lịch nếu có quyền.");

        var pat = new PasswordBox();
        System.Windows.Automation.AutomationProperties.SetName(pat, "Personal Access Token");
        var patHint = Text("", 11.5, P.Muted);
        Tick(() => patHint.Text = _session.Conn.AzureDevOps.Auth.Equals("Pat", StringComparison.OrdinalIgnoreCase)
            ? (_session.HasPat ? "Đã có PAT (lưu mã hoá trên máy này). Dán PAT mới để thay." : $"Chưa có PAT. Dán vào đây hoặc điền {_session.Conn.AzureDevOps.PatEnvVar} trong .env rồi mở lại app.")
            : "Azure DevOps dùng chung đăng nhập Microsoft, không cần PAT.");
        var boards = Card(new StackPanel
        {
            Children =
            {
                Status(BoardsState),
                Spacer(10),
                Label("Personal Access Token"),
                Spacer(4),
                Row(Input(pat), Btn("Lưu PAT", () =>
                {
                    _session.SavePat(pat.Password);
                    pat.Clear();
                }, BtnKind.Primary), Btn("Xoá PAT", _session.ClearPat, BtnKind.Ghost)),
                patHint,
            },
        }, "Azure Boards", $"Task đang làm, task kẹt, sprint. Org {_session.Conn.AzureDevOps.Organization} · project {_session.Conn.AzureDevOps.Project}.");

        var key = new PasswordBox();
        System.Windows.Automation.AutomationProperties.SetName(key, "API key Claude");
        var llm = Text("", 11.5, P.Ink2);
        Tick(() => llm.Text = _session.LlmStatus);
        var claude = Card(new StackPanel
        {
            Children =
            {
                Status(ClaudeState),
                Spacer(10),
                Label("API key"),
                Spacer(4),
                Row(Input(key), Btn("Lưu key", () =>
                {
                    _session.SaveClaudeKey(key.Password);
                    key.Clear();
                }, BtnKind.Primary)),
                llm,
                Text("Claude chỉ nhận tên tình huống và số liệu, không bao giờ nhận tiêu đề hay nội dung email, cuộc họp, task.", 11.5, P.Muted),
            },
        }, "Claude (tuỳ chọn)", "Viết lời thoại tự nhiên hơn, trả lời chat, chấm mood và đánh giá cuộc họp. Bật từng phần trong .env (README mục Tham chiếu biến .env).");

        return Page("Kết nối", "PAT và API key được lưu mã hoá trên máy này (DPAPI), không ghi vào file nào.", ms, boards, claude);
    }

    // ================= Thử tình huống (Sandbox) =================
    private FrameworkElement Try()
    {
        var e = Engine;
        var end = new TextBox { Text = Tm.Hm(e.Cfg.End) };
        System.Windows.Automation.AutomationProperties.SetName(end, "Giờ về");
        var seedLog = Text("", 11.5, P.Ink2);
        Button? seed = null;
        seed = Btn("Tạo dữ liệu mẫu", async () =>
        {
            seed!.IsEnabled = false;
            var lines = new List<string>();
            seedLog.Text = "Đang tạo…";
            try
            {
                await _session.Seeder.SeedAsync(DateTime.Now, new Progress<string>(l =>
                {
                    lines.Add("✓ " + l);
                    seedLog.Text = string.Join("\n", lines);
                }));
                lines.Add("Xong. Đang làm mới dữ liệu…");
                seedLog.Text = string.Join("\n", lines);
                await _session.RefreshAsync();
            }
            catch (Exception ex)
            {
                lines.Add("✗ " + LiveWorkDataProvider.Describe(ex));
                seedLog.Text = string.Join("\n", lines);
            }
            finally
            {
                seed.IsEnabled = true;
            }
        }, BtnKind.Primary);
        var prep = Card(new StackPanel
        {
            Children =
            {
                Row(Btn("Reset ngày (chào sáng lại)", () =>
                {
                    _session.ResetDay();
                    RefreshMilo();
                }, BtnKind.Soft, IcRestart)),
                Label("Giờ về hôm nay"),
                Spacer(4),
                Row(Input(end, 90), Btn("Đặt giờ về", () =>
                {
                    if (!_session.SetWorkEnd(end.Text)) MessageBox.Show("Nhập giờ dạng HH:mm, ví dụ 17:30.", "Minditful");
                }), Btn("Giờ về = bây giờ + 2 phút", () =>
                {
                    end.Text = DateTime.Now.AddMinutes(2).ToString("HH:mm");
                    _session.SetWorkEnd(end.Text);
                }, BtnKind.Ghost)),
                Text("Đặt giờ về sớm để xem ngay thẻ Tan tầm, nút \"Hôm nay thấy sao?\" và cảnh Milo chạy ra xe.", 11.5, P.Muted),
                Spacer(12),
                Row(seed),
                Text("Tạo trong tenant: 1 cuộc họp Teams sau 7 phút, chuỗi 3 cuộc họp liền, 6 work item giao cho bạn.", 11.5, P.Muted),
                seedLog,
                Spacer(8),
                Text("Ngưỡng đang dùng: " + (_session.OverridesText ?? "chuẩn theo tài liệu"), 11.5, P.Muted),
            },
        }, "Chuẩn bị", "Checklist đầy đủ ở docs/KET-NOI-SANDBOX.md mục 7.");

        var cases = Card(CaseList(c =>
        {
            e.ForceCase(c);
            Refresh();
        }), "Cho Milo làm ngay", "Bỏ qua điều kiện và giới hạn số lần nhắc. Bấm nút trên thẻ của Milo sẽ tác động thật: giữ chỗ tạo sự kiện Outlook, khoá tập trung bật Không làm phiền.");

        Border Sim(string tag, string label, string effect, Func<bool> on) =>
            Switch(label, effect, on, () => _session.Toggle(tag));
        var you = new WrapPanel
        {
            Children =
            {
                Sim("typing", "Đang gõ phím", "Milo chờ bạn dừng tay mới nói.", () => e.S.Typing),
                Sim("away", "Rời khỏi máy", "Milo không nói với màn hình trống.", () => e.S.Away),
                Sim("fullscreen", "Mở app toàn màn hình", "Milo im lặng, lời nhắc thành chấm chờ.", () => e.S.Fullscreen),
                Sim("dnd", "Teams: Không làm phiền", "Milo im lặng như trên.", () => e.S.UserDnd),
                Sim("presenting", "Đang trình chiếu", "Milo trốn hẳn, kể cả chóp đuôi.", () => e.S.Presenting),
                Switch("Ngày căng thẳng", "Trừ 30 điểm mood để xem Milo mệt.", () => e.S.Stress > 0, e.ToggleStress),
            },
        };
        var once = new WrapPanel
        {
            Children =
            {
                ActionTile("Rời cuộc họp", "Khi lịch đang có cuộc họp.", e.LeaveMeeting, e.InCall),
                ActionTile("Nhảy việc 12 lần", "Milo đề nghị gom việc.", e.SimulateFragmentation),
                ActionTile("Mở dashboard", "Như bấm vào chóp đuôi Milo.", () => ((App)Application.Current).Companion?.OpenDashboard()),
            },
        };
        return Page("Thử tình huống",
            "Công cụ để test Sandbox nhanh. Các công tắc \"giả vờ\" đè lên tín hiệu thật của máy tới khi bạn tắt.",
            prep, cases,
            Card(new StackPanel { Children = { you, Spacer(6), Label("Làm 1 lần"), Spacer(6), once } }, "Giả vờ bạn đang…"));
    }

    // ================= Milo của bạn =================
    private FrameworkElement Milo()
    {
        var e = Engine;
        var wb = _session.Wellbeing;
        StackPanel Feature(string name, bool on, string what)
        {
            var pill = Pill(on ? "Bật" : "Tắt", on ? "#E3EFD6" : "#EFE3D0", on ? "#3E5A22" : P.Ink2, 10.5);
            pill.Padding = new Thickness(8, 2, 8, 2);
            pill.VerticalAlignment = VerticalAlignment.Top;
            pill.Margin = new Thickness(0, 1, 10, 0);
            var t = Text(name, 13, P.Ink, FontWeights.SemiBold);
            var d = Text(what, 11.5, P.Ink2);
            var row = Columns((pill, Auto), (new StackPanel { Children = { t, d } }, Star));
            row.Margin = new Thickness(0, 0, 0, 10);
            return new StackPanel { Children = { row } };
        }
        var care = Card(new StackPanel
        {
            Children =
            {
                Feature("Giữ giờ tập trung", wb.FocusPlan, $"Đề nghị giữ khoảng trống dài nhất (≥ {wb.FocusPlanMinMinutes} phút) trong lịch, tới giờ tự bật Không làm phiền."),
                Feature("Báo cáo tuần", wb.WeekReport, "Sáng thứ Hai tóm tắt tuần trước và gợi ý 1 điều cho tuần mới."),
                Feature("Uống nước · nhìn xa", wb.MicroBreakEveryMinutes > 0,
                    wb.MicroBreakEveryMinutes > 0 ? $"Mỗi {wb.MicroBreakEveryMinutes} phút ngồi máy liên tục, tối đa {wb.MicroBreakMaxPerDay} lần/ngày." : "Đang tắt."),
                Feature("Hôm nay thấy sao? · nghỉ ngày mai", wb.EveningCheck, "Thẻ tan tầm hỏi cảm nhận và gợi ý giữ chỗ nghỉ giữa chuỗi họp ngày mai."),
                Feature("Trốn khi trình chiếu", wb.HideWhenPresenting, "Teams báo đang trình chiếu thì Milo ẩn hẳn."),
                Feature("Tủ đồ", wb.Wardrobe, "Phụ kiện mới khi về đúng giờ nhiều ngày liền."),
                Text("Bật/tắt trong .env (nhóm Wellbeing, xem README mục Tham chiếu biến .env) rồi mở lại app.", 11.5, P.Muted),
            },
        }, "Milo chăm sóc bạn thế nào");

        var tuning = Text("", 12, P.Ink2);
        Tick(() => tuning.Text = Personalizer.Describe(e.Tuning));
        var storage = Text("", 12, P.Ink2);
        Tick(() => storage.Text = "Chỉ lưu số liệu (điểm, số phút, số lần), không lưu tiêu đề email/họp/task. " + _session.StorageText);
        var data = Card(new StackPanel
        {
            Children =
            {
                Label("Milo tự điều chỉnh theo 7 ngày gần nhất"), Spacer(4), tuning, Spacer(12),
                Label("Dữ liệu trên máy"), Spacer(4), storage, Spacer(10),
                Btn("Xoá toàn bộ dữ liệu thống kê ngay", () =>
                {
                    if (MessageBox.Show("Xoá toàn bộ điểm mỗi ngày, mẫu mood, log phản hồi, đánh giá cuộc họp và tủ đồ của môi trường này?\n(Không đụng tới đăng nhập, PAT hay API key.)",
                            "Minditful", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                        _session.WipeLocalData();
                }, BtnKind.Danger),
            },
        }, "Riêng tư & dữ liệu");

        return Page("Milo của bạn", "Phụ kiện, chỗ đứng, những gì Milo làm cho bạn và dữ liệu Milo giữ trên máy.",
            wb.Wardrobe
                ? WardrobeCard(_session.Env, () => e.Snap.Wardrobe?.Best ?? 0, () => _session.WardrobeText)
                : Card(Text("Tủ đồ đang tắt (Wellbeing.Wardrobe = false).", 12, P.Ink2), "Tủ đồ của Milo"),
            CornerCard(_session.Env), care, data);
    }

    private static Border Spacer(double h) => new() { Height = h };
}
