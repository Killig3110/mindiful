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

    public MiloEngine(EngineConfig cfg, WorkSnapshot snap, ScenarioScript? script = null, DateOnly? day = null, double? startT = null)
    {
        Cfg = cfg;
        Snap = snap;
        Script = script;
        Day = day ?? DateOnly.FromDateTime(DateTime.Now);
        S = NewState(startT ?? cfg.DayOpen);
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
    }

    /// <summary>Dữ liệu Graph/Azure DevOps vừa làm mới. Giữ lại cờ đã xử lý của email/task.</summary>
    public void ApplySnapshot(WorkSnapshot snap)
    {
        Snap = snap;
        foreach (var e in S.Emails.Where(e => e.Handled)) S.HandledMail.Add(e.Id);
        S.Emails = snap.Emails.Select(CloneMail).ToList();
        foreach (var e in S.Emails) e.Handled = S.HandledMail.Contains(e.Id);
        var done = S.Tasks.Where(x => x.Done).Select(x => x.Id).ToHashSet();
        S.Tasks = snap.Tasks.Select(CloneTask).ToList();
        foreach (var x in S.Tasks) x.Done = done.Contains(x.Id);

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
    public List<WorkTask> StuckTasks() => S.Tasks.Where(x => !x.Done && x.Days >= 3).OrderByDescending(x => x.Days).ToList();
    public int InProgress() => S.Tasks.Count(x => !x.Done);
    public List<MailItem> WaitingEmails() => S.Emails.Where(e => e.Days >= 1 && !e.Handled).OrderByDescending(e => e.Days).ToList();
    public bool FocusActive() => S.FocusUntil is { } f && S.T < f;

    public Gate? HardGate()
    {
        if (!S.DayStarted) return Gate.Off;
        if (S.Locked) return Gate.Locked;
        if (S.OffDuty) return Gate.Off;
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
        if (g is Gate.Off or Gate.Locked) return PresenceState.Off;
        if (g is not null) return PresenceState.Silent;
        if (S.Visit is not null) return PresenceState.Visit;
        if (S.Peek) return PresenceState.Peek;
        return PresenceState.Hidden;
    }

    public Band CurrentBand => Catalog.Bands[S.BandIdx];

    // ================= mood (mục 11) =================
    public void ComputeMood()
    {
        var p = new Dictionary<string, double>
        {
            ["meet"] = Math.Min(20, Math.Max(0, MeetingMin() - 180) * 0.1),
            ["chain"] = Math.Min(12, Math.Max(0, LongestChainSoFar() - 2) * 4),
            ["streak"] = Math.Min(15, Math.Max(0, Streak() - 90) * 0.2),
            ["ot"] = Math.Min(25, S.OtMin * 0.33) + (S.FirstAct is { } fa && fa < Cfg.Start - 1800 ? 5 : 0),
        };
        var w = Worked();
        p["rest"] = w >= 120 ? Math.Min(15, Math.Max(0, 45 * w / 480 - S.Rest) * 0.5) : 0;
        p["frag"] = Math.Min(12, Math.Max(0, SwitchesHour() - Cfg.FragThreshold) * 2);
        var ratio = InProgress() / Math.Max(0.1, Snap.AvgInProgress);
        p["work"] = ratio > 2 ? 10 : ratio > 1.5 ? 6 : 0;
        p["stuck"] = Math.Min(6, StuckTasks().Count * 2);
        p["email"] = Math.Min(4, Math.Max(0, WaitingEmails().Count - 2));
        p["stress"] = S.Stress;
        var bonus = Math.Min(12, S.AcceptedBreaks * 3) + Math.Min(6, S.TasksDone * 2) + Math.Min(8, S.FocusDone * 4) + Math.Min(3, S.ExtraBonus);
        var sum = p.Values.Sum();
        S.Pen = p;
        S.Bonus = bonus;
        S.Score = (int)Tm.Clamp(Tm.JsRound(92 - sum + bonus), 0, 100);

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
    }

    public DayRecord BuildDayRecord() => new(Day, S.Score, MeetingMin(), S.AcceptedBreaks, S.FocusMinDone, S.TasksDone, S.OtMin,
        S.Vibe.F, S.Vibe.E, S.Vibe.S, InProgress());
}
