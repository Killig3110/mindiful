namespace Minditful.Core.Engine;

// Thứ tự khai báo = thứ tự phá hoà trong hàng đợi (giống Object.keys(CASES) của prototype).
public enum CaseId
{
    Dashboard, MorningHello, MeetingSoon, EodWrapup, MeetingOverload, LowRest, Overtime, NoBreak,
    LunchMissed, HighFragmentation, EodNudge, CalendarPacked, StuckTask, EmailWaiting, TaskDone, FocusDone, CheckIn,
    // Tính năng mở rộng (sau prototype): thêm ở cuối để không đổi thứ tự phá hoà của 17 case gốc
    FocusPlan, WeekReport, MicroBreak,
}

public enum CaseKind { User, Social, Assist, Care }

public enum Clip
{
    Gone, HangPull, Hello, ClimbIn, PeekIn, JumpIn, StandUp, Stretch, Celebrate, Thanks, Greet, Reminder, Point, Breathe,
    Idle, IdleTired, LookAround, HoverPeek, ClimbOut, ClimbOutShort, ClimbOutFast, RunToCar, DigExhausted,
}

public enum Pose { Idle, Tired, Breathe, Wave, Point, Care, Greeting, Run }

public enum Gate { Off, Locked, Meeting, Fullscreen, Focus, Dnd, Presenting }

public enum PresenceState { Off, Hidden, Peek, Visit, Talk, Silent }

public enum Phase { Enter, Show, Breathe, Confirm, Thanks, Bubble, Chat, Exit }

public sealed record CaseDef(
    CaseId Id, int Pri, CaseKind Kind, string Name, string Color, string Ref,
    int Timeout = 0, Clip? Stay = null, string? Bg = null, string? Fg = null, int Sev = 0,
    int Snooze = 0, int Dismiss = 0, int MaxDay = 0, int Need = 0, bool Exempt = false);

public sealed record Band(string Id, int Min, string Label, double Saturation, string Grape, string Bg, string Fg);

public static class Catalog
{
    public const int CareTimeout = 45;

    public static readonly IReadOnlyDictionary<CaseId, CaseDef> Cases = new[]
    {
        new CaseDef(CaseId.Dashboard, 0, CaseKind.User, "Dashboard", "#7B83EB", "Mục 9.6 · B05, B06"),
        new CaseDef(CaseId.MorningHello, 1, CaseKind.Social, "Chào sáng", "#E8A33D", "Mục 6.1 · B01, B02", Timeout: 20),
        new CaseDef(CaseId.MeetingSoon, 1, CaseKind.Assist, "Sắp họp", "#5B5FC7", "Mục 7.1 · B04", Timeout: 60, Stay: Clip.Point),
        new CaseDef(CaseId.EodWrapup, 2, CaseKind.Social, "Tan tầm", "#7261B0", "Mục 6.2 · B16", Timeout: 60, Stay: Clip.Greet),
        new CaseDef(CaseId.MeetingOverload, 2, CaseKind.Care, "Họp liên tục", "#E8A33D", "Mục 8 · B13, B14", Bg: "#FBE3C0", Fg: "#7A4A0C", Sev: 3, Snooze: 15, Dismiss: 60, MaxDay: 3, Need: 5),
        new CaseDef(CaseId.LowRest, 2, CaseKind.Care, "Nghỉ quá ít", "#8C5BB5", "Mục 8 · §8b", Bg: "#E9DDF4", Fg: "#5A3380", Sev: 3, Snooze: 20, Dismiss: 90, MaxDay: 2, Need: 5),
        new CaseDef(CaseId.Overtime, 3, CaseKind.Care, "Quá giờ", "#D1495B", "Mục 8 · B13", Bg: "#F8D7DC", Fg: "#8A1F2B", Sev: 2, Snooze: 10, Dismiss: 60, MaxDay: 4, Need: 0),
        new CaseDef(CaseId.NoBreak, 3, CaseKind.Care, "Làm liền", "#3E8E9E", "Mục 8 · §8b", Bg: "#D5ECEF", Fg: "#1F5A63", Sev: 2, Snooze: 15, Dismiss: 60, MaxDay: 3, Need: 5),
        new CaseDef(CaseId.LunchMissed, 3, CaseKind.Care, "Chưa nghỉ trưa", "#7FA65A", "Mục 8 · B13, B18", Bg: "#E3EFD6", Fg: "#3E5A22", Sev: 2, Snooze: 15, Dismiss: 90, MaxDay: 2, Need: 5),
        new CaseDef(CaseId.HighFragmentation, 3, CaseKind.Care, "Phân mảnh", "#C07A2C", "Mục 8 · §8b, B09", Bg: "#F6E1C8", Fg: "#7A4A12", Sev: 2, Snooze: 30, Dismiss: 90, MaxDay: 2, Need: 5),
        new CaseDef(CaseId.EodNudge, 3, CaseKind.Social, "Nhắc lại tan tầm", "#7261B0", "Mục 6.3 · B16", Timeout: 30, Stay: Clip.Greet),
        new CaseDef(CaseId.CalendarPacked, 4, CaseKind.Assist, "Lịch kín", "#0F6CBD", "Mục 7.2 · B07", Sev: 2, Timeout: 45, Need: 5),
        new CaseDef(CaseId.StuckTask, 4, CaseKind.Assist, "Task kẹt", "#0078D4", "Mục 7.4 · B09", Sev: 2, Timeout: 45, Snooze: 60, Need: 45),
        new CaseDef(CaseId.EmailWaiting, 4, CaseKind.Assist, "Email chờ", "#0B4F8A", "Mục 7.3 · B08", Sev: 1, Timeout: 45, Need: 10),
        new CaseDef(CaseId.TaskDone, 5, CaseKind.Assist, "Task xong", "#7FA65A", "Mục 7.5 · B18", Exempt: true),
        new CaseDef(CaseId.FocusDone, 1, CaseKind.Assist, "Hết giờ tập trung", "#7FA65A", "Mục 7.4 · B09", Exempt: true),
        new CaseDef(CaseId.CheckIn, 5, CaseKind.Social, "Chào hỏi", "#E8A33D", "Mục 9.5 · §4.2", Timeout: 15, Stay: Clip.Greet, Need: 5),
        new CaseDef(CaseId.FocusPlan, 4, CaseKind.Assist, "Giữ giờ tập trung", "#2E7D6B", "Mở rộng · khoảng trống dài nhất", Sev: 1, Timeout: 45, Need: 5),
        new CaseDef(CaseId.WeekReport, 4, CaseKind.Social, "Báo cáo tuần", "#7261B0", "Mở rộng · sáng thứ Hai", Timeout: 45, Stay: Clip.Greet, Need: 5),
        new CaseDef(CaseId.MicroBreak, 5, CaseKind.Assist, "Uống nước · nhìn xa", "#3E8E9E", "Mở rộng · 20-20-20", Exempt: true),
    }.ToDictionary(c => c.Id);

    public static CaseDef Def(CaseId c) => Cases[c];

    /// <summary>Độ dài clip (giây) — khớp keyframes CSS của prototype.</summary>
    public static readonly IReadOnlyDictionary<Clip, double> ClipSeconds = new Dictionary<Clip, double>
    {
        [Clip.HangPull] = 3.4, [Clip.Hello] = 2.3, [Clip.ClimbIn] = 1.3, [Clip.PeekIn] = 1.8, [Clip.JumpIn] = 1.6,
        [Clip.StandUp] = .5, [Clip.Stretch] = 1.2, [Clip.Celebrate] = 3, [Clip.Thanks] = 1.9, [Clip.ClimbOut] = 4.2,
        [Clip.ClimbOutShort] = 2.6, [Clip.ClimbOutFast] = .7, [Clip.RunToCar] = 2.9, [Clip.DigExhausted] = 1.8, [Clip.LookAround] = 8,
    };

    public static readonly IReadOnlyDictionary<Clip, Pose> ClipPose = new Dictionary<Clip, Pose>
    {
        [Clip.HangPull] = Pose.Idle, [Clip.Hello] = Pose.Greeting, [Clip.ClimbIn] = Pose.Idle, [Clip.PeekIn] = Pose.Idle,
        [Clip.JumpIn] = Pose.Idle, [Clip.StandUp] = Pose.Idle, [Clip.Stretch] = Pose.Greeting, [Clip.Greet] = Pose.Wave,
        [Clip.Thanks] = Pose.Wave, [Clip.Reminder] = Pose.Care, [Clip.Point] = Pose.Point, [Clip.Breathe] = Pose.Breathe,
        [Clip.Celebrate] = Pose.Greeting, [Clip.ClimbOut] = Pose.Wave, [Clip.ClimbOutShort] = Pose.Idle, [Clip.ClimbOutFast] = Pose.Idle,
        [Clip.RunToCar] = Pose.Run, [Clip.DigExhausted] = Pose.Tired, [Clip.Idle] = Pose.Idle, [Clip.IdleTired] = Pose.Tired,
        [Clip.LookAround] = Pose.Idle, [Clip.HoverPeek] = Pose.Greeting, [Clip.Gone] = Pose.Idle,
    };

    public static double ClipLength(Clip c) => ClipSeconds.TryGetValue(c, out var s) ? s : 0;

    public static readonly Band[] Bands =
    [
        new("radiant", 80, "Mọng", 1, "ripe", "#6B4FC0", "#FFFFFF"),
        new("balanced", 60, "Cân bằng", 1, "mid", "#A58FE0", "#2B1B55"),
        new("tired", 40, "Mệt dần", .75, "pale", "#C5B7EC", "#3B2E66"),
        new("exhausted", 0, "Kiệt sức", .55, "dry", "#DDC787", "#4A3A12"),
    ];

    public static readonly IReadOnlyDictionary<Gate, string> GateLabel = new Dictionary<Gate, string>
    {
        [Gate.Off] = "Nghỉ làm", [Gate.Locked] = "Khoá máy", [Gate.Meeting] = "Đang họp",
        [Gate.Fullscreen] = "Toàn màn hình", [Gate.Focus] = "Giờ tập trung", [Gate.Dnd] = "Không làm phiền", [Gate.Presenting] = "Đang trình chiếu",
    };

    public static string ClipName(Clip k) => k switch
    {
        Clip.HangPull => "Bám mép + leo lên",
        Clip.Hello => "Hello!",
        Clip.ClimbIn => "ClimbIn",
        Clip.PeekIn => "leo chậm",
        Clip.JumpIn => "JumpIn từ mép phải",
        Clip.StandUp => "nảy lên",
        Clip.Stretch => "vươn vai",
        _ => k.ToString(),
    };
}
