using Minditful.Core.Engine;

namespace Minditful.Integrations.Live;

/// <summary>
/// Sandbox có 2 chế độ: <b>test</b> (ngưỡng rút gọn trong <see cref="BehaviorOverrides"/>, để thấy mọi hành vi trong 1 buổi)
/// và <b>như Production</b> (ngưỡng chuẩn của tài liệu). Đổi qua lại lúc đang chạy, không cần mở lại app.
/// </summary>
public static class BehaviorProfile
{
    /// <param name="standard">Cấu hình chuẩn (ngưỡng tài liệu + mục Wellbeing), dùng khi tắt chế độ test.</param>
    public static void Apply(EngineConfig cfg, EngineConfig standard, BehaviorOverrides? ov, bool test)
    {
        var o = test ? ov : null;
        cfg.StuckMinDays = o?.StuckTaskMinBusinessDays ?? standard.StuckMinDays;
        cfg.EmailNotBefore = o?.EmailNotBefore is { } nb ? Tm.T(nb) : standard.EmailNotBefore;
        cfg.NoBreakMin = o?.NoBreakStreakMin ?? standard.NoBreakMin;
        cfg.OverloadMinChain = o?.OverloadMinChainCount ?? standard.OverloadMinChain;
        cfg.GapBudget = o?.BudgetGapMin is { } gap ? gap * 60 : standard.GapBudget;
        cfg.VisitMinMinutes = o?.VisitEveryMin is [var vmin, _] ? vmin : standard.VisitMinMinutes;
        cfg.VisitMaxMinutes = o?.VisitEveryMin is [_, var vmax] ? vmax : standard.VisitMaxMinutes;
        cfg.ParkTtl = o?.ParkedReminderTtlMin is { } park ? park * 60 : standard.ParkTtl;
        // Chế độ test chỉ rút ngắn nhịp nhắc, không bật lại tính năng người dùng đã tắt
        cfg.MicroBreakEveryMin = o?.MicroBreakEveryMin is { } mb && standard.MicroBreakEveryMin > 0 ? mb : standard.MicroBreakEveryMin;
        cfg.FocusPlanMinMinutes = o?.FocusPlanMinMinutes ?? standard.FocusPlanMinMinutes;
    }

    /// <summary>Chu kỳ đọc email / lịch / Boards: Sandbox đọc nhanh để demo "gửi mail là Milo báo"; Production giữ chu kỳ chuẩn.</summary>
    public static void ApplyPolling(PollingOptions target, PollingOptions standard, BehaviorOverrides? ov, bool fast)
    {
        var o = fast ? ov : null;
        target.MailSeconds = o?.MailPollSeconds ?? standard.MailSeconds;
        target.CalendarSeconds = o?.CalendarPollSeconds ?? standard.CalendarSeconds;
        target.BoardsSeconds = o?.BoardsPollSeconds ?? standard.BoardsSeconds;
    }

    /// <summary>Số ngày email phải chờ trước khi Milo nhắc.</summary>
    public static int MailMinWaitDays(int configured, BehaviorOverrides? ov, bool test) =>
        test && ov?.EmailMinBusinessDaysWaiting is { } d ? d : configured;
}
