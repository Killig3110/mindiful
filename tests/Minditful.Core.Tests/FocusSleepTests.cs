using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;
using Xunit;

namespace Minditful.Core.Tests;

/// <summary>Lúc bạn tập trung, Milo nằm ngủ trên chóp đuôi thay vì chỉ còn chóp đuôi mờ; hết giờ thì tỉnh.</summary>
public class FocusSleepTests
{
    private static MiloEngine Focusing()
    {
        var e = DemoScenario.CreateEngine();
        e.SetAuto(false);
        DemoTour.RunCase(e, CaseId.HighFragmentation);
        for (var i = 0; i < 60 && e.S.Ep?.Phase != Phase.Show; i++) e.Advance(1, stopWhenBusy: false);
        e.UserReply("accept"); // "Tập trung 30 phút" → khoá tập trung + Không làm phiền
        for (var i = 0; i < 60 && e.S.Ep is not null; i++) e.Advance(1, stopWhenBusy: false);
        return e;
    }

    [Fact]
    public void Milo_sleeps_on_the_tail_during_focus_and_says_until_when()
    {
        var e = Focusing();
        Assert.Equal(Gate.Focus, e.HardGate());
        Assert.True(Present.Sleeping(e));
        Assert.True(Present.TailVisible(e));
        Assert.Contains(Tm.Hm(e.S.FocusUntil!.Value), Present.SleepText(e));
    }

    [Fact]
    public void Milo_wakes_up_when_focus_ends()
    {
        var e = Focusing();
        e.Advance(31 * 60, false);
        Assert.False(Present.Sleeping(e));
        Assert.Null(Present.SleepText(e));
    }

    [Fact]
    public void Meetings_and_presenting_keep_the_plain_dim_tail()
    {
        var e = DemoScenario.CreateEngine();
        e.RunTo(DemoTour.FreeMoment);
        e.SetPresenting(true);
        Assert.False(Present.Sleeping(e));
        var m = DemoScenario.CreateEngine();
        var meeting = m.Meetings.OrderBy(x => x.Start).First(x => x.Start > m.Cfg.Start);
        m.RunTo(meeting.Start + 120);
        Assert.Equal(Gate.Meeting, m.HardGate());
        Assert.False(Present.Sleeping(m));
    }
}
