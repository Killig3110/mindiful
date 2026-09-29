using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;
using Xunit;
using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Tests;

/// <summary>Lỗi đã báo khi chạy trên Windows: thẻ đè lên Milo khi bấm chấm chờ; đang họp mất cả chóp đuôi.</summary>
public class SilentAndDotTests
{
    private static MiloEngine InMeeting()
    {
        var e = DemoScenario.CreateEngine();
        e.RunTo(T("14:56")); // giữa chuỗi 3 cuộc họp, đã có lời nhắc đang chờ
        Assert.Equal(PresenceState.Silent, e.Presence());
        return e;
    }

    [Fact]
    public void While_silent_the_tail_stays_as_a_dimmed_hint()
    {
        var e = InMeeting();
        Assert.True(Present.TailVisible(e));
        Assert.True(Present.TailDimmed(e));
        Assert.NotNull(Present.DotPill(e));
        e.SetPresenting(true); // trình chiếu thì vẫn ẩn hẳn
        Assert.False(Present.TailVisible(e));
    }

    [Fact]
    public void A_card_opened_from_the_dot_does_not_bring_milo_out_on_top_of_it()
    {
        var e = InMeeting();
        e.DotPillClick();
        for (var i = 0; i < 20 && e.S.Ep is not { Phase: Phase.Show }; i++) e.Advance(0.5, false);
        Assert.True(e.S.Ep!.FromDot);
        Assert.Equal(Clip.Gone, Present.VisualClip(e)); // chỉ thẻ, Milo vẫn ẩn
        var card = Present.Card(e);
        Assert.NotEqual(CardVariant.None, card.Variant);
        Assert.True(card.Low);
    }

    [Fact]
    public void Normal_episodes_still_show_milo()
    {
        var e = DemoScenario.CreateEngine();
        e.RunTo(T("08:57:50"));
        for (var i = 0; i < 200 && e.S.Ep is not { Phase: Phase.Show }; i++) e.Advance(0.5, false);
        Assert.False(e.S.Ep!.FromDot);
        Assert.NotEqual(Clip.Gone, Present.VisualClip(e));
    }
}
