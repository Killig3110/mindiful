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
    /// <summary>Giờ bắt đầu làm thật của hôm nay đọc lại từ máy (mở lại app giữa ngày).</summary>
    public double? KnownDayStart;
    /// <summary>Case do Sandbox ép vào hàng đợi: không bị bỏ khi điều kiện không đúng.</summary>
    public readonly HashSet<CaseId> Forced = [];
    public readonly Dictionary<CaseId, int> VariantCounter = [];
    public readonly Dictionary<string, MeetingAssessment> Assessments = [];
    public MoodInsight? MoodInsight;
    public double LastMoodAsk = -1e9;
    /// <summary>Clip hài đang chen vào (vd. bấm Milo liên tục → ngất) trong khoảng [Start, End) giờ engine.</summary>
    public (Clip Clip, double Start, double End)? Reaction;
    public readonly List<double> Pokes = [];
    public double? AwaySince;
    /// <summary>Vừa quay lại sau ≥ 30 phút vắng: lần ghé tới Milo phủ mạng nhện.</summary>
    public bool CobwebPending;
    /// <summary>Thẻ "Có mới": id đã thấy (m:/e:/t:). Lần đọc đầu chỉ ghi nhận, không báo những thứ có sẵn.</summary>
    public readonly HashSet<string> SeenIncoming = [];
    /// <summary>Nguồn (mail / meeting / task) đã có lần đọc đầu tiên làm mốc. Nguồn chưa đọc được (chưa đăng nhập, mất mạng) chưa làm mốc,
    /// để khi đọc được lần đầu không báo mọi thứ có sẵn là "mới".</summary>
    public readonly HashSet<string> IncomingSeeded = [];
    public int DemoIncoming;
    public readonly List<string> ChatHistory = [];
    /// <summary>Teams presence "Presenting": Milo trốn hẳn, kể cả chóp đuôi.</summary>
    public bool Presenting;
    /// <summary>Tự đánh giá cuối ngày (<see cref="Feeling"/>), 0 = chưa trả lời.</summary>
    public int Feeling;
    public double? TomorrowHoldAt;
    /// <summary>Demo · Mood realtime: giữ Milo đứng ngoài (ghé ngang kéo dài) để người xem thấy dáng/màu đổi theo điểm.</summary>
    public bool HoldVisit;
    public double LastMicro = -1e9;
    public int MicroCount;
    /// <summary>Điểm Mood Engine theo luật, trước khi Claude chỉnh.</summary>
    public int RuleScore = 92;
}
