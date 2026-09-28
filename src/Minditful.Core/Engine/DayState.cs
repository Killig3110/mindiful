namespace Minditful.Core.Engine;

/// <summary>Trạng thái 1 ngày của Milo (tương ứng biến S của prototype).</summary>
public sealed class DayState
{
    public double T;
    public required Mulberry32 Rnd;
    public bool Auto = true, Instant;
    public bool DayStarted, Locked = true, Away, Typing, Fullscreen, UserDnd, OffDuty;
    public double Stress;
    public double? FirstAct, LastBreakEnd;
    public int BreakRun;
    public double Rest;
    public bool LunchTaken;
    public double OtMin, OtAfter, OffActive;
    public double? ExtendedUntil;
    public bool EodShown, Nudged;
    public int NmRun, LongestNm;
    public readonly List<double> Switches = [];
    public int AcceptedBreaks, TasksDone, FocusDone;
    public double FocusMinDone;
    public double? FocusUntil, FocusStart;
    public WorkTask? FocusTask;
    public int ExtraBonus;
    public readonly HashSet<string> Skipped = [];
    public readonly List<Hold> Holds = [];
    public List<MailItem> Emails = [];
    public List<WorkTask> Tasks = [];
    public List<QueueItem> Queue = [];
    public List<Parked> ParkedList = [];
    public readonly List<Folded> FoldedList = [];
    public readonly List<Reminder> Reminders = [];
    public readonly Dictionary<CaseId, CaseMemory> Mem = [];
    public Episode? Ep;
    public Visit? Visit;
    public bool Peek;
    public double? BreakUntil;
    public Gate? Gate;
    public double SettleUntil, JumpUntil;
    public double LastProactive = -1e9;
    public List<double> HourList = [];
    public int DayCount;
    public double LastEpEnd;
    public double? NextVisit, DeferStart;
    public int Score = 92, BandIdx = 1;
    public Dictionary<string, double> Pen = [];
    public int Bonus;
    public (int F, int E, int S) Vibe = (3, 4, 1);
    public readonly List<LogEntry> Log = [];
    public int Wi, Ui;
    public long LastMin = -1;
    public int EpSeq;
    public readonly Dictionary<CaseId, string> Reasons = [];
    public bool Ended;
    public HashSet<CaseId>? Live;
    /// <summary>Presence Teams nói đang trong cuộc gọi (null = không có presence, suy từ lịch).</summary>
    public bool? InCallOverride;
    public readonly HashSet<string> AnnouncedDone = [];
    public readonly HashSet<string> HandledMail = [];
    public bool DoneSeeded;
    /// <summary>Case do Sandbox ép vào hàng đợi: không bị bỏ khi điều kiện không đúng.</summary>
    public readonly HashSet<CaseId> Forced = [];
}
