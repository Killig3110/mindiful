namespace Minditful.Core.Engine;

public sealed class EngineConfig
{
    public double Start { get; init; } = Tm.T("09:00");
    public double End { get; init; } = Tm.T("18:00");
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
