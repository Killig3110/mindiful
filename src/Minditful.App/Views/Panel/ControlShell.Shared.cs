using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Minditful.App.Controls;
using Minditful.App.Rendering;
using Minditful.App.Services;
using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Integrations;
using Minditful.Integrations.Llm;
using static Minditful.App.Rendering.Ui;

namespace Minditful.App.Views.Panel;

/// <summary>Các khối dùng chung cho cả Demo và Sandbox/Production.</summary>
internal abstract partial class ControlShell
{
    /// <summary>Mỗi case: nhóm + 1 câu "Milo sẽ làm gì" bằng lời thường.</summary>
    protected static readonly (string Group, string Hint, (CaseId C, string What)[] Cases)[] CaseCatalog =
    [
        ("Chào hỏi", "Milo xuất hiện đầu ngày, cuối ngày và thỉnh thoảng hỏi thăm.",
        [
            (CaseId.MorningHello, "Leo lên chào + bản tin: mấy cuộc họp, email chưa đọc, task đang dở"),
            (CaseId.CheckIn, "Hỏi thăm khi bạn đang làm tốt, 1 nút \"Cảm ơn Milo\""),
            (CaseId.EodWrapup, "Tổng kết ngày, hỏi hôm nay thấy sao, rủ về"),
            (CaseId.EodNudge, "Nhắc lại sau 30 phút làm thêm"),
        ]),
        ("Giúp việc", "Nhắc đúng lúc về lịch họp, email và task.",
        [
            (CaseId.MeetingSoon, "5 phút trước họp Teams: mở slide, bấm vào họp"),
            (CaseId.CalendarPacked, "Thấy họp liền nhau, đề nghị giữ 10 phút nghỉ trong lịch"),
            (CaseId.EmailWaiting, "Email hỏi thẳng bạn mà chưa trả lời"),
            (CaseId.StuckTask, "Task dở nhiều ngày → khoá giờ tập trung + Không làm phiền"),
            (CaseId.TaskDone, "Nhảy tưng ăn mừng khi 1 task sang Done"),
            (CaseId.FocusDone, "Chúc mừng khi hết khối tập trung"),
        ]),
        ("Chăm sóc", "Khi bạn làm quá sức. Mỗi lần chỉ nhắc 1 chuyện.",
        [
            (CaseId.MeetingOverload, "Họp liền mấy tiếng → rủ thở 4-4-4"),
            (CaseId.NoBreak, "Ngồi liền lâu không nghỉ → thở 1 phút"),
            (CaseId.LowRest, "Cả ngày nghỉ quá ít → nghỉ hẳn 15 phút"),
            (CaseId.LunchMissed, "Quá trưa chưa rời máy → rủ đi ăn"),
            (CaseId.HighFragmentation, "Nhảy việc liên tục → gom việc 30 phút"),
            (CaseId.Overtime, "Quá giờ về → nhắc chốt việc rồi về"),
        ]),
        ("Mới thêm", "Tính năng mở rộng ngoài kịch bản gốc.",
        [
            (CaseId.FocusPlan, "Đề nghị giữ khoảng trống dài nhất trong ngày để tập trung"),
            (CaseId.WeekReport, "Sáng thứ Hai: tóm tắt tuần trước + 1 mẹo"),
            (CaseId.MicroBreak, "Ló lên 5 giây nhắc nghỉ ngắn: uống nước, vươn vai"),
            (CaseId.Dashboard, "Mở 4 quả quanh Milo (nho, cam, anh đào, táo)"),
        ]),
    ];

    /// <summary>Danh sách case chia nhóm, bấm là Milo làm ngay trên desktop.</summary>
    protected StackPanel CaseList(Action<CaseId> run)
    {
        var sp = new StackPanel();
        foreach (var (group, hint, cases) in CaseCatalog)
        {
            var head = Text(group, 14, P.Ink, FontWeights.Bold);
            var h = Text(hint, 11.5, P.Ink2);
            h.Margin = new Thickness(0, 2, 0, 8);
            var wrap = new WrapPanel();
            foreach (var (c, what) in cases)
            {
                var cc = c;
                wrap.Children.Add(ActionTile(Catalog.Def(c).Name, what, () => run(cc), color: Catalog.Def(c).Color));
            }
            sp.Children.Add(new StackPanel { Margin = new Thickness(0, 0, 0, 10), Children = { head, h, wrap } });
        }
        return sp;
    }

    /// <summary>Tủ đồ: 5 lựa chọn, món chưa mở khoá bị mờ. Mỗi món có ảnh Milo đang mặc.</summary>
    protected Border WardrobeCard(AppEnvironment env, Func<int> best, Func<string> progress)
    {
        (string Id, string Name, int Need)[] items = [("auto", "Tự chọn món mới nhất", 0), ("none", "Không mặc", 0),
            .. Wardrobe.Items.Select(i => (i.Id, $"{char.ToUpper(i.Name[0])}{i.Name[1..]}", i.Streak))];
        var wrap = new WrapPanel();
        var boxes = new List<(Border Box, string Id, int Need, TextBlock Lock)>();
        foreach (var (id, name, need) in items)
        {
            var acc = Wardrobe.Find(id)?.Id;
            var img = new Image
            {
                Width = 74, Height = 74, HorizontalAlignment = HorizontalAlignment.Center,
                Source = id == "none" || id == "auto" ? MiloSkin.Get(Pose.Idle, 1) : MiloSkin.Frame(Pose.Idle, MiloRig.Idle, 0, false, 1, acc),
            };
            var title = Text(name, 12, P.Ink, FontWeights.SemiBold);
            title.TextAlignment = TextAlignment.Center;
            var lockText = Text(need > 0 ? $"{need} ngày về đúng giờ" : id == "auto" ? "mặc định" : "", 10.5, P.Muted);
            lockText.TextAlignment = TextAlignment.Center;
            var box = new Border
            {
                Width = 128, Padding = new Thickness(8, 8, 8, 10), Margin = new Thickness(0, 0, 8, 8), CornerRadius = new CornerRadius(14),
                Background = Br(P.Fill), BorderThickness = new Thickness(2), Cursor = System.Windows.Input.Cursors.Hand,
                Child = new StackPanel { Children = { img, title, lockText } },
            };
            System.Windows.Automation.AutomationProperties.SetName(box, name);
            var chosen = id;
            box.MouseLeftButtonUp += (_, _) =>
            {
                if (need > best()) return;
                ((App)Application.Current).Companion?.SetAccessory(chosen);
                Refresh();
            };
            boxes.Add((box, id, need, lockText));
            wrap.Children.Add(box);
        }
        var prog = Text("", 12.5, P.Ink2);
        prog.Margin = new Thickness(0, 4, 0, 0);
        Tick(() =>
        {
            var b = best();
            var cur = UiSettings.LoadAccessory(env);
            foreach (var (box, id, need, lk) in boxes)
            {
                var open = need <= b;
                box.Opacity = open ? 1 : .45;
                box.BorderBrush = Br(id == cur ? P.Accent : "#00000000");
                box.Background = Br(id == cur ? "#FFF1DE" : P.Fill);
                lk.Text = need == 0 ? (id == "auto" ? "mặc định" : "") : open ? "đã mở khoá" : $"cần {need} ngày về đúng giờ";
            }
            prog.Text = progress();
        });
        return Card(new StackPanel { Children = { wrap, prog } }, "Tủ đồ của Milo",
            "Về đúng giờ (quá giờ dưới 15 phút) nhiều ngày liền để mở khoá. Bấm 1 món để Milo mặc ngay.");
    }

    protected Border CornerCard(AppEnvironment env) =>
        Card(Segmented(
            [("↖ Trên trái", nameof(Corner.TopLeft)), ("↗ Trên phải", nameof(Corner.TopRight)), ("↙ Dưới trái", nameof(Corner.BottomLeft)), ("↘ Dưới phải", nameof(Corner.BottomRight))],
            () => UiSettings.LoadCorner(env).ToString(),
            v => ((App)Application.Current).Companion?.SetCorner(Enum.Parse<Corner>(v))),
            "Milo ở góc nào", "Cũng có thể kéo chóp đuôi Milo sang góc khác ngay trên desktop.");

    /// <summary>Trang nâng cao: trạng thái, lý do im lặng, hàng đợi, cách tính mood, nhật ký quyết định.</summary>
    protected FrameworkElement BrainPage()
    {
        var brain = new BrainPanel { Margin = new Thickness(0, 0, 10, 14) };
        Tick(() => brain.Render(Engine));
        return Page("Bộ não Milo",
            "Dành cho người muốn xem bên trong: Milo đang ở trạng thái nào, vì sao đang im lặng, lời nhắc nào đang chờ, điểm mood được tính ra sao và nhật ký từng quyết định.",
            brain);
    }

    // ================= Mood Engine: công tắc luật ↔ AI + 3 bộ kiểm chứng =================
    protected const string IcEngine = "";

    /// <summary>LLM của phiên (Claude hoặc OpenAI-compatible) và cấu hình; null thì trang Mood Engine chỉ có phần luật.</summary>
    protected virtual IMiloLlm? Llm => null;
    protected virtual LlmOptions? LlmOpts => null;

    private SuiteReport? _rulesReport, _aiReport, _compareReport;
    private CancellationTokenSource? _evalCts;

    protected FrameworkElement MoodEnginePage()
    {
        var e = Engine;
        var llm = Llm;
        var opts = LlmOpts;
        var ready = llm is { Available: true } && opts is { Enabled: true };

        // ---- công tắc ----
        var status = Status(() => llm is null ? ("idle", "Chưa cấu hình AI.")
            : !(opts?.Enabled ?? false) ? ("idle", "AI đang tắt (Llm.Enabled = false).")
            : llm.Available ? ("ok", $"Sẵn sàng: {llm.Name} · đã gửi {LlmBridge.Requests} request từ lúc mở app")
            : ("warn", $"Chưa dùng được {llm.Name}: " + (llm.LastError ?? "thiếu API key hoặc BaseUrl. Xem README mục Chọn AI để test.")));
        var hint = Text("", 12, P.Bad);
        void Pick(MoodMode? mood, MeetingMode? meeting)
        {
            if (llm is null || opts is null) return;
            var want = (mood ?? e.Cfg.MoodMode, meeting ?? e.Cfg.MeetingMode);
            if (!ready && (want.Item1 != MoodMode.Rules || want.Item2 != MeetingMode.Rules))
            {
                hint.Text = "Chưa có AI nên vẫn chấm bằng luật. Thêm API key (hoặc chạy Ollama) rồi mở lại app.";
                return;
            }
            hint.Text = "";
            LlmBridge.SetModes(e, llm, opts, want.Item1, want.Item2);
            RefreshMilo();
        }
        var mood = Segmented([("Luật", nameof(MoodMode.Rules)), ("Luật + AI (±10 điểm)", nameof(MoodMode.Hybrid)), ("AI chấm hẳn", nameof(MoodMode.Llm))],
            () => e.Cfg.MoodMode.ToString(), v => Pick(Enum.Parse<MoodMode>(v), null));
        var meeting = Segmented([("Luật", nameof(MeetingMode.Rules)), ("AI", nameof(MeetingMode.Llm))],
            () => e.Cfg.MeetingMode.ToString(), v => Pick(null, Enum.Parse<MeetingMode>(v)));
        var now = Text("", 12.5, P.Ink);
        Tick(() =>
        {
            var s = e.S;
            now.Text = e.Cfg.MoodMode switch
            {
                MoodMode.Rules => $"Đang chấm bằng luật: {s.Score} điểm ({e.CurrentBand.Label}).",
                _ when s.MoodInsight is { } mi => $"Luật {s.RuleScore} · AI {(e.Cfg.MoodMode == MoodMode.Llm ? mi.Score : mi.Adjust)}"
                    + (e.Cfg.MoodMode == MoodMode.Hybrid ? " điểm chỉnh" : "") + $" → điểm dùng {s.Score} ({e.CurrentBand.Label}). AI nói: “{mi.Insight}”",
                _ => $"Đang chờ AI trả lời… tạm dùng luật: {s.Score} điểm.",
            };
        });
        var switchCard = Card(new StackPanel
        {
            Children =
            {
                status, Spacer(10),
                Label("Chấm điểm mood"), Spacer(4), mood,
                Label("Đánh giá mức nặng cuộc họp"), Spacer(4), meeting,
                Row(Btn("Hỏi AI chấm ngay", () =>
                {
                    if (e.Cfg.MoodMode == MoodMode.Rules) { hint.Text = "Chọn \"Luật + AI\" hoặc \"AI chấm hẳn\" trước."; return; }
                    e.AskMoodNow();
                }, BtnKind.Ghost)),
                hint, now,
                Text("Luật + AI: luật làm nền, AI chỉnh tối đa ±10 điểm và viết 1 câu nhận xét. AI chấm hẳn: AI cho điểm, luật dùng khi AI chưa trả lời. AI chỉ nhận con số, không nhận tiêu đề hay nội dung công việc.", 11.5, P.Muted),
            },
        }, "Luật hay AI?", "Đổi ngay lúc đang chạy. Điểm và dáng Milo cập nhật ở dải trên cùng.");

        // ---- 3 bộ kiểm chứng ----
        var repeats = 3;
        var results = new StackPanel();
        var progress = Text("", 12, P.Ink2);
        void Show()
        {
            results.Children.Clear();
            foreach (var rep in new[] { _rulesReport, _aiReport, _compareReport }.Where(r => r is not null)) results.Children.Add(ReportView(rep!));
        }
        var runAi = Btn($"2 + 3 · Kiểm chứng AI và so sánh (≈{MoodEvaluation.RequestsNeeded(repeats)} request)", () => { }, BtnKind.Primary);
        var stop = Btn("Dừng", () => _evalCts?.Cancel(), BtnKind.Ghost);
        stop.Visibility = Visibility.Collapsed;
        runAi.Click += async (_, _) =>
        {
            if (!ready || llm is null || opts is null)
            {
                progress.Text = "Cần AI để chạy bộ 2 và 3. Thêm API key hoặc chạy Ollama (README mục Chọn AI để test).";
                return;
            }
            _evalCts = new CancellationTokenSource();
            runAi.IsEnabled = false;
            stop.Visibility = Visibility.Visible;
            try
            {
                var run = await MoodEvaluation.RunLlmAsync(llm.Name, async (facts, ct) =>
                {
                    LlmBridge.Count();
                    var m = await llm.AssessMoodAsync(new MoodRequest(0, facts, []), ct);
                    return m?.Score;
                }, repeats, opts.EvalDelayMs, new Progress<string>(p => progress.Text = "Đang hỏi AI: " + p), _evalCts.Token);
                _aiReport = MoodEvaluation.LlmSuite(run);
                _compareReport = MoodEvaluation.Compare(run);
                progress.Text = $"Xong {run.Requests} request." + (llm.LastError is { } err ? " Lỗi gần nhất: " + err : "");
                Show();
            }
            catch (OperationCanceledException) { progress.Text = "Đã dừng."; }
            finally
            {
                runAi.IsEnabled = true;
                stop.Visibility = Visibility.Collapsed;
            }
        };
        var rep2 = Segmented([("Hỏi mỗi ngày 2 lần", "2"), ("3 lần", "3")], () => repeats.ToString(), v =>
        {
            repeats = int.Parse(v);
            ((System.Windows.Controls.TextBlock)((System.Windows.Controls.StackPanel)runAi.Content).Children[1]).Text = $"2 + 3 · Kiểm chứng AI và so sánh (≈{MoodEvaluation.RequestsNeeded(repeats)} request)";
        });
        var saved = Text("", 11.5, P.Ink2);
        var suiteCard = Card(new StackPanel
        {
            Children =
            {
                Text($"Hỏi cả luật lẫn AI về {MoodEvaluation.Scenarios.Length} ngày làm việc mẫu (nhẹ, bình thường, họp nhiều, họp liền, ngồi liền, quá giờ, tuần 55 giờ, nhảy việc, nghỉ đủ, kiệt sức). Chỉ gửi con số.", 12.5, P.Ink),
                Spacer(8),
                Row(Btn("1 · Chứng minh luật hợp lý", () =>
                {
                    _rulesReport = MoodEvaluation.RunRules();
                    Show();
                }, BtnKind.Soft)),
                Row(rep2),
                Row(runAi, stop),
                progress,
                Row(Btn("Lưu báo cáo (Markdown)", () =>
                {
                    var reps = new[] { _rulesReport, _aiReport, _compareReport }.Where(r => r is not null).Select(r => r!).ToArray();
                    if (reps.Length == 0) { saved.Text = "Chạy ít nhất 1 bộ trước."; return; }
                    var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"milo-kiem-chung-mood-{DateTime.Now:yyyyMMdd-HHmm}.md");
                    System.IO.File.WriteAllText(path, MoodEvaluation.ToMarkdown(DateTime.Now, reps));
                    saved.Text = "Đã lưu " + path;
                }, BtnKind.Ghost)),
                saved,
                Text("Bộ 1 chạy ngay, không cần mạng. Bộ 2 kiểm AI trả lời đúng dạng, hỏi lại vẫn ra gần như nhau (lệch ≤ 10 điểm) và xếp đúng thứ tự ngày nặng / nhẹ. Bộ 3 so điểm AI với luật: lệch trung bình, tương quan, cùng mức mood. Chi tiết: docs/CO-SO-KHOA-HOC.md.", 11.5, P.Muted),
            },
        }, "3 bộ kiểm chứng", "Chứng minh luật hợp lý · AI hợp lý và ổn định · so sánh luật với AI.");

        return Page("Mood Engine", "Chọn cách chấm điểm mood (luật hoặc AI) và chạy bộ kiểm chứng để thấy điểm có đáng tin không.",
            switchCard, suiteCard, results);
    }

    private static Border ReportView(SuiteReport rep)
    {
        var sp = new StackPanel();
        var head = Text((rep.Passed ? "✓ ĐẠT · " : "✗ CHƯA ĐẠT · ") + rep.Summary, 13, rep.Passed ? P.Good : P.Bad, FontWeights.Bold);
        head.Margin = new Thickness(0, 0, 0, 8);
        sp.Children.Add(head);
        foreach (var c in rep.Checks)
        {
            var mark = Text(c.Passed ? "✓" : "✗", 13, c.Passed ? P.Good : P.Bad, FontWeights.Bold, false);
            var name = Text(c.Name, 12.5, P.Ink, FontWeights.SemiBold);
            var detail = Text(c.Detail, 11.5, P.Ink2);
            var row = Columns((mark, Px(22)), (new StackPanel { Children = { name, detail } }, Star));
            row.Margin = new Thickness(0, 0, 0, 6);
            sp.Children.Add(row);
        }
        if (rep.Notes.Count > 0)
        {
            var notes = Text(string.Join("\n", rep.Notes), 11.5, P.Muted);
            notes.Margin = new Thickness(22, 4, 0, 0);
            sp.Children.Add(notes);
        }
        return Card(sp, rep.Title);
    }

    protected static Border Spacer(double h) => new() { Height = h };
}
