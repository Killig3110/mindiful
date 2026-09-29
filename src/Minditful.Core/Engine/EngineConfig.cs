namespace Minditful.Core.Engine;

public sealed class EngineConfig
{
    public double Start { get; set; } = Tm.T("09:00");
    /// <summary>Giờ kết thúc khung làm việc — Sandbox cho đổi lúc đang chạy để test Tan tầm ngay.</summary>
    public double End { get; set; } = Tm.T("18:00");
    /// <summary>Chỉ dùng cho ngày mẫu: giờ bắt đầu/kết thúc mô phỏng.</summary>
    public double DayOpen { get; init; } = Tm.T("08:50");
    public double DayClose { get; init; } = Tm.T("18:45");
    public double GapBudget { get; init; } = 15 * 60;
    public int PerHour { get; init; } = 3;
    public int PerDay { get; init; } = 10;
    public double TypingDefer { get; init; } = 300;
    public double Settle { get; init; } = 120;
    public double JumpWindow { get; init; } = 600;
    public double ParkTtl { get; init; } = 1800;
    /// <summary>Số lần chuyển việc/giờ bắt đầu bị tính là phân mảnh (spec: 8).</summary>
    public int FragThreshold { get; init; } = 8;
    /// <summary>Chào sáng chỉ khi lần mở máy đầu tiên trước giờ này (null = luôn chào). Spec: sau 04:00.</summary>
    public double? MorningHelloUntil { get; init; }
    /// <summary>Chạy theo kịch bản ngày mẫu (world/ui script, tự trả lời, dừng ở DayClose).</summary>
    public bool Scripted { get; init; }
    public uint Seed { get; init; } = 24;

    // ---- Ngưỡng hành vi (tài liệu mục 4, 7, 8, 9). Sandbox có thể rút gọn qua BehaviorOverrides để test trong 1 buổi. ----
    /// <summary>Task kẹt: số ngày làm việc ở Active (spec: 3).</summary>
    public int StuckMinDays { get; init; } = 3;
    /// <summary>Email chờ không giao trước giờ này vì bản tin sáng đã báo (spec: 10:00).</summary>
    public double EmailNotBefore { get; init; } = Tm.T("10:00");
    /// <summary>Làm liền: số phút không có lần nghỉ ≥ 5 phút (spec: 120).</summary>
    public double NoBreakMin { get; init; } = 120;
    /// <summary>Họp liên tục: số cuộc họp liền nhau tối thiểu (spec: 3).</summary>
    public int OverloadMinChain { get; init; } = 3;
    /// <summary>Ghé ngang mỗi 30–60 phút (spec §9.3).</summary>
    public double VisitMinMinutes { get; init; } = 30;
    public double VisitMaxMinutes { get; init; } = 60;

    // ---- Giờ làm linh hoạt (Bosch flexible time): bắt đầu = lần mở máy đầu ngày, kẹp trong [FlexEarliestStart, FlexLatestStart] ----
    /// <summary>null = khung cố định Start–End. Có giá trị = linh hoạt (vd. 08:00).</summary>
    public double? FlexEarliestStart { get; init; }
    public double FlexLatestStart { get; init; } = Tm.T("10:00");
    /// <summary>Số tiếng từ lúc bắt đầu tới giờ về (8h làm + 1h nghỉ trưa = 9).</summary>
    public double FlexHours { get; init; } = 9;
    public bool IsFlexible => FlexEarliestStart is not null;

    // ---- Lớp 2 (LLM). Host hạ về Rules khi không có API key. ----
    public MoodMode MoodMode { get; set; } = MoodMode.Rules;
    public MeetingMode MeetingMode { get; set; } = MeetingMode.Rules;
    /// <summary>Bao lâu hỏi Claude về mood 1 lần (Hybrid/Llm).</summary>
    public double MoodIntervalMinutes { get; set; } = 30;
    /// <summary>Gửi kèm tối đa 5 câu người dùng tự gõ cho Milo hôm nay để Claude đọc cảm xúc (mặc định tắt).</summary>
    public bool IncludeChatInMood { get; set; }
}

/// <summary>Hành động Milo cần làm ra thế giới thật (Teams, Outlook, Azure Boards). Demo chỉ ghi log.</summary>
public abstract record MiloAction
{
    public sealed record JoinMeeting(CalendarEvent Event) : MiloAction;
    public sealed record OpenAttachment(CalendarEvent Event) : MiloAction;
    /// <summary>POST /me/events, showAs tentative.</summary>
    public sealed record HoldBreak(double Start, double End, string Subject) : MiloAction;
    /// <summary>Tạo sự kiện "Tập trung: #id" + presence DoNotDisturb tới hết khối.</summary>
    public sealed record StartFocus(string TaskId, string Title, double Start, double End) : MiloAction;
    public sealed record EndFocus : MiloAction;
    public sealed record OpenMail(MailItem Mail) : MiloAction;
    /// <summary>Tan tầm: lưu quả nho của ngày.</summary>
    public sealed record DayClosed(DayRecord Record) : MiloAction;
}

/// <summary>Bản ghi cuối ngày (mục 11 · Lưu cuối ngày).</summary>
public sealed record DayRecord(
    DateOnly Date, int Score, double MeetingMin, int AcceptedBreaks, double FocusMin, int TasksDone, double OvertimeMin,
    int VibeFocus, int VibeEnergy, int VibeStress, int InProgress);
