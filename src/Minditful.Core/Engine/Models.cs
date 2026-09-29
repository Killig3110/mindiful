namespace Minditful.Core.Engine;

public sealed record Person(string Initials, string Color);

public sealed class CalendarEvent
{
    public required string Id { get; init; }
    public required string Subject { get; init; }
    /// <summary>Giây tính từ 00:00 hôm nay.</summary>
    public required double Start { get; init; }
    public required double End { get; init; }
    /// <summary>"Trình bày" / "Bắt buộc" / "Tuỳ chọn".</summary>
    public string Role { get; init; } = "Bắt buộc";
    public string? Attachment { get; init; }
    public IReadOnlyList<Person> People { get; init; } = [];
    public bool IsOnline { get; init; } = true;
    public string? JoinUrl { get; init; }
    public string? WebLink { get; init; }
    /// <summary>Người tổ chức (hiện trên thẻ "Có mới").</summary>
    public string? Organizer { get; init; }
    /// <summary>Chính bạn tạo cuộc họp này (không báo "lời mời mới").</summary>
    public bool ByMe { get; init; }
}

/// <summary>1 thứ vừa tới: email mới, lời mời họp mới, task mới được giao. <paramref name="Kind"/>: mail / meeting / task.</summary>
public sealed record IncomingItem(string Kind, string Id, string Title, string From, string? Link, string? When = null);

public sealed class MailItem
{
    public required string Id { get; init; }
    public required string From { get; init; }
    public string Color { get; init; } = "#5471B0";
    public required string Subject { get; init; }
    /// <summary>Tên đầy đủ người gửi (thẻ "Có mới"); <see cref="From"/> là chữ viết tắt.</summary>
    public string? FromName { get; init; }
    public int Days { get; init; }
    public string? WebLink { get; init; }
    public bool Handled { get; set; }
}

public sealed class WorkTask
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public int Days { get; init; }
    public string? Url { get; init; }
    public bool Done { get; set; }
}

public sealed record SprintInfo(string Name, double Done, double Total, int DaysLeft);

public sealed record DayScore(string Label, int Score);

public sealed record TomorrowInfo(string Time, string Subject);

public sealed record CompletedTask(string Id, string Title);

/// <summary>Thống kê 1 tuần (thứ Hai → Chủ nhật) tính từ dữ liệu local.</summary>
public sealed record WeekStats(
    DateOnly From, int Days, double AvgScore, DayScore? Best, DayScore? Worst, double MeetingMin, int AcceptedBreaks,
    double FocusMin, int TasksDone, double OvertimeMin, int Shown, int Accepted, int Snoozed, int Dismissed, int Ignored,
    int FeelGood = 0, int FeelOk = 0, int FeelBad = 0);

/// <summary>Chuỗi ngày về đúng giờ và phụ kiện Milo đã mở khoá (tủ đồ).</summary>
/// <param name="Breaks">Tổng số lần nghỉ cùng Milo từ trước tới nay (mở khoá món nghỉ).</param>
/// <param name="FocusMin">Tổng phút tập trung sâu từ trước tới nay.</param>
/// <param name="Kept">Món theo mùa đã mở ở mùa trước (giữ luôn).</param>
public sealed record WardrobeInfo(int Streak, int Best, string? NewItem = null, int Breaks = 0, int FocusMin = 0, IReadOnlyList<string>? Kept = null);

/// <summary>Toàn bộ dữ liệu công việc mà Rule Engine đọc. Demo dùng bản cố định, Prod/Sandbox làm mới định kỳ.</summary>
public sealed class WorkSnapshot
{
    public IReadOnlyList<CalendarEvent> Calendar { get; init; } = [];
    public TomorrowInfo? Tomorrow { get; init; }
    /// <summary>Lịch ngày mai (giây tính từ 00:00 ngày mai) — để gợi ý nghỉ giữa chuỗi họp từ tối hôm trước.</summary>
    public IReadOnlyList<CalendarEvent> TomorrowCalendar { get; init; } = [];
    public WardrobeInfo? Wardrobe { get; init; }
    public IReadOnlyList<MailItem> Emails { get; init; } = [];
    /// <summary>Email gửi thẳng cho bạn trong 24 giờ qua (để báo "email mới", không phải email chờ trả lời).</summary>
    public IReadOnlyList<MailItem> RecentMail { get; init; } = [];
    public int Unread { get; init; }
    public IReadOnlyList<WorkTask> Tasks { get; init; } = [];
    public double AvgInProgress { get; init; } = 2.8;
    public SprintInfo? Sprint { get; init; }
    public IReadOnlyList<DayScore> Week { get; init; } = [];
    public int? YesterdayScore { get; init; }
    public WeekStats? ThisWeek { get; init; }
    public WeekStats? LastWeek { get; init; }
    /// <summary>Work item chuyển Done hôm nay (Prod/Sandbox). Engine tự lọc những cái đã báo.</summary>
    public IReadOnlyList<CompletedTask> CompletedToday { get; init; } = [];
    /// <summary>Tính năng tắt vì thiếu quyền/mất mạng, hiện 1 dòng nhỏ trong dashboard (mục 14).</summary>
    public string? StatusNote { get; init; }
    public bool MailAvailable { get; init; } = true;
    public bool BoardsAvailable { get; init; } = true;
    /// <summary>Có Calendars.ReadWrite. Không có thì "Giữ chỗ" chỉ là lời hẹn nhắc, không ghi vào lịch (§14).</summary>
    public bool CanWriteCalendar { get; init; } = true;
}

public sealed class CaseData
{
    /// <summary>Thẻ "Có mới": các thứ vừa tới, mới nhất ở cuối.</summary>
    public List<IncomingItem>? Incoming { get; set; }
    public CalendarEvent? Ev { get; set; }
    public List<CalendarEvent>? Chain { get; set; }
    public WorkTask? Task { get; set; }
    public List<MailItem>? List { get; set; }
    public int Count { get; set; }
    public int FocusMin { get; set; }
    public double Min { get; set; }
    public double Over { get; set; }
    public double Rest { get; set; }
    public double Worked { get; set; }
    public int Sw { get; set; }
    public bool FromHold { get; set; }
    /// <summary>Giờ bắt đầu khối được đề nghị (Giữ giờ tập trung).</summary>
    public double At { get; set; }
    /// <summary>Câu chính do LLM viết sẵn lúc case vào hàng đợi (null = dùng template).</summary>
    public string? Line { get; set; }
}

public sealed class QueueItem
{
    public required CaseId C { get; init; }
    public required string Key { get; set; }
    public CaseData Data { get; set; } = new();
    public int Pri { get; init; }
    public int Sev { get; init; }
    public double Enq { get; set; }
    public double? Ttl { get; init; }
    public int Need { get; init; }
}

public sealed class CaseMemory
{
    public int Shown;
    public readonly HashSet<string> Keys = [];
    public double SnoozedUntil;
    public double DismissedUntil;
    public int Dismisses;
    public int Snoozes;
    public int Widen = 1;
    public HashSet<string> Half = [];
    public bool ForDay;
}

public sealed record Parked(CaseId C, QueueItem Item, double Until);
public sealed record Folded(CaseId C, double T, CaseData Data);
public sealed record Reminder(CaseId C, string Key, double At, CaseData Data);
public sealed record Hold(string Kind, double Start, double End, string Label);

public enum VisitPhase { In, Look, Out }

public sealed class Visit
{
    public VisitPhase Phase;
    public double End;
    public Clip Clip;
}

public enum LogKind { None, Queue, Deliver, Wait, User, Gate, Sig, Mood, Action, Error }

/// <summary>Dòng nhật ký quyết định. Text có thể chứa **đậm**.</summary>
public sealed record LogEntry(double T, string Text, LogKind Kind);

/// <param name="Actions">Mã tính năng Milo đề nghị kèm câu trả lời (xem <see cref="Talk.Actions"/>), hiện thành nút ngay dưới câu.</param>
public sealed record ChatLine(string You, string Milo, IReadOnlyList<string>? Actions = null);

public enum DashPage { Today, Week }

public sealed class Episode
{
    public int Id;
    public CaseId C;
    public required QueueItem Item;
    public required CaseData Data;
    public int Pri;
    /// <summary>Biến thể template của lần hiện này (xoay vòng để không lặp câu).</summary>
    public int Variant;
    public bool Compact, FromDot, FromParked;
    public bool Card;
    public int CardVer;
    public readonly List<ChatLine> Chat = [];
    public Clip? Clip;
    public Phase Phase = Phase.Enter;
    public double PhaseStart, PhaseEnd, T0;
    public Queue<(double Delay, string Act)> AutoPlan = new();
    public Queue<Clip> Seq = new();
    public bool AfterThanks;
    public Clip? AfterClip;
    public string? ThanksText;
    public Clip AfterThanksClip = Engine.Clip.ClimbOut;
    public bool Opened, Held, Confirmed, GoHome, LongBreak;
    /// <summary>Dashboard: đang xem bảng chi tiết thay cho 4 quả.</summary>
    public bool Detail;
    /// <summary>Dashboard đang mở tủ đồ (phối đồ ngay trên đầu Milo, thay cho 4 quả).</summary>
    public bool Wardrobe;
    public int Cycles, Cycle;
    public DashPage Page = DashPage.Today;
    public string? Intent;
    public Clip? ExitClip;
}
