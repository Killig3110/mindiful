using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;
using Xunit;
using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Tests;

/// <summary>
/// Tính cách Milo: Dễ thương giữ nguyên hành vi cũ, Hài hước gặp dịp là diễn meme, Pha trộn lâu lâu mới hài.
/// Clip hài không bao giờ chạy lúc Milo phải im lặng.
/// </summary>
public class PersonalityTests
{
    private static void Until(MiloEngine e, Func<bool> done, double maxSec = 600)
    {
        var end = e.S.T + maxSec;
        while (!done())
        {
            Assert.True(e.S.T < end, $"Quá {maxSec}s lúc {Hm(e.S.T)}: " + string.Join(" | ", e.S.Log.TakeLast(5).Select(l => l.Text)));
            e.Advance(0.5, stopWhenBusy: false);
        }
    }

    private static MiloEngine Free(Personality p, uint? seed = null)
    {
        var e = seed is null ? DemoScenario.CreateEngine() : DemoScenario.CreateEngine(seed.Value);
        e.Cfg.Personality = p;
        e.SetAuto(false);
        e.RunTo(DemoTour.FreeMoment);
        return e;
    }

    private static Clip ShowClipOf(MiloEngine e, CaseId c)
    {
        e.ForceCase(c);
        Until(e, () => e.S.Ep is { } ep && ep.C == c && ep.Phase is Phase.Show or Phase.Bubble);
        return Present.VisualClip(e);
    }

    [Theory]
    [InlineData(Personality.Cute, CaseId.TaskDone, Clip.Celebrate)]
    [InlineData(Personality.Funny, CaseId.TaskDone, Clip.Slay)]
    [InlineData(Personality.Cute, CaseId.EmailWaiting, Clip.Point)]
    [InlineData(Personality.Funny, CaseId.EmailWaiting, Clip.SideEye)]
    [InlineData(Personality.Funny, CaseId.StuckTask, Clip.Confused)]
    [InlineData(Personality.Cute, CaseId.Overtime, Clip.Reminder)]
    [InlineData(Personality.Funny, CaseId.Overtime, Clip.ThisIsFine)]
    [InlineData(Personality.Funny, CaseId.MeetingOverload, Clip.Zombie)]
    public void Cute_never_jokes_funny_always_does(Personality p, CaseId c, Clip expected)
    {
        var e = Free(p);
        Assert.Equal(expected, ShowClipOf(e, c));
    }

    [Fact]
    public void Mixed_is_mostly_cute_and_sometimes_funny()
    {
        var slay = 0;
        const int n = 40;
        for (uint seed = 1; seed <= n; seed++)
            if (ShowClipOf(Free(Personality.Mixed, seed), CaseId.TaskDone) == Clip.Slay) slay++;
        Assert.InRange(slay, 4, n / 2); // khoảng 1/3 số dịp, không bao giờ áp đảo
    }

    [Fact]
    public void Monday_morning_hello_loads_the_new_week()
    {
        var monday = new DateOnly(2026, 9, 28);
        var e = new MiloEngine(new EngineConfig { Personality = Personality.Funny }, DemoScenario.Snapshot(), null, monday, T("09:00"));
        e.SetAuto(false);
        e.SetLocked(false);
        Until(e, () => e.S.Ep is { C: CaseId.MorningHello, Phase: Phase.Show });
        Assert.Equal(Clip.Loading, Present.VisualClip(e));
    }

    [Fact]
    public void Five_quick_pokes_make_milo_faint_unless_cute()
    {
        var e = Free(Personality.Mixed);
        e.OpenTalk();
        Until(e, () => e.S.Ep?.Phase == Phase.Show);
        for (var i = 0; i < 4; i++) e.MiloClick();
        Assert.NotEqual(Clip.Faint, Present.VisualClip(e));
        e.MiloClick();
        Assert.Equal(Clip.Faint, Present.VisualClip(e));
        e.Advance(4, false);
        Assert.NotEqual(Clip.Faint, Present.VisualClip(e));

        var calm = Free(Personality.Cute);
        calm.OpenTalk();
        Until(calm, () => calm.S.Ep?.Phase == Phase.Show);
        for (var i = 0; i < 6; i++) calm.MiloClick();
        Assert.NotEqual(Clip.Faint, Present.VisualClip(calm));
    }

    [Fact]
    public void Play_meme_brings_milo_out_then_performs()
    {
        var e = Free(Personality.Cute); // xem thử ở bảng điều khiển Demo chạy với mọi tính cách
        Assert.Null(e.S.Visit);
        e.PlayMeme(Clip.Vibe);
        Assert.NotNull(e.S.Visit);
        e.Advance(Catalog.ClipLength(Clip.PeekIn) + .5, false);
        Assert.Equal(Clip.Vibe, Present.VisualClip(e));
    }

    [Fact]
    public void Memes_never_play_while_milo_must_stay_quiet()
    {
        var e = Free(Personality.Funny);
        e.SetPresenting(true);
        e.PlayMeme(Clip.Slay);
        Assert.Null(e.S.Visit);
        Assert.Equal(PresenceState.Off, e.Presence());
    }

    [Fact]
    public void Coming_back_after_a_long_absence_shows_the_cobweb_visit()
    {
        // Ngày trống lịch để không có cuộc họp nào bắt Milo im lặng
        var e = new MiloEngine(new EngineConfig { Personality = Personality.Funny }, new WorkSnapshot(), null, new DateOnly(2026, 9, 29), T("10:00"));
        e.SetAuto(false);
        e.SetLocked(false);
        Until(e, () => e.S.DayStarted && e.S.Ep is null && e.S.Visit is null, 900);
        e.SetAway(true);
        e.Advance(31 * 60, false);
        Until(e, () => e.S.Ep is null && e.S.Visit is null, 900);
        e.SetAway(false);
        Until(e, () => e.S.Visit is { Phase: VisitPhase.Look }, 300); // Chào sáng (bị hoãn lúc vắng) được ưu tiên trước
        Assert.Equal(Clip.Cobweb, e.S.Visit!.Clip);
    }

    [Fact]
    public void Friday_afternoon_visit_is_a_vibe()
    {
        var friday = new DateOnly(2026, 9, 25);
        var e = new MiloEngine(new EngineConfig { Personality = Personality.Funny, VisitMinMinutes = 1, VisitMaxMinutes = 2 }, new WorkSnapshot(), null, friday, T("15:10"));
        e.SetAuto(false);
        e.SetLocked(false);
        Until(e, () => e.S.Visit is { Phase: VisitPhase.Look } && e.S.Ep is null, 3600);
        Assert.Equal(Clip.Vibe, e.S.Visit!.Clip);
    }
}
