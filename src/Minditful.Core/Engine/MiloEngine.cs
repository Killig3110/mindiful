namespace Minditful.Core.Engine;

public sealed record ScriptStep(double T, string Action, string? Id = null);

/// <summary>Kịch bản chạy tự động của ngày mẫu: người dùng làm gì, rê chuột lúc nào, tự bấm nút nào.</summary>
public sealed class ScenarioScript
{
    public IReadOnlyList<ScriptStep> World { get; init; } = [];
    public IReadOnlyList<ScriptStep> Ui { get; init; } = [];
    public IReadOnlyDictionary<CaseId, (double Delay, string Act)[]> AutoReplies { get; init; } =
        new Dictionary<CaseId, (double, string)[]>();
}

/// <summary>
/// Bộ não Milo: Rule Engine → hàng đợi → Điều phối → episode, cộng Mood Engine.
/// Port 1-1 từ prototype "Milo sống" để ngày mẫu cho ra đúng các mốc trong tài liệu.
/// </summary>
public sealed partial class MiloEngine
{
    public EngineConfig Cfg { get; }
    public ScenarioScript? Script { get; }
    public WorkSnapshot Snap { get; private set; }
    public DayState S { get; private set; }
    public DateOnly Day { get; private set; }

    /// <summary>Milo cần làm gì đó ra thế giới thật (host chuyển tới Graph/Azure DevOps).</summary>
    public event Action<MiloAction>? ActionRequested;

    /// <summary>Case vừa vào hàng đợi: host gọi LLM viết sẵn câu chính rồi trả về bằng <see cref="SetLine"/>.</summary>
    public event Action<QueueItem, LineRequest>? LineWanted;

    /// <summary>Câu chat không khớp từ khoá: host hỏi LLM rồi trả về bằng <see cref="ResolveChat"/>.</summary>
    public event Action<int, int, ChatRequest>? ChatWanted;

    /// <summary>Mỗi lần hiện/đồng ý/để sau/không cần/bỏ qua… (§14 · Log) — host lưu để cá nhân hoá.</summary>
    public event Action<CaseId, Outcome>? OutcomeRecorded;

    /// <summary>Điều chỉnh theo 7 ngày gần nhất (host tính bằng <see cref="Personalizer"/> mỗi đầu ngày).</summary>
    public IReadOnlyDictionary<CaseId, CaseTuning> Tuning { get; set; } = new Dictionary<CaseId, CaseTuning>();

    public MiloEngine(EngineConfig cfg, WorkSnapshot snap, ScenarioScript? script = null, DateOnly? day = null, double? startT = null)
    {
        Cfg = cfg;
        Snap = snap;
        Script = script;
        Day = day ?? DateOnly.FromDateTime(DateTime.Now);
        S = NewState(startT ?? cfg.DayOpen);
        AssessMeetings();
    }

    private DayState NewState(double t0)
    {
        var s = new DayState { T = t0, Rnd = new Mulberry32(Cfg.Seed), LastEpEnd = t0 };
        s.Emails = Snap.Emails.Select(CloneMail).ToList();
        s.Tasks = Snap.Tasks.Select(CloneTask).ToList();
        return s;
    }

    private static MailItem CloneMail(MailItem e) => new() { Id = e.Id, From = e.From, Color = e.Color, Subject = e.Subject, Days = e.Days, WebLink = e.WebLink };
    private static WorkTask CloneTask(WorkTask x) => new() { Id = x.Id, Title = x.Title, Days = x.Days, Url = x.Url };

    /// <summary>Sang ngày mới (Prod/Sandbox): trạng thái về đầu ngày, máy coi như đang khoá tới lần mở đầu tiên.</summary>
    public void StartNewDay(DateOnly day, double t0, WorkSnapshot snap)
    {
        Day = day;
        Snap = snap;
        var auto = S.Auto;
        S = NewState(t0);
        S.Auto = auto;
        AssessMeetings();
    }

    /// <summary>Dữ liệu Graph/Azure DevOps vừa làm mới. Giữ lại cờ đã xử lý của email/task.</summary>
    public void ApplySnapshot(WorkSnapshot snap)
    {
        Snap = snap;
        foreach (var e in S.Emails.Where(e => e.Handled)) S.HandledMail.Add(e.Id);
        S.Emails = snap.Emails.Select(CloneMail).ToList();
        foreach (var e in S.Emails) e.Handled = S.HandledMail.Contains(e.Id);
        AssessMeetings();
        var done = S.Tasks.Where(x => x.Done).Select(x => x.Id).ToHashSet();
        S.Tasks = snap.Tasks.Select(CloneTask).ToList();
        foreach (var x in S.Tasks) x.Done = done.Contains(x.Id);
        DetectIncoming(snap);

        // Lần đầu chỉ ghi nhận, không ăn mừng những task đã Done trước khi mở app.
        if (!S.DoneSeeded)
        {
            foreach (var c in snap.CompletedToday) S.AnnouncedDone.Add(c.Id);
            S.DoneSeeded = true;
        }
        else
        {
            foreach (var c in snap.CompletedToday.Where(c => S.AnnouncedDone.Add(c.Id)))
                CompleteTask(c.Id, c.Title);
        }
    }

    private CaseMemory Mem(CaseId c) => S.Mem.TryGetValue(c, out var m) ? m : S.Mem[c] = new CaseMemory();

    private void Log(string text, LogKind kind = LogKind.None)
    {
        S.Log.Add(new LogEntry(S.T, text, kind));
        if (S.Log.Count > 220) S.Log.RemoveAt(0);
    }

    public void LogExternal(string text, LogKind kind) => Log(text, kind);

    private double Dt(double sec) => S.Instant ? 0 : sec;
    private double RandMin(double a, double b) => (a + S.Rnd.Next() * (b - a)) * 60;

    private CaseTuning Tune(CaseId c) => Tuning.TryGetValue(c, out var t) ? t : CaseTuning.None;
    /// <summary>Hệ số ngưỡng kích hoạt (luật 2: +15%).</summary>
    private double Tf(CaseId c) => Tune(c).ThresholdFactor;
    /// <summary>Hệ số thời gian chờ Để sau/Không cần (luật 1 trong ngày × luật 2 theo 7 ngày).</summary>
    public double WaitFactor(CaseId c) => Mem(c).Widen * Tune(c).WaitFactor;
    public int SnoozeMinutes(CaseId c) => (int)Math.Round(Catalog.Def(c).Snooze * WaitFactor(c));
    public int SnoozeCount(CaseId c) => Mem(c).Snoozes;

    private void Record(CaseId c, Outcome o)
    {
        if (S.Instant || c is CaseId.Dashboard or CaseId.Talk) return;
        OutcomeRecorded?.Invoke(c, o);
    }

    /// <summary>LLM đã viết xong câu chính cho case trong hàng đợi (hoặc đang hiện).</summary>
    public void SetLine(QueueItem item, string line)
    {
        item.Data.Line = line;
        if (S.Ep is { } ep && ep.Item == item && !ep.Opened) ep.CardVer++;
    }

    /// <summary>
    /// LLM trả lời câu chat (null = hết giờ/lỗi → câu theo từ khoá). <paramref name="actions"/>: tính năng AI đề nghị,
    /// được lọc lại theo danh sách cho phép và những gì đang dùng được.
    /// </summary>
    public void ResolveChat(int epId, int index, string? text, IReadOnlyList<string>? actions = null)
    {
        if (S.Ep is not { } ep || ep.Id != epId || index >= ep.Chat.Count) return;
        var line = ep.Chat[index];
        ep.Chat[index] = text is null
            ? line with { Milo = ep.C == CaseId.Talk ? Talk.Reply(this, line.You) : Lines.ChatFallback, Actions = Talk.Suggest(this, line.You) }
            : line with { Milo = text, Actions = Talk.Pick(this, actions) };
        ep.CardVer++;
    }

    private void Raise(MiloAction a)
    {
        if (S.Instant) return; // tua nhanh để nhảy mốc: không đụng tới thế giới thật
        ActionRequested?.Invoke(a);
    }

    // ================= tín hiệu =================
    public IReadOnlyList<CalendarEvent> Meetings => Snap.Calendar;
    public CalendarEvent? Ongoing() => Meetings.FirstOrDefault(e => e.Start <= S.T && S.T < e.End && !S.Skipped.Contains(e.Id));
    public bool InCall() => S.DayStarted && !S.Locked && (S.InCallOverride ?? Ongoing() is not null);
    public CalendarEvent? NextMeeting() => Meetings.Where(e => e.Start > S.T).OrderBy(e => e.Start).FirstOrDefault();
    public double GapToNext() => NextMeeting() is { } n ? (n.Start - S.T) / 60 : double.PositiveInfinity;

    public List<List<CalendarEvent>> Chains()
    {
        var outList = new List<List<CalendarEvent>>();
        var cur = new List<CalendarEvent>();
        foreach (var e in Meetings.OrderBy(e => e.Start))
        {
            if (cur.Count > 0 && e.Start - cur[^1].End < 300) cur.Add(e);
            else
            {
                if (cur.Count > 0) outList.Add(cur);
                cur = [e];
            }
        }
        if (cur.Count > 0) outList.Add(cur);
        return outList;
    }

    public double MeetingMin()
    {
        double m = 0;
        foreach (var e in Meetings)
        {
            if (S.Skipped.Contains(e.Id)) continue;
            double a = e.Start, b = Math.Min(e.End, S.T);
            if (b > a) m += (b - a) / 60;
        }
        return m;
    }

    private int LongestChainSoFar() => Chains().Select(c => c.Count(e => e.Start <= S.T)).DefaultIfEmpty(0).Max();
    public double Streak() => !S.DayStarted || S.BreakRun >= 5 ? 0 : (S.T - (S.LastBreakEnd ?? S.T)) / 60;
    public double Worked() => S.FirstAct is null ? 0 : (S.T - S.FirstAct.Value) / 60;
    public int SwitchesHour() => S.Switches.Count(x => x > S.T - 3600);
    public List<WorkTask> StuckTasks() => S.Tasks.Where(x => !x.Done && x.Days >= Cfg.StuckMinDays).OrderByDescending(x => x.Days).ToList();
    public int InProgress() => S.Tasks.Count(x => !x.Done);
    public List<MailItem> WaitingEmails() => S.Emails.Where(e => e.Days >= 1 && !e.Handled).OrderByDescending(e => e.Days).ToList();
    public bool FocusActive() => S.FocusUntil is { } f && S.T < f;

    public Gate? HardGate()
    {
        if (!S.DayStarted) return Gate.Off;
        if (S.Locked) return Gate.Locked;
        if (S.OffDuty) return Gate.Off;
        if (S.Presenting) return Gate.Presenting;
        if (InCall()) return Gate.Meeting;
        if (S.Fullscreen) return Gate.Fullscreen;
        if (FocusActive()) return Gate.Focus;
        if (S.UserDnd) return Gate.Dnd;
        return null;
    }

    public PresenceState Presence()
    {
        if (S.Ep is not null) return PresenceState.Talk;
        var g = HardGate();
        // Đang trình chiếu: trốn hẳn, không để lại cả chóp đuôi trên màn hình đang chia sẻ
        if (g is Gate.Off or Gate.Locked or Gate.Presenting) return PresenceState.Off;
        if (g is not null) return PresenceState.Silent;
        if (S.Visit is not null) return PresenceState.Visit;
        if (S.Peek) return PresenceState.Peek;
        return PresenceState.Hidden;
    }

    public Band CurrentBand => Catalog.Bands[S.BandIdx];

    // ================= mood (mục 11) =================
    public void ComputeMood()
    {
        // Công thức nằm ở MoodModel (có nguồn nghiên cứu từng khoản, kiểm chứng bằng MoodEvidenceTests)
        var inputs = MoodInputsNow();
        var p = MoodModel.Penalties(inputs);
        var bonus = MoodModel.Bonus(inputs);
        var sum = p.Values.Sum();
        S.RuleScore = MoodModel.Score(sum, bonus);
        var insight = Cfg.MoodMode == MoodMode.Rules ? null : FreshInsight();
        if (insight is not null && Cfg.MoodMode == MoodMode.Hybrid && insight.Adjust != 0)
        {
            p["llm"] = -Math.Clamp(insight.Adjust, -10, 10); // Claude chỉnh tối đa ±10 điểm
            sum = p.Values.Sum();
        }
        S.Pen = p;
        S.Bonus = bonus;
        S.Score = insight is { Score: { } llmScore } && Cfg.MoodMode == MoodMode.Llm
            ? Math.Clamp(llmScore, 0, 100)
            : MoodModel.Score(sum, bonus);

        int[] min = [80, 60, 40, 0];
        int i = S.BandIdx, before = i;
        if (!S.DayStarted) i = S.Score >= 80 ? 0 : S.Score >= 60 ? 1 : S.Score >= 40 ? 2 : 3;
        else
        {
            while (i < 3 && S.Score <= min[i] - 3) i++;
            while (i > 0 && S.Score >= min[i - 1] + 3) i--;
        }
        S.BandIdx = i;
        if (S.DayStarted && i != before)
            Log($"Điểm {S.Score} → mức {Catalog.Bands[i].Label}" + (i > before ? ". Milo đổi dáng, không popup (B10)" : ". Màu Milo hồi lại"), LogKind.Mood);

        var sw = SwitchesHour();
        var focusLen = Math.Max(S.LongestNm, S.FocusMinDone);
        S.Vibe = (
            (int)Tm.Clamp(Tm.JsRound(5 * Math.Min(1, focusLen / 90) * (sw > 4 ? Math.Max(0, 1 - (sw - 4) / 12.0) : 1)), 0, 5),
            (int)Tm.Clamp(Tm.JsRound(S.Score / 20.0), 0, 5),
            (int)Tm.Clamp(Tm.JsRound((p["meet"] + p["chain"] + p["ot"] + p["work"] + p["stuck"] + p["email"] + p["rest"] + p["stress"]) / 40 * 5), 0, 5));
        if (insight is not null)
            S.Vibe = (Math.Clamp(insight.Focus ?? S.Vibe.F, 0, 5), Math.Clamp(insight.Energy ?? S.Vibe.E, 0, 5), Math.Clamp(insight.Stress ?? S.Vibe.S, 0, 5));
    }

    /// <summary>Số liệu hiện tại đưa vào công thức mood.</summary>
    public MoodInputs MoodInputsNow() => new(
        MeetingMin(), LongestChainSoFar(), Streak(), S.OtMin, S.FirstAct is { } fa && fa < Cfg.Start - 1800,
        Worked(), S.Rest, SwitchesHour(), Cfg.FragThreshold, InProgress() / Math.Max(0.1, Snap.AvgInProgress),
        StuckTasks().Count, WaitingEmails().Count, S.Stress, S.Feeling,
        S.AcceptedBreaks, S.TasksDone, S.FocusDone, S.ExtraBonus,
        // Thống kê tuần trên máy có thể đã gồm phần hôm nay lưu định kỳ → lấy max để không cộng trùng
        Math.Max(Snap.ThisWeek?.OvertimeMin ?? 0, S.OtMin));

    public DayRecord BuildDayRecord() => new(Day, S.Score, MeetingMin(), S.AcceptedBreaks, S.FocusMinDone, S.TasksDone, S.OtMin,
        S.Vibe.F, S.Vibe.E, S.Vibe.S, InProgress(), S.Feeling);

    // ================= tính năng mở rộng =================
    /// <summary>
    /// Khoảng trống dài nhất từ 15 phút nữa tới hết giờ làm (bỏ giờ họp, khối đã giữ, 12:00–13:00 ăn trưa).
    /// Trả về giờ bắt đầu (chừa 5 phút sau cuộc họp) và số phút đề nghị (tối đa 90).
    /// </summary>
    public (double Start, double Min)? FocusSlot()
    {
        var from = Math.Ceiling((S.T + 900) / 300) * 300;
        var end = Cfg.End;
        var busy = Meetings.Where(m => !S.Skipped.Contains(m.Id)).Select(m => (m.Start, End: m.End + 300))
            .Concat(S.Holds.Select(h => (h.Start, End: h.End)))
            .Append((Start: Tm.T("12:00"), End: Tm.T("13:00")))
            .Where(b => b.End > from && b.Start < end).OrderBy(b => b.Start).ToList();
        (double Start, double Len) best = (0, 0);
        var cursor = from;
        foreach (var b in busy)
        {
            if (b.Start - cursor > best.Len) best = (cursor, b.Start - cursor);
            cursor = Math.Max(cursor, b.End);
        }
        if (end - cursor > best.Len) best = (cursor, end - cursor);
        var start = Math.Ceiling(best.Start / 300) * 300;
        var min = Math.Floor(Math.Min(90, (best.Start + best.Len - start) / 60) / 5) * 5;
        return min >= Cfg.FocusPlanMinMinutes ? (start, min) : null;
    }

    /// <summary>Chuỗi ≥ 3 cuộc họp liền nhau của ngày mai (cách nhau dưới 5 phút).</summary>
    public IReadOnlyList<CalendarEvent>? TomorrowChain()
    {
        var cur = new List<CalendarEvent>();
        foreach (var e in Snap.TomorrowCalendar.OrderBy(e => e.Start))
        {
            if (cur.Count > 0 && e.Start - cur[^1].End < 300) cur.Add(e);
            else
            {
                if (cur.Count >= 3) return cur;
                cur = [e];
            }
        }
        return cur.Count >= 3 ? cur : null;
    }
}
