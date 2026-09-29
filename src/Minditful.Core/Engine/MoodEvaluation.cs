using System.Globalization;
using System.Text;

namespace Minditful.Core.Engine;

public sealed record CheckResult(string Name, bool Passed, string Detail);

public sealed record SuiteReport(string Title, string Summary, IReadOnlyList<CheckResult> Checks, IReadOnlyList<string> Notes)
{
    public bool Passed => Checks.All(c => c.Passed);
}

/// <summary>1 ngày làm việc mẫu dùng để hỏi cả luật lẫn AI (chỉ có con số, không có nội dung công việc).</summary>
public sealed record MoodScenario(string Id, string Name, MoodInputs Inputs);

/// <summary>Kết quả hỏi AI: mỗi tình huống hỏi lặp lại nhiều lần để đo độ ổn định. null = AI không trả lời / sai dạng.</summary>
public sealed record LlmRun(string Model, IReadOnlyDictionary<string, IReadOnlyList<int?>> Scores, int Requests);

/// <summary>
/// 3 bộ kiểm chứng cho điểm mood (docs/CO-SO-KHOA-HOC.md):
/// 1. Luật hợp lý: công thức luôn đúng chiều nghiên cứu với dữ liệu ngẫu nhiên, và xếp đúng thứ tự các ngày mẫu.
/// 2. AI hợp lý và ổn định: trả lời đúng dạng, hỏi lại nhiều lần vẫn ra gần như nhau, xếp đúng thứ tự các cặp ngày.
/// 3. So sánh luật với AI: lệch trung bình bao nhiêu điểm, tương quan, cùng mức mood, cùng thứ tự.
/// Bộ 1 chạy ngay không cần mạng. Bộ 2, 3 cần 1 LLM (Claude, Ollama, Groq, Gemini…) và tốn Scenarios × lặp request.
/// </summary>
public static class MoodEvaluation
{
    public static readonly MoodScenario[] Scenarios =
    [
        new("nhe", "Ngày nhẹ", new(MeetingMin: 60, LongestChain: 1, StreakMin: 40, WorkedMin: 480, RestMin: 60, SwitchesHour: 4,
            AcceptedBreaks: 2, TasksDone: 1, FocusBlocks: 1)),
        new("thuong", "Ngày bình thường", new(MeetingMin: 180, LongestChain: 2, StreakMin: 80, WorkedMin: 480, RestMin: 40, SwitchesHour: 6,
            AcceptedBreaks: 1, TasksDone: 1, WaitingEmails: 1)),
        new("hop-nhieu", "Họp nhiều", new(MeetingMin: 330, LongestChain: 3, StreakMin: 80, WorkedMin: 480, RestMin: 30, SwitchesHour: 6,
            AcceptedBreaks: 1, TasksDone: 1, WaitingEmails: 1)),
        new("hop-lien", "Họp liền không nghỉ", new(MeetingMin: 240, LongestChain: 5, StreakMin: 80, WorkedMin: 480, RestMin: 20, SwitchesHour: 6,
            AcceptedBreaks: 1, TasksDone: 1, WaitingEmails: 1)),
        new("lam-lien", "Ngồi liền không nghỉ", new(MeetingMin: 30, LongestChain: 1, StreakMin: 210, WorkedMin: 480, RestMin: 10, SwitchesHour: 6,
            TasksDone: 1, WaitingEmails: 1)),
        new("qua-gio", "Quá giờ 2 tiếng", new(MeetingMin: 180, LongestChain: 2, StreakMin: 100, OvertimeMin: 120, WorkedMin: 600, RestMin: 30,
            SwitchesHour: 6, AcceptedBreaks: 1, TasksDone: 1, WaitingEmails: 1, WeekOvertimeMin: 300)),
        new("tuan-55h", "Quá giờ 2 tiếng, cả tuần 55 giờ", new(MeetingMin: 180, LongestChain: 2, StreakMin: 100, OvertimeMin: 120, WorkedMin: 600,
            RestMin: 30, SwitchesHour: 6, AcceptedBreaks: 1, TasksDone: 1, WaitingEmails: 1, WeekOvertimeMin: 900)),
        new("nhay-viec", "Nhảy việc liên tục", new(MeetingMin: 180, LongestChain: 2, StreakMin: 80, WorkedMin: 480, RestMin: 40, SwitchesHour: 22,
            AcceptedBreaks: 1, TasksDone: 1, WaitingEmails: 1)),
        new("hoi-phuc", "Nghỉ đủ, tập trung tốt", new(MeetingMin: 180, LongestChain: 2, StreakMin: 30, WorkedMin: 480, RestMin: 70, SwitchesHour: 5,
            AcceptedBreaks: 4, TasksDone: 3, FocusBlocks: 2, WaitingEmails: 1)),
        new("kiet-suc", "Kiệt sức", new(MeetingMin: 420, LongestChain: 6, StreakMin: 180, OvertimeMin: 150, WorkedMin: 660, RestMin: 5, SwitchesHour: 20,
            WorkloadRatio: 2.2, StuckTasks: 3, WaitingEmails: 8, WeekOvertimeMin: 1000)),
    ];

    /// <summary>Cặp (dễ hơn, nặng hơn): ngày nặng hơn không được có điểm cao hơn. Kèm lý do từ nghiên cứu.</summary>
    public static readonly (string Easier, string Harder, string Why)[] Pairs =
    [
        ("nhe", "thuong", "nhiều họp hơn, ít nghỉ hơn"),
        ("thuong", "hop-nhieu", "họp trực tiếp/online nhiều gây mệt (Nurmi & Pakarinen, 2023)"),
        ("thuong", "hop-lien", "họp liền không nghỉ (Albulescu et al., 2022)"),
        ("thuong", "lam-lien", "ngồi liền không nghỉ ngắn (Albulescu et al., 2022; 2025)"),
        ("thuong", "qua-gio", "quá giờ mất thời gian hồi phục (Sonnentag et al., 2022)"),
        ("qua-gio", "tuan-55h", "> 48 giờ/tuần (Kim et al., 2024)"),
        ("thuong", "nhay-viec", "đa nhiệm, bị ngắt quãng (Becker et al., 2023)"),
        ("hoi-phuc", "thuong", "ít nguồn hồi phục hơn (JD-R, Bakker et al., 2023)"),
        ("thuong", "kiet-suc", "dồn mọi áp lực"),
    ];

    public static string Band(int score) => score >= 80 ? "Mọng" : score >= 60 ? "Cân bằng" : score >= 40 ? "Mệt dần" : "Kiệt sức";

    private static string Dur(double min) => Tm.Dur(min);

    /// <summary>Số liệu gửi AI: chỉ con số, không có điểm theo luật (để AI chấm độc lập khi so sánh).</summary>
    public static string Facts(MoodInputs x) => string.Join("; ", new[]
    {
        $"đã làm {Dur(x.WorkedMin)} hôm nay",
        $"họp tổng {Dur(x.MeetingMin)}, chuỗi họp liền dài nhất {x.LongestChain} cuộc",
        $"đang làm liền {Dur(x.StreakMin)} chưa nghỉ, tổng nghỉ hôm nay {x.RestMin:0} phút",
        $"quá giờ hôm nay {x.OvertimeMin:0} phút, quá giờ cả tuần {Dur(x.WeekOvertimeMin)}" + (x.EarlyStart ? ", bắt đầu sớm hơn giờ làm" : ""),
        $"chuyển việc {x.SwitchesHour} lần/giờ (bình thường tới {x.FragThreshold})",
        $"số task đang làm gấp {x.WorkloadRatio.ToString("0.0", CultureInfo.InvariantCulture)} lần mức thường, {x.StuckTasks} task kẹt, {x.WaitingEmails} email chờ trả lời",
        $"đã nghỉ ngắn {x.AcceptedBreaks} lần, {x.FocusBlocks} khối tập trung trọn vẹn, xong {x.TasksDone} task",
    });

    // ================= Bộ 1: luật =================
    private static readonly (string Name, Func<MoodInputs, double, MoodInputs> More)[] Demands =
    [
        ("giờ họp", (x, d) => x with { MeetingMin = x.MeetingMin + d * 120 }),
        ("chuỗi họp liền", (x, d) => x with { LongestChain = x.LongestChain + (int)Math.Ceiling(d * 4) }),
        ("làm liền", (x, d) => x with { StreakMin = x.StreakMin + d * 120 }),
        ("quá giờ", (x, d) => x with { OvertimeMin = x.OvertimeMin + d * 120 }),
        ("quá giờ cả tuần", (x, d) => x with { WeekOvertimeMin = x.WeekOvertimeMin + d * 600 }),
        ("nhảy việc", (x, d) => x with { SwitchesHour = x.SwitchesHour + (int)Math.Ceiling(d * 15) }),
        ("khối lượng việc", (x, d) => x with { WorkloadRatio = x.WorkloadRatio + d * 1.5 }),
        ("task kẹt", (x, d) => x with { StuckTasks = x.StuckTasks + (int)Math.Ceiling(d * 4) }),
        ("email chờ", (x, d) => x with { WaitingEmails = x.WaitingEmails + (int)Math.Ceiling(d * 6) }),
    ];

    private static readonly (string Name, Func<MoodInputs, double, MoodInputs> More)[] Resources =
    [
        ("thời gian nghỉ", (x, d) => x with { RestMin = x.RestMin + d * 60 }),
        ("lần nghỉ", (x, d) => x with { AcceptedBreaks = x.AcceptedBreaks + (int)Math.Ceiling(d * 3) }),
        ("khối tập trung", (x, d) => x with { FocusBlocks = x.FocusBlocks + (int)Math.Ceiling(d * 2) }),
        ("task xong", (x, d) => x with { TasksDone = x.TasksDone + (int)Math.Ceiling(d * 3) }),
    ];

    public static MoodInputs RandomInputs(Random r) => new(
        MeetingMin: r.Next(0, 600), LongestChain: r.Next(0, 9), StreakMin: r.Next(0, 360), OvertimeMin: r.Next(0, 300),
        EarlyStart: r.Next(2) == 0, WorkedMin: r.Next(0, 720), RestMin: r.Next(0, 120), SwitchesHour: r.Next(0, 40),
        FragThreshold: r.Next(4, 31), WorkloadRatio: r.NextDouble() * 3, StuckTasks: r.Next(0, 8), WaitingEmails: r.Next(0, 15),
        Stress: r.Next(0, 61), Feeling: r.Next(0, 4), AcceptedBreaks: r.Next(0, 8), TasksDone: r.Next(0, 6), FocusBlocks: r.Next(0, 4),
        ExtraBonus: r.Next(0, 5), WeekOvertimeMin: r.Next(0, 1500));

    /// <summary>Bộ 1: chứng minh công thức luật hợp lý (chạy ngay, không cần mạng).</summary>
    public static SuiteReport RunRules(int samples = 5000, int seed = 42)
    {
        var r = new Random(seed);
        var checks = new List<CheckResult>();
        int bad = 0, badDemand = 0, badResource = 0, badWeek = 0, badSwitch = 0;
        string? firstDemand = null, firstResource = null;
        for (var i = 0; i < samples; i++)
        {
            var x = RandomInputs(r);
            var s = MoodModel.Score(x);
            if (s is < 0 or > 100) bad++;
            foreach (var (name, more) in Demands)
                if (MoodModel.Score(more(x, r.NextDouble())) > s) { badDemand++; firstDemand ??= name; }
            foreach (var (name, more) in Resources)
                if (MoodModel.Score(more(x, r.NextDouble())) < s) { badResource++; firstResource ??= name; }
            if (MoodModel.Penalties(x with { WeekOvertimeMin = r.Next(0, 481) })["week"] != 0 ||
                MoodModel.Penalties(x with { WeekOvertimeMin = 481 + r.Next(0, 1000) })["week"] <= 0) badWeek++;
            if (MoodModel.Penalties(x with { SwitchesHour = r.Next(0, x.FragThreshold + 1) })["frag"] != 0) badSwitch++;
        }
        checks.Add(new("Điểm luôn trong 0–100", bad == 0, $"{samples:N0} bộ số ngẫu nhiên, {bad} lỗi"));
        checks.Add(new("Thêm áp lực không bao giờ tăng điểm (JD-R)", badDemand == 0,
            $"{samples * Demands.Length:N0} phép thử trên {Demands.Length} loại áp lực, {badDemand} lỗi" + (firstDemand is null ? "" : $" (vd. {firstDemand})")));
        checks.Add(new("Thêm hồi phục không bao giờ giảm điểm (JD-R)", badResource == 0,
            $"{samples * Resources.Length:N0} phép thử trên {Resources.Length} loại hồi phục, {badResource} lỗi" + (firstResource is null ? "" : $" (vd. {firstResource})")));
        checks.Add(new("Quá giờ cả tuần chỉ bị trừ khi > 48 giờ (Kim et al., 2024)", badWeek == 0, $"{badWeek} lỗi"));
        checks.Add(new("Chuyển việc bình thường không bị trừ (Becker et al., 2023)", badSwitch == 0, $"{badSwitch} lỗi"));

        var score = Scenarios.ToDictionary(s => s.Id, s => MoodModel.Score(s.Inputs));
        foreach (var (easier, harder, why) in Pairs)
            checks.Add(new($"{Name(harder)} ≤ {Name(easier)}", score[harder] <= score[easier], $"{score[harder]} ≤ {score[easier]} · {why}"));
        var notes = Scenarios.Select(s => $"{s.Name}: {score[s.Id]} ({Band(score[s.Id])})").ToList();
        var passed = checks.Count(c => c.Passed);
        return new SuiteReport("Bộ 1 · Luật hợp lý", $"{passed}/{checks.Count} kiểm tra đạt", checks, notes);
    }

    private static string Name(string id) => Scenarios.First(s => s.Id == id).Name;

    // ================= Bộ 2 + 3: AI =================
    public static int RequestsNeeded(int repeats) => Scenarios.Length * repeats;

    /// <summary>Hỏi AI chấm từng tình huống <paramref name="repeats"/> lần. <paramref name="ask"/> nhận số liệu, trả điểm 0–100 hoặc null.</summary>
    public static async Task<LlmRun> RunLlmAsync(string model, Func<string, CancellationToken, Task<int?>> ask, int repeats, int delayMs,
        IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var scores = new Dictionary<string, IReadOnlyList<int?>>();
        var n = 0;
        foreach (var s in Scenarios)
        {
            var list = new List<int?>();
            for (var k = 0; k < repeats; k++)
            {
                ct.ThrowIfCancellationRequested();
                if (n > 0 && delayMs > 0) await Task.Delay(delayMs, ct);
                n++;
                progress?.Report($"{n}/{Scenarios.Length * repeats} · {s.Name} lần {k + 1}");
                int? v;
                try { v = await ask(Facts(s.Inputs), ct); }
                catch (Exception ex) when (ex is not OperationCanceledException) { v = null; }
                list.Add(v is { } x && x is >= 0 and <= 100 ? x : null);
            }
            scores[s.Id] = list;
        }
        return new LlmRun(model, scores, n);
    }

    private static double? Median(IEnumerable<int?> xs)
    {
        var v = xs.Where(x => x is not null).Select(x => (double)x!.Value).OrderBy(x => x).ToList();
        if (v.Count == 0) return null;
        return v.Count % 2 == 1 ? v[v.Count / 2] : (v[v.Count / 2 - 1] + v[v.Count / 2]) / 2;
    }

    private static double Sd(IReadOnlyList<double> v)
    {
        if (v.Count < 2) return 0;
        var m = v.Average();
        return Math.Sqrt(v.Sum(x => (x - m) * (x - m)) / (v.Count - 1));
    }

    /// <summary>Bộ 2: AI trả lời đúng dạng, ổn định khi hỏi lại, và xếp đúng thứ tự các cặp ngày.</summary>
    public static SuiteReport LlmSuite(LlmRun run, double maxSd = 5, int maxRange = 10, int tolerance = 3)
    {
        var checks = new List<CheckResult>();
        var all = run.Scores.Values.SelectMany(x => x).ToList();
        var valid = all.Count(x => x is not null);
        checks.Add(new("Trả lời đúng dạng (điểm 0–100)", valid >= Math.Ceiling(all.Count * .95), $"{valid}/{all.Count} request hợp lệ"));

        var repeats = run.Scores.Values.Max(x => x.Count);
        if (repeats >= 2)
        {
            var spread = run.Scores.Select(kv =>
            {
                var v = kv.Value.Where(x => x is not null).Select(x => (double)x!.Value).ToList();
                return (Id: kv.Key, Sd: Sd(v), Range: v.Count > 0 ? v.Max() - v.Min() : 0);
            }).ToList();
            var worst = spread.MaxBy(x => x.Range);
            checks.Add(new($"Ổn định: hỏi lại {repeats} lần lệch ≤ {maxRange} điểm", spread.All(x => x.Range <= maxRange && x.Sd <= maxSd),
                $"lệch lớn nhất {worst.Range:0} điểm ở \"{Name(worst.Id)}\" (độ lệch chuẩn {worst.Sd:0.0})"));
        }

        var med = run.Scores.ToDictionary(kv => kv.Key, kv => Median(kv.Value));
        foreach (var (easier, harder, why) in Pairs)
        {
            var ok = med[harder] is { } h && med[easier] is { } e && h <= e + tolerance;
            checks.Add(new($"{Name(harder)} ≤ {Name(easier)} (+{tolerance})", ok, $"{Fmt(med[harder])} vs {Fmt(med[easier])} · {why}"));
        }
        var passed = checks.Count(c => c.Passed);
        var notes = Scenarios.Select(s => $"{s.Name}: {string.Join(", ", run.Scores[s.Id].Select(Fmt))}").ToList();
        return new SuiteReport($"Bộ 2 · AI hợp lý và ổn định ({run.Model})", $"{passed}/{checks.Count} kiểm tra đạt · {run.Requests} request", checks, notes);
    }

    /// <summary>Bộ 3: so sánh điểm luật với điểm AI trên cùng các ngày.</summary>
    public static SuiteReport Compare(LlmRun run, double maxMae = 15, double minR = .7, double minBand = .6)
    {
        var rows = Scenarios.Select(s => (s, Rule: MoodModel.Score(s.Inputs), Ai: Median(run.Scores[s.Id]))).Where(x => x.Ai is not null).ToList();
        var checks = new List<CheckResult>();
        if (rows.Count < 3)
            return new SuiteReport($"Bộ 3 · So sánh luật và AI ({run.Model})", "Chưa đủ câu trả lời của AI để so sánh", [new("Đủ dữ liệu", false, $"{rows.Count} tình huống có điểm AI")], []);
        var mae = rows.Average(x => Math.Abs(x.Rule - x.Ai!.Value));
        var r = Validation.Pearson(rows.Select(x => (double)x.Rule).ToList(), rows.Select(x => x.Ai!.Value).ToList());
        var band = rows.Count(x => Band(x.Rule) == Band((int)Math.Round(x.Ai!.Value))) / (double)rows.Count;
        var pairAgree = Pairs.Count(p =>
        {
            var e = rows.FirstOrDefault(x => x.s.Id == p.Easier);
            var h = rows.FirstOrDefault(x => x.s.Id == p.Harder);
            return e.s is not null && h.s is not null && Math.Sign(e.Rule - h.Rule) == Math.Sign(e.Ai!.Value - h.Ai!.Value);
        });
        checks.Add(new($"Lệch trung bình ≤ {maxMae} điểm", mae <= maxMae, $"{mae:0.0} điểm"));
        checks.Add(new($"Tương quan r ≥ {minR}", r is { } rv && rv >= minR, r is { } rr ? $"r = {rr:0.00}" : "không tính được"));
        checks.Add(new($"Cùng mức mood ≥ {minBand:P0}", band >= minBand, $"{band:P0} tình huống cùng mức (Mọng / Cân bằng / Mệt dần / Kiệt sức)"));
        checks.Add(new("Cùng thứ tự các cặp ngày", pairAgree == Pairs.Length, $"{pairAgree}/{Pairs.Length} cặp"));
        var notes = rows.OrderByDescending(x => Math.Abs(x.Rule - x.Ai!.Value))
            .Select(x => $"{x.s.Name}: luật {x.Rule} ({Band(x.Rule)}) · AI {x.Ai:0} ({Band((int)Math.Round(x.Ai!.Value))}) · lệch {x.Ai - x.Rule:+0;-0;0}").ToList();
        var passed = checks.Count(c => c.Passed);
        return new SuiteReport($"Bộ 3 · So sánh luật và AI ({run.Model})", $"{passed}/{checks.Count} kiểm tra đạt · lệch TB {mae:0.0} điểm", checks, notes);
    }

    private static string Fmt(double? v) => v is { } x ? x.ToString("0", CultureInfo.InvariantCulture) : "—";
    private static string Fmt(int? v) => v?.ToString(CultureInfo.InvariantCulture) ?? "—";

    /// <summary>Báo cáo Markdown để lưu / đưa vào bài trình bày.</summary>
    public static string ToMarkdown(DateTime at, params SuiteReport[] reports)
    {
        var sb = new StringBuilder($"# Kiểm chứng điểm mood · {at:dd/MM/yyyy HH:mm}\n\nCơ sở: docs/CO-SO-KHOA-HOC.md\n");
        foreach (var rep in reports)
        {
            sb.Append($"\n## {rep.Title}\n\n**{(rep.Passed ? "ĐẠT" : "CHƯA ĐẠT")}** · {rep.Summary}\n\n| Kiểm tra | Kết quả | Chi tiết |\n| --- | --- | --- |\n");
            foreach (var c in rep.Checks) sb.Append($"| {c.Name} | {(c.Passed ? "✓" : "✗")} | {c.Detail} |\n");
            if (rep.Notes.Count > 0)
            {
                sb.Append('\n');
                foreach (var n in rep.Notes) sb.Append($"- {n}\n");
            }
        }
        return sb.ToString();
    }
}
