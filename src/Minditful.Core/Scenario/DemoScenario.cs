using Minditful.Core.Engine;
using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Scenario;

public sealed record Milestone(string Time, string Label, double LeadSeconds);

/// <summary>Ngày mẫu Thứ Năm 24/9 — y hệt prototype "Milo sống" và mục 13 của tài liệu.</summary>
public static class DemoScenario
{
    public static readonly DateOnly Day = new(2026, 9, 24);

    public static EngineConfig Config() => new() { Scripted = true };

    public static WorkSnapshot Snapshot() => new()
    {
        Calendar =
        [
            new CalendarEvent
            {
                Id = "e1", Subject = "Sprint Planning", Start = T("09:30"), End = T("10:30"), Role = "Trình bày", Attachment = "Sprint 42 review.pptx",
                People = [new("TL", "#5471B0"), new("MH", "#7FA65A"), new("+5", "#B85A34")],
            },
            new CalendarEvent
            {
                Id = "e2", Subject = "Design sync", Start = T("13:30"), End = T("14:30"), Role = "Bắt buộc",
                People = [new("QN", "#B85A34"), new("DT", "#7261B0")],
            },
            new CalendarEvent
            {
                Id = "e3", Subject = "1:1 với lead", Start = T("14:30"), End = T("15:30"), Role = "Bắt buộc",
                People = [new("TL", "#5471B0")],
            },
            new CalendarEvent
            {
                Id = "e4", Subject = "Demo review", Start = T("15:30"), End = T("16:10"), Role = "Trình bày", Attachment = "Demo sprint 42.pptx",
                People = [new("TL", "#5471B0"), new("MH", "#7FA65A"), new("+6", "#B85A34")],
            },
        ],
        Tomorrow = new TomorrowInfo("9:00", "Daily"),
        // Mai có chuỗi 3 cuộc họp liền → thẻ tan tầm gợi ý giữ 10' nghỉ từ tối nay
        TomorrowCalendar =
        [
            new CalendarEvent { Id = "t1", Subject = "Daily", Start = T("09:00"), End = T("09:15") },
            new CalendarEvent { Id = "t2", Subject = "Architecture review", Start = T("13:30"), End = T("14:30") },
            new CalendarEvent { Id = "t3", Subject = "Sprint 43 planning", Start = T("14:30"), End = T("15:30") },
            new CalendarEvent { Id = "t4", Subject = "Customer call", Start = T("15:30"), End = T("16:15") },
        ],
        // Chuỗi 15 ngày về đúng giờ: đã mở khoá cả tủ đồ (đồng phục Bosch vừa mở hôm qua)
        // Ngày mẫu mở full tủ đồ để trình diễn: đủ chuỗi, lần nghỉ, giờ tập trung, và đã giữ đồ của mọi mùa
        Wardrobe = new WardrobeInfo(15, 15, "đồng phục Bosch", Breaks: 24, FocusMin: 320, Kept: ["lixi", "lantern", "witch", "santa"]),
        LastWeek = new WeekStats(new DateOnly(2026, 9, 14), 5, 68, new DayScore("T5", 81), new DayScore("T3", 42), 11 * 60 + 20, 3, 190, 7, 35,
            18, 8, 5, 3, 2, FeelGood: 2, FeelOk: 2, FeelBad: 1),
        ThisWeek = new WeekStats(new DateOnly(2026, 9, 21), 3, 58, new DayScore("T2", 77), new DayScore("T3", 38), 9 * 60 + 40, 4, 240, 7, 35,
            14, 6, 4, 2, 2, FeelGood: 1, FeelOk: 1, FeelBad: 1),
        Emails =
        [
            new MailItem { Id = "m1", From = "PM", Color = "#5471B0", Subject = "Chốt scope sprint 43?", Days = 2 },
            new MailItem { Id = "m2", From = "QA", Color = "#7FA65A", Subject = "Bug #4830 có cần fix gấp?", Days = 1 },
            new MailItem { Id = "m3", From = "HR", Color = "#B85A34", Subject = "Xác nhận lịch team building", Days = 1 },
            new MailItem { Id = "m4", From = "DT", Color = "#7261B0", Subject = "Review giúp PR #212?", Days = 1 },
            new MailItem { Id = "m5", From = "MH", Color = "#2B7A4B", Subject = "Tài liệu API bản mới?", Days = 1 },
        ],
        Unread = 7,
        Tasks =
        [
            new WorkTask { Id = "4821", Title = "Refactor login flow", Days = 4 },
            new WorkTask { Id = "4815", Title = "Cập nhật API docs", Days = 3 },
            new WorkTask { Id = "4830", Title = "Fix bug đăng nhập SSO", Days = 1 },
            new WorkTask { Id = "4833", Title = "Unit test module auth", Days = 2 },
            new WorkTask { Id = "4840", Title = "Tối ưu query báo cáo", Days = 1 },
            new WorkTask { Id = "4842", Title = "Review PR #212", Days = 0 },
        ],
        AvgInProgress = 2.8,
        Sprint = new SprintInfo("Sprint 42", 13, 21, 3),
        Week = [new("T4", 68), new("T5", 81), new("T6", 66), new("T2", 77), new("T3", 38), new("T4", 58)],
        YesterdayScore = 58,
    };

    public static ScenarioScript Script() => new()
    {
        World =
        [
            new(T("08:58"), "unlock"), new(T("10:30:30"), "away"), new(T("10:37"), "back"),
            new(T("12:15"), "lock"), new(T("12:55"), "unlock"), new(T("17:45"), "done", "4821"), new(T("18:32"), "lock"),
        ],
        Ui = [new(T("13:14"), "hover"), new(T("13:14:04"), "unhover"), new(T("13:15"), "openDash")],
        AutoReplies = new Dictionary<CaseId, (double, string)[]>
        {
            [CaseId.MorningHello] = [(4, "gotIt")],
            [CaseId.MeetingSoon] = [(3, "open"), (6, "join")],
            [CaseId.CalendarPacked] = [(5, "accept")],
            [CaseId.StuckTask] = [(5, "accept")],
            [CaseId.EmailWaiting] = [(5, "openMail")],
            [CaseId.MeetingOverload] = [(5, "accept")],
            [CaseId.NoBreak] = [(5, "accept")],
            [CaseId.LunchMissed] = [(5, "accept")],
            [CaseId.LowRest] = [(5, "snooze")],
            [CaseId.HighFragmentation] = [(5, "accept")],
            [CaseId.Overtime] = [(5, "accept")],
            [CaseId.CheckIn] = [(4, "thanks")],
            [CaseId.EodWrapup] = [(6, "extend")],
            [CaseId.EodNudge] = [(4, "goHome")],
            [CaseId.Dashboard] = [(4, "week"), (9, "close")],
            // Không có trong ngày mẫu (tắt sẵn); dùng khi Kịch bản trình diễn cho Milo làm từng case
            [CaseId.FocusPlan] = [(6, "accept")],
            [CaseId.WeekReport] = [(6, "gotIt")],
        },
    };

    public static readonly Milestone[] Milestones =
    [
        new("08:58", "Mở máy · Chào sáng + bản tin", 15), new("09:25", "Sắp họp · Sprint Planning", 8),
        new("09:30", "Đang họp · Milo im lặng", 3), new("10:37", "Quay lại máy · Task kẹt → khoá 90'", 6),
        new("12:07", "Hết giờ tập trung · Nghỉ quá ít", 6), new("12:55", "Sau bữa trưa · Lịch kín, giữ chỗ nghỉ", 6),
        new("13:10", "Email chờ bạn", 6), new("13:14", "Rê chuột lên đuôi · ló đầu", 4), new("13:15", "Dashboard · xem cả tuần", 4),
        new("13:25", "Sắp họp · Design sync", 6), new("14:56", "3 cuộc họp liền · chấm chờ", 4),
        new("16:10", "Hết họp · chờ 2 phút ổn định", 6), new("16:12", "Nhảy ra · Họp liên tục · thở 4-4-4", 12),
        new("17:01", "Ghé ngang ở dáng mệt", 6), new("17:45", "Task xong · Milo hồi màu", 6),
        new("18:00", "Tan tầm · Thêm 30 phút", 6), new("18:31", "Về thôi · chạy ra xe", 8),
    ];

    public static MiloEngine CreateEngine() => new(Config(), Snapshot(), Script(), Day);

    /// <summary>Ngày mẫu với hạt ngẫu nhiên khác (để thử những gì có yếu tố may rủi, vd. tính cách Pha trộn).</summary>
    public static MiloEngine CreateEngine(uint seed) => new(new EngineConfig { Scripted = true, Seed = seed }, Snapshot(), Script(), Day);
}
