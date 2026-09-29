using Minditful.Core.Engine;
using Minditful.Integrations;
using Minditful.Integrations.Live;
using Xunit;
using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Tests;

/// <summary>Sandbox bật/tắt chế độ test lúc đang chạy: ngưỡng rút gọn ↔ ngưỡng chuẩn như Production.</summary>
public class SandboxModeTests
{
    private static readonly BehaviorOverrides Ov = new()
    {
        StuckTaskMinBusinessDays = 0, EmailMinBusinessDaysWaiting = 0, EmailNotBefore = "00:00", NoBreakStreakMin = 20,
        OverloadMinChainCount = 3, BudgetGapMin = 3, VisitEveryMin = [3, 5], ParkedReminderTtlMin = 5, MicroBreakEveryMin = 5, FocusPlanMinMinutes = 30,
    };

    [Fact]
    public void Test_mode_shortens_thresholds_and_production_mode_restores_the_standard()
    {
        var standard = new EngineConfig { MicroBreakEveryMin = 50, FocusPlan = true };
        var cfg = new EngineConfig { MicroBreakEveryMin = 50, FocusPlan = true };

        BehaviorProfile.Apply(cfg, standard, Ov, test: true);
        Assert.Equal((0, 0.0, 20.0, 180.0, 3.0, 5.0, 300.0, 5.0, 30.0),
            (cfg.StuckMinDays, cfg.EmailNotBefore, cfg.NoBreakMin, cfg.GapBudget, cfg.VisitMinMinutes, cfg.VisitMaxMinutes, cfg.ParkTtl, cfg.MicroBreakEveryMin, cfg.FocusPlanMinMinutes));
        Assert.Equal(0, BehaviorProfile.MailMinWaitDays(1, Ov, test: true));

        BehaviorProfile.Apply(cfg, standard, Ov, test: false);
        Assert.Equal((3, T("10:00"), 120.0, 900.0, 30.0, 60.0, 1800.0, 50.0, 60.0),
            (cfg.StuckMinDays, cfg.EmailNotBefore, cfg.NoBreakMin, cfg.GapBudget, cfg.VisitMinMinutes, cfg.VisitMaxMinutes, cfg.ParkTtl, cfg.MicroBreakEveryMin, cfg.FocusPlanMinMinutes));
        Assert.Equal(1, BehaviorProfile.MailMinWaitDays(1, Ov, test: false));
    }

    [Fact]
    public void Test_mode_never_turns_on_a_feature_the_user_turned_off()
    {
        var standard = new EngineConfig { MicroBreakEveryMin = 0 };
        var cfg = new EngineConfig { MicroBreakEveryMin = 0 };
        BehaviorProfile.Apply(cfg, standard, Ov, test: true);
        Assert.Equal(0, cfg.MicroBreakEveryMin);
    }

    [Fact]
    public void Switching_mode_while_running_changes_when_milo_speaks()
    {
        // Ngồi máy liên tục 25 phút: chế độ test (ngưỡng 20') nhắc Làm liền, như Production (120') thì chưa
        var standard = new EngineConfig();
        var cfg = new EngineConfig();
        BehaviorProfile.Apply(cfg, standard, Ov, test: false);
        var e = new MiloEngine(cfg, new WorkSnapshot(), null, new DateOnly(2026, 9, 29), T("10:00"));
        e.SetAuto(false);
        e.SetLocked(false);
        for (var i = 0; i < 400 && e.S.Ep is not { Phase: Phase.Show }; i++) e.Advance(0.5, false);
        e.UserReply("gotIt");
        e.Advance(25 * 60, false);
        Assert.DoesNotContain(e.S.Log, l => l.Text.StartsWith("Làm liền đủ điều kiện"));

        BehaviorProfile.Apply(e.Cfg, standard, Ov, test: true);
        e.Advance(90, false);
        Assert.Contains(e.S.Log, l => l.Text.StartsWith("Làm liền đủ điều kiện"));
    }
}
