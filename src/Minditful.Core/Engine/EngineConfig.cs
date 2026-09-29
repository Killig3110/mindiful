namespace Minditful.Core.Engine;

public sealed class EngineConfig
{
    public double Start { get; set; } = Tm.T("09:00");
    /// <summary>Giờ kết thúc khung làm việc — Sandbox cho đổi lúc đang chạy để test Tan tầm ngay.</summary>
    public double End { get; set; } = Tm.T("18:00");
    /// <summary>Chỉ dùng cho ngày mẫu: giờ bắt đầu/kết thúc mô phỏng.</summary>
    public double DayOpen { get; init; } = Tm.T("08:50");
    public double DayClose { get; init; } = Tm.T("18:45");
    public double GapBudget { get; set; } = 15 * 60;
    public int PerHour { get; init; } = 3;
    public int PerDay { get; init; } = 10;
    public double TypingDefer { get; init; } = 300;
    public double Settle { get; init; } = 120;
    public double JumpWindow { get; init; } = 600;
    public double ParkTtl { get; set; } = 1800;
    /// <summary>Số lần chuyển việc/giờ bắt đầu bị tính là phân mảnh (spec: 8).</summary>
    public int FragThreshold { get; init; } = 8;
    /// <summary>Chào sáng chỉ khi lần mở máy đầu tiên trước giờ này (null = luôn chào). Spec: sau 04:00.</summary>
    public double? MorningHelloUntil { get; init; }
    /// <summary>Chạy theo kịch bản ngày mẫu (world/ui script, tự trả lời, dừng ở DayClose).</summary>
    public bool Scripted { get; init; }
    public uint Seed { get; init; } = 24;

    // ---- Ngưỡng hành vi (tài liệu mục 4, 7, 8, 9). Sandbox có thể rút gọn qua BehaviorOverrides để test trong 1 buổi;
    //      các ngưỡng này đổi được lúc đang chạy (Sandbox bật/tắt chế độ test). ----
    /// <summary>Task kẹt: số ngày làm việc ở Active (spec: 3).</summary>
    public int StuckMinDays { get; set; } = 3;
    /// <summary>Email chờ không giao trước giờ này vì bản tin sáng đã báo (spec: 10:00).</summary>
    public double EmailNotBefore { get; set; } = Tm.T("10:00");
    /// <summary>Làm liền: số phút không có lần nghỉ ≥ 5 phút (spec: 120).</summary>
    public double NoBreakMin { get; set; } = 120;
    /// <summary>Họp liên tục: số cuộc họp liền nhau tối thiểu (spec: 3).</summary>
    public int OverloadMinChain { get; set; } = 3;
    /// <summary>Ghé ngang mỗi 30–60 phút (spec §9.3).</summary>
    public double VisitMinMinutes { get; set; } = 30;
    public double VisitMaxMinutes { get; set; } = 60;

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

    // ---- Tính năng mở rộng. Mặc định tắt để ngày mẫu giữ đúng mốc của tài liệu; Sandbox/Prod bật qua mục "Wellbeing". ----
    /// <summary>Đề nghị giữ khoảng trống dài nhất trong ngày làm khối tập trung, tới giờ tự bật Không làm phiền.</summary>
    public bool FocusPlan { get; init; }
    /// <summary>Khoảng trống tối thiểu (phút) để đề nghị giữ giờ tập trung.</summary>
    public double FocusPlanMinMinutes { get; set; } = 60;
    /// <summary>Sáng thứ Hai: tóm tắt tuần trước kèm 1 mẹo (cần dữ liệu tuần trước trên máy).</summary>
    public bool WeekReport { get; init; }
    /// <summary>Nhắc nghỉ ngắn (uống nước, vươn vai) sau mỗi N phút làm liên tục (0 = tắt).</summary>
    public double MicroBreakEveryMin { get; set; }
    public int MicroBreakMaxPerDay { get; init; } = 6;
    /// <summary>Hỏi "Hôm nay thấy sao?" trên thẻ tan tầm và gợi ý nghỉ giữa chuỗi họp ngày mai.</summary>
    public bool EveningCheck { get; init; } = true;
}

/// <summary>Hành động Milo cần làm ra thế giới thật (Teams, Outlook, Azure Boards). Demo chỉ ghi log.</summary>
public abstract record MiloAction
{
    public sealed record JoinMeeting(CalendarEvent Event) : MiloAction;
    public sealed record OpenAttachment(CalendarEvent Event) : MiloAction;
    /// <summary>POST /me/events, showAs tentative.</summary>
    /// <param name="DayOffset">0 = hôm nay, 1 = ngày mai (giữ chỗ nghỉ giữa chuỗi họp ngày mai).</param>
    public sealed record HoldBreak(double Start, double End, string Subject, int DayOffset = 0) : MiloAction;
    /// <summary>Giữ trước 1 khối tập trung trong lịch (busy). Tới giờ engine tự <see cref="StartFocus"/>.</summary>
    public sealed record HoldFocus(double Start, double End) : MiloAction;
    /// <summary>Tạo sự kiện "Tập trung: #id" + presence DoNotDisturb tới hết khối. <paramref name="CalendarHeld"/>: lịch đã giữ từ trước, không tạo thêm.</summary>
    public sealed record StartFocus(string TaskId, string Title, double Start, double End, bool CalendarHeld = false) : MiloAction;
    public sealed record EndFocus : MiloAction;
    public sealed record OpenMail(MailItem Mail) : MiloAction;
    /// <summary>Tan tầm: lưu quả nho của ngày.</summary>
    public sealed record DayClosed(DayRecord Record) : MiloAction;
}

/// <summary>Bản ghi cuối ngày (mục 11 · Lưu cuối ngày).</summary>
public sealed record DayRecord(
    DateOnly Date, int Score, double MeetingMin, int AcceptedBreaks, double FocusMin, int TasksDone, double OvertimeMin,
    int VibeFocus, int VibeEnergy, int VibeStress, int InProgress, int Feeling = 0);

/// <summary>Người dùng tự nói hôm nay thấy sao (thẻ tan tầm). 0 = chưa trả lời.</summary>
public static class Feeling
{
    public const int Good = 3, Ok = 2, Bad = 1;
    public static int Parse(string? v) => v switch { "good" => Good, "ok" => Ok, "bad" => Bad, _ => 0 };
    public static string Label(int f) => f switch { Good => "Vui", Ok => "Bình thường", Bad => "Mệt", _ => "" };
}
