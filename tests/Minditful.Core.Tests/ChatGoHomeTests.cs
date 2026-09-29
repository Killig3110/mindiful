using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;
using Xunit;
using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Tests;

/// <summary>Chat "về thôi" / "đồng ý" trên thẻ: tan tầm thì chạy ra xe, thẻ khác thì bấm nút chính.</summary>
public class ChatGoHomeTests
{
    private static MiloEngine Show(CaseId c, string at = "11:00")
    {
        var e = new MiloEngine(new EngineConfig(), new WorkSnapshot { Calendar = DemoScenario.Snapshot().Calendar }, null, DemoScenario.Day, T(at));
        e.SetAuto(false);
        e.SetLocked(false);
        for (var i = 0; i < 200 && e.S.Ep is not { Phase: Phase.Show }; i++) e.Advance(0.25, false);
        e.UserReply("gotIt");
        e.S.Queue.Clear();
        for (var i = 0; i < 200 && e.S.Ep is not null; i++) e.Advance(0.25, false);
        e.ForceCase(c);
        for (var i = 0; i < 200 && e.S.Ep is not { Phase: Phase.Show }; i++) e.Advance(0.25, false);
        Assert.Equal(c, e.S.Ep!.C);
        return e;
    }

    [Theory]
    [InlineData("về thôi")]
    [InlineData("oke đi về")]
    [InlineData("đồng ý")]
    [InlineData("ok")]
    public void End_of_day_chat_runs_to_the_car(string text)
    {
        var e = Show(CaseId.EodWrapup, "18:01");
        Assert.Contains(Present.Card(e).Blocks, b => b is ChatBlock);   // thẻ tan tầm có ô chat
        e.UserReply("chat", text);
        e.Advance(2, false);
        Assert.Equal(Clip.RunToCar, e.S.Ep!.Clip);
        for (var i = 0; i < 40 && e.S.Ep is not null; i++) e.Advance(0.25, false);
        Assert.True(e.S.OffDuty);
    }

    [Fact]
    public void End_of_day_chat_not_yet_means_30_more_minutes()
    {
        var e = Show(CaseId.EodWrapup, "18:01");
        e.UserReply("chat", "chưa, còn bận chút");
        e.Advance(2, false);
        Assert.NotNull(e.S.ExtendedUntil);
        Assert.False(e.S.OffDuty);
    }

    [Fact]
    public void Overtime_chat_go_home_runs_to_the_car()
    {
        var e = Show(CaseId.Overtime, "18:45");
        e.UserReply("chat", "ừ về nhà đây");
        e.Advance(2, false);
        Assert.Equal(Clip.RunToCar, e.S.Ep!.Clip);
    }

    [Fact]
    public void Yes_on_a_care_card_presses_accept()
    {
        var e = Show(CaseId.NoBreak);
        e.UserReply("chat", "đồng ý nha");
        e.Advance(2, false);
        Assert.Equal(Phase.Breathe, e.S.Ep!.Phase);
    }

    [Fact]
    public void Negated_yes_is_not_accept()
    {
        var e = Show(CaseId.NoBreak);
        e.UserReply("chat", "không đồng ý");
        e.Advance(2, false);
        Assert.True(e.S.Mem[CaseId.NoBreak].DismissedUntil > e.S.T);
    }
}
