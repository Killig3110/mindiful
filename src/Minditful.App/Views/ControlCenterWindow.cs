using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Minditful.App.Services;
using Minditful.App.Views.Panel;
using Minditful.Core.Engine;
using Minditful.Integrations;
using Minditful.Integrations.Live;
using Minditful.Integrations.Storage;
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
    protected override Minditful.Integrations.Llm.IMiloLlm? Llm => _session.Writer;
    protected override LlmOptions? LlmOpts => _session.Llm;
    protected override string ClockNote =>
        Engine.Day.ToDateTime(TimeOnly.MinValue).ToString("dddd · dd/MM", new CultureInfo("vi-VN")) + $" · giờ làm {Tm.Hm(Engine.Cfg.Start)}–{Tm.Hm(Engine.Cfg.End)}";

    public ControlCenterWindow(LiveSession session)
        : base($"Minditful · {(session.Env == AppEnvironment.Sandbox ? "Sandbox" : "Production")}",
            session.Env == AppEnvironment.Sandbox ? "SANDBOX" : "PRODUCTION · BOSCH",
            session.Env == AppEnvironment.Sandbox ? "#2E7D6B" : "#0F6CBD",
            "Teams, Outlook, Azure Boards thật của bạn. Ngưỡng chuẩn theo tài liệu.")
    {
        _session = session;
        _sandbox = session.Env == AppEnvironment.Sandbox;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        AddPage("home", IcHome, "Tổng quan", Home);
        AddPage("connect", IcLink, "Kết nối", Connect, "Microsoft 365, Azure Boards, Claude");
        if (_sandbox) AddPage("try", IcTry, "Thử tình huống", Try, "ép Milo làm như Demo");
        AddPage("milo", IcMilo, "Milo của bạn", Milo, "tủ đồ, chăm sóc, dữ liệu");
        AddPage("engine", IcEngine, "Mood Engine", MoodEnginePage, "luật ↔ AI · kiểm chứng");
        AddPage("check", IcCheck, "Kiểm chứng điểm", Check, "WHO-5 hằng tuần");
        AddPage("brain", IcBrain, "Bộ não Milo", BrainPage, "nâng cao");
        _session.Changed += OnChanged;
        Closed += (_, _) => _session.Changed -= OnChanged;
        Show("home");
    }

    private void OnChanged() => Dispatcher.BeginInvoke(Refresh);

    private bool? _shownMode;

    /// <summary>Sandbox: nhãn môi trường và trang "Thử tình huống" đi theo chế độ đang chọn.</summary>
    protected override void OnRefresh()
    {
        if (!_sandbox || _shownMode == _session.TestMode) return;
        _shownMode = _session.TestMode;
        if (_session.TestMode)
            SetBadge("SANDBOX · CHẾ ĐỘ TEST", "#2E7D6B", "Tenant thử, dữ liệu thật. Có công cụ ép Milo làm từng tình huống như Demo, ngưỡng rút ngắn để test trong 1 buổi.");
        else
            SetBadge("SANDBOX · NHƯ PRODUCTION", "#0F6CBD", "Tenant thử nhưng Milo chạy y như Production: ngưỡng chuẩn, không có công cụ test, không giả lập tín hiệu.");
        SetPageVisible("try", _session.TestMode);
    }

    /// <summary>Công tắc 2 chế độ của Sandbox: giao thoa giữa Demo (ép Milo làm) và Production (để Milo tự chạy).</summary>
    private Border ModeCard()
    {
        var seg = Segmented([("Chế độ test (như Demo)", "test"), ("Chạy như Production", "prod")],
            () => _session.TestMode ? "test" : "prod", v => _session.SetTestMode(v == "test"));
        StackPanel Col(string title, string color, params string[] lines)
        {
            var sp = new StackPanel { Margin = new Thickness(0, 0, 16, 0) };
            var h = Text(title, 12.5, color, FontWeights.Bold);
            h.Margin = new Thickness(0, 0, 0, 4);
            sp.Children.Add(h);
            foreach (var l in lines)
            {
                var x = Text("• " + l, 12, P.Ink2);
                x.Margin = new Thickness(0, 0, 0, 2);
                sp.Children.Add(x);
            }
            return sp;
        }
        var compare = Grid2(
            Col("Chế độ test", "#2E7D6B", "Có trang Thử tình huống: ép Milo làm bất kỳ tình huống nào ngay", "Giả vờ đang gõ, rời máy, trình chiếu…", "Ngưỡng rút ngắn: ngồi liền 20' đã nhắc, 3' giữa 2 lời nhắc", "Bảng điều khiển tự mở khi chạy app"),
            Col("Như Production", "#0F6CBD", "Milo tự chạy theo lịch, email, task thật", "Ngưỡng chuẩn: ngồi liền 2 tiếng mới nhắc, 15' giữa 2 lời nhắc", "Bỏ mọi tín hiệu giả lập", "Dùng để xem bản Production trông thế nào trước khi lên tenant Bosch"));
        compare.Margin = new Thickness(0, 4, 0, 0);
        var share = Switch("Hiện Milo khi chia sẻ màn hình",
            "Để demo qua Teams: người xem thấy Milo, và trình chiếu không làm Milo trốn. Tắt = ẩn như Production.",
            () => _session.ShowOnShare, () => _session.SetShowOnShare(!_session.ShowOnShare));
        share.Margin = new Thickness(0, 12, 0, 0);
        share.Width = double.NaN;
        return Card(new StackPanel { Children = { seg, compare, share } }, "Chế độ Sandbox",
            "Sandbox là giao thoa giữa Demo và Production. Đổi lúc nào cũng được, không cần mở lại app; menu khay cũng có công tắc này.");
    }

    // ================= trạng thái kết nối =================
    private (string, string) GraphState()
    {
        var s = _session.GraphStatus;
        return s.StartsWith("Đã đăng nhập", StringComparison.Ordinal) ? (s.Contains("THIẾU") ? "warn" : "ok", s)
            : s.StartsWith("Lỗi", StringComparison.Ordinal) ? ("bad", s) : ("warn", s);
    }

    /// <summary>Bản ngắn cho trang Tổng quan: bỏ danh sách quyền, giữ phần THIẾU nếu có.</summary>
    private (string, string) GraphShort()
    {
        var (lvl, s) = GraphState();
        var i = s.IndexOf(" · quyền:", StringComparison.Ordinal);
        if (i < 0) return (lvl, s);
        var missing = s.IndexOf("THIẾU", StringComparison.Ordinal);
        return (lvl, s[..i] + (missing >= 0 ? " · " + s[missing..] : ""));
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
                Width = 236, Margin = new Thickness(0, 0, 10, 10), Padding = new Thickness(14, 12, 14, 12), CornerRadius = new CornerRadius(12),
                Background = Br("#FFFFFF"), BorderBrush = Br(P.Line), BorderThickness = new Thickness(1),
                Child = new StackPanel { Children = { Text(title, 13.5, P.Ink, FontWeights.Bold), Spacer(6), Status(state), Spacer(10), action } },
            };
        var conns = new WrapPanel
        {
            Children =
            {
                ConnCard("Microsoft 365 · Teams, Outlook", GraphShort, Btn("Đăng nhập Microsoft", async () => await _session.SignInAsync(true), BtnKind.Primary)),
                ConnCard("Azure Boards · task, sprint", BoardsState, Btn("Nhập PAT", () => Show("connect"))),
                ConnCard("AI · lời thoại, chấm mood (tuỳ chọn)", ClaudeState, Btn("Nhập API key", () => Show("connect"), BtnKind.Ghost)),
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

        var cards = new List<UIElement>();
        if (_sandbox) cards.Add(ModeCard());
        cards.Add(Card(conns, "Kết nối"));
        return Page(_sandbox ? "Milo đang chạy trên Sandbox" : "Milo đang làm việc cùng bạn",
            "Milo ở góc phải dưới màn hình. Trang này cho biết Milo đã kết nối được những gì và đang nhìn thấy dữ liệu nào. Chấm xanh là ổn, vàng là cần làm thêm 1 bước, đỏ là lỗi.",
            [.. cards,
            Card(seen, "Milo đang thấy", _session.WorkHoursText),
            TipBox(
                "Rê chuột lên chóp đuôi ở góc màn hình: Milo ló đầu. Bấm: mở dashboard 4 quả, bấm \"Chi tiết\" để xem thêm.",
                "Milo tự im lặng khi bạn đang họp, trình chiếu, toàn màn hình hoặc bật Không làm phiền.",
                "Đóng cửa sổ này Milo vẫn chạy. Muốn tắt hẳn: chuột phải biểu tượng chóp đuôi ở khay → Thoát Milo.")]);
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
        System.Windows.Automation.AutomationProperties.SetName(key, "API key AI");
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
        }, "AI (tuỳ chọn)", "Claude hoặc AI tương thích OpenAI (Ollama trên máy, Groq, Gemini…): viết lời thoại, trả lời chat, chấm mood, đánh giá cuộc họp. Chọn nhà cung cấp trong .env (README mục Chọn AI để test); bật/tắt chấm mood bằng AI ở trang Mood Engine.");

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
            "Ép Milo làm từng hành động như ở Demo, nhưng trên dữ liệu và tài khoản thật. Các công tắc \"giả vờ\" đè lên tín hiệu thật của máy tới khi bạn tắt. Muốn xem Milo tự chạy như Production: Tổng quan → Chế độ Sandbox → Chạy như Production.",
            prep, cases,
            Card(new StackPanel { Children = { you, Spacer(6), Label("Làm 1 lần"), Spacer(6), once } }, "Giả vờ bạn đang…"));
    }

    /// <summary>Công tắc khởi động cùng Windows (ghi HKCU\...\Run, không cần quyền admin).</summary>
    private Border StartupCard()
    {
        var env = _session.Env;
        var note = Text("", 11.5, P.Ink2);
        void Explain() => note.Text = AutoStart.OtherEnvironment(env) is { } other
            ? $"Đang tự khởi động bản {other}. Bật ở đây sẽ chuyển sang {env} (mỗi lần chỉ 1 môi trường)."
            : "Tắt thì bạn tự mở Milo khi cần. Menu khay cũng có công tắc này.";
        Explain();
        Tick(Explain);
        return Card(new StackPanel
        {
            Children =
            {
                Switch("Khởi động cùng Windows", $"Đăng nhập Windows là Milo ({env}) tự chạy, không hiện màn hình chọn môi trường.",
                    () => AutoStart.IsEnabled(env),
                    () =>
                    {
                        if (AutoStart.Set(env, !AutoStart.IsEnabled(env)) is { } err) MessageBox.Show(err, "Minditful");
                        Explain();
                    }),
                note,
            },
        }, "Khởi động");
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
                Feature("Nghỉ ngắn · uống nước", wb.MicroBreakEveryMinutes > 0,
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
                ? WardrobeCard(_session.Env, () => _session.Engine.Snap.Wardrobe, () => _session.Engine.Day)
                : Card(Text("Tủ đồ đang tắt (Wellbeing.Wardrobe = false).", 12, P.Ink2), "Tủ đồ của Milo"),
            PersonalityCard(_session.Env, _session.Engine), CornerCard(_session.Env), StartupCard(), care, data);
    }

    // ================= Kiểm chứng điểm (WHO-5) =================
    /// <summary>
    /// Mỗi tuần người dùng tự trả lời 5 câu WHO-5, Milo so với điểm trung bình tuần của mình (tương quan Pearson).
    /// Đây là cách chứng minh thực tế công thức có đúng với người thật (docs/CO-SO-KHOA-HOC.md mục 5).
    /// </summary>
    private FrameworkElement Check()
    {
        var store = _session.History;
        var answers = new int?[5];
        var week = LocalStore.Who5Week(Engine.Day);
        var form = new StackPanel();
        for (var i = 0; i < Who5.Items.Length; i++)
        {
            var idx = i;
            var q = Text($"{i + 1}. {Who5.Items[i]}", 13, P.Ink, FontWeights.SemiBold);
            q.Margin = new Thickness(0, 6, 0, 4);
            form.Children.Add(q);
            form.Children.Add(Segmented(
                Enumerable.Range(0, 6).Select(v => ($"{v} · {Who5.Scale[v]}", v.ToString())).ToArray(),
                () => answers[idx]?.ToString() ?? "", v => answers[idx] = int.Parse(v)));
        }
        var saved = Text("", 12.5, P.Good, FontWeights.SemiBold);
        form.Children.Add(Row(Btn("Lưu câu trả lời", () =>
        {
            if (answers.Any(a => a is null))
            {
                saved.Text = "Trả lời đủ 5 câu giúp Milo nhé.";
                saved.Foreground = Br(P.Bad);
                return;
            }
            var pct = Who5.Percent(answers.Select(a => a!.Value).ToList());
            store.SaveWho5(week, pct);
            saved.Text = $"Đã lưu: WHO-5 tuần {week:dd/MM} = {pct}/100.";
            saved.Foreground = Br(P.Good);
            Refresh();
        }, BtnKind.Primary)));
        form.Children.Add(saved);

        var table = new StackPanel();
        var verdict = Text("", 13, P.Ink, FontWeights.SemiBold);
        var rText = Text("", 22, P.Accent, FontWeights.Bold, false);
        var lastCount = -1;
        Tick(() =>
        {
            var rows = store.ValidationRows();
            if (rows.Count == lastCount) return; // đọc SQLite chỉ khi có thay đổi
            lastCount = rows.Count;
            table.Children.Clear();
            table.Children.Add(Columns((Label("Tuần"), Px(120)), (Label("Điểm Milo TB"), Px(140)), (Label("WHO-5"), Star)));
            foreach (var (w, milo, who5) in rows)
                table.Children.Add(Columns((Text($"{w:dd/MM/yyyy}", 12.5, P.Ink, null, false), Px(120)),
                    (Text(milo is { } m ? $"{m:0}" : "—", 12.5, P.Ink, null, false), Px(140)), (Text($"{who5}", 12.5, P.Ink, null, false), Star)));
            if (rows.Count == 0) table.Children.Add(Text("Chưa có tuần nào. Trả lời 5 câu ở trên vào cuối mỗi tuần.", 12, P.Muted));
            var pairs = rows.Where(x => x.MiloAvg is not null).ToList();
            var r = Core.Engine.Validation.Pearson(pairs.Select(x => x.MiloAvg!.Value).ToList(), pairs.Select(x => (double)x.Who5).ToList());
            rText.Text = r is { } v ? $"r = {v:0.00}" : "r = —";
            verdict.Text = Core.Engine.Validation.Interpret(r, pairs.Count);
        });
        var export = Text("", 11.5, P.Ink2);
        var result = Card(new StackPanel
        {
            Children =
            {
                Row(rText), verdict, Spacer(10), table, Spacer(10),
                Row(Btn("Xuất CSV ẩn danh (gộp cả nhóm)", () => export.Text = "Đã lưu " + ExportCsv(store.ValidationRows()), BtnKind.Ghost)),
                export,
                Text("CSV chỉ có mã người dùng ngẫu nhiên, ngày đầu tuần, điểm Milo TB và điểm WHO-5. Gộp CSV của cả nhóm rồi tính tương quan (Excel: =CORREL(cột diem_milo; cột who5)).", 11.5, P.Muted),
            },
        }, "Điểm Milo có khớp cảm nhận thật không?", "Tương quan Pearson giữa điểm Milo trung bình tuần và điểm WHO-5 của chính bạn. Có ý nghĩa sau khoảng 4 tuần.");

        var thisWeek = week == LocalStore.WeekStart(Engine.Day);
        return Page("Kiểm chứng điểm",
            "Nghiên cứu cho biết chiều tác động (họp nhiều thì mệt, nghỉ ngắn thì đỡ), còn con số cụ thể phải kiểm chứng với người thật. Mỗi cuối tuần bạn tự trả lời 5 câu WHO-5 (thang đo sức khoẻ tinh thần của Tổ chức Y tế Thế giới), Milo so với điểm của mình. Đây không phải công cụ chẩn đoán.",
            Card(form, thisWeek ? $"WHO-5 · tuần này (từ {week:dd/MM})" : $"WHO-5 · tuần trước (từ {week:dd/MM})",
                "Trong tuần qua, bạn thấy những điều sau đúng với mình tới mức nào? 0 = không lúc nào, 5 = mọi lúc."),
            result);
    }

    private string ExportCsv(IReadOnlyList<(DateOnly Week, double? MiloAvg, int Who5)> rows)
    {
        var idFile = System.IO.Path.Combine(AppPaths.For(_session.Env), "pilot-id.txt");
        if (!System.IO.File.Exists(idFile)) System.IO.File.WriteAllText(idFile, Guid.NewGuid().ToString("N")[..8]);
        var id = System.IO.File.ReadAllText(idFile).Trim();
        var sb = new System.Text.StringBuilder("nguoi,tuan,diem_milo,who5\n");
        foreach (var (w, m, s) in rows)
            sb.Append(CultureInfo.InvariantCulture, $"{id},{w:yyyy-MM-dd},{(m is { } v ? v.ToString("0.0", CultureInfo.InvariantCulture) : "")},{s}\n");
        var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"milo-kiem-chung-{id}.csv");
        System.IO.File.WriteAllText(path, sb.ToString());
        return path;
    }

}
