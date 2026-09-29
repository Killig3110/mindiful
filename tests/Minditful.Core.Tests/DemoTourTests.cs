using Minditful.Core.Engine;
using Minditful.Core.Scenario;
using Xunit;

namespace Minditful.Core.Tests;

/// <summary>Kịch bản trình diễn của Demo: đi hết các bước thì Milo đã làm đủ mọi case, và mood realtime đổi dáng Milo ngay.</summary>
public class DemoTourTests
{
    [Fact]
    public void Tour_covers_every_case()
    {
        var covered = DemoTour.Steps.SelectMany(s => s.Expect ?? []).ToHashSet();
        Assert.Empty(Enum.GetValues<CaseId>().Where(c => !covered.Contains(c)));
    }

    [Fact]
    public void Each_step_makes_milo_deliver_what_it_promises()
    {
        var e = DemoScenario.CreateEngine();
        foreach (var step in DemoTour.Steps)
        {
            var from = e.S.Log.Count;
            DemoTour.Apply(e, step);
            if (e.S.Log.Count < from) from = 0; // tua tới mốc = chạy lại ngày từ đầu, nhật ký cũng làm lại
            var expect = step.Expect ?? [];
            for (var i = 0; i < 3600 && expect.Any(c => !Delivered(e, c, from)); i++) e.Advance(1, stopWhenBusy: false);
            foreach (var c in expect) Assert.True(Delivered(e, c, from), $"Bước \"{step.Title}\" chưa giao {Catalog.Def(c).Name}");
            for (var i = 0; i < 600 && e.S.Ep is not null; i++) e.Advance(1, stopWhenBusy: false); // Milo xong việc rồi mới sang bước sau
        }
    }

    private static bool Delivered(MiloEngine e, CaseId c, int from) =>
        e.S.Log.Skip(from).Any(l => l.Kind == LogKind.Deliver && l.Text.StartsWith($"**Giao {Catalog.Def(c).Name}**"));

    [Fact]
    public void Presenting_step_hides_milo_and_the_next_step_brings_him_back()
    {
        var e = DemoScenario.CreateEngine();
        DemoTour.Apply(e, DemoTour.Steps.First(s => s.Kind == TourKind.Presenting));
        Assert.Equal(PresenceState.Off, e.Presence());
        DemoTour.Apply(e, DemoTour.Steps.First(s => s.Kind == TourKind.Mood));
        Assert.False(e.S.Presenting);
    }

    [Fact]
    public void Mood_realtime_keeps_milo_out_and_changes_his_look_immediately()
    {
        var e = DemoScenario.CreateEngine();
        DemoTour.Apply(e, DemoTour.Steps.First(s => s.Kind == TourKind.Mood));
        e.Advance(3, false);
        Assert.Equal(PresenceState.Visit, e.Presence());
        e.Advance(60, false);
        Assert.Equal(PresenceState.Visit, e.Presence()); // vẫn đứng ngoài, không đi sau 8 giây

        var calm = e.S.Score;
        e.SetStressLevel(60);
        Assert.Equal(Math.Max(0, calm - 60), e.S.Score);
        Assert.Equal(3, e.S.BandIdx); // kiệt sức → dáng mệt, nhạt màu, chữ z
        Assert.Equal(Clip.IdleTired, Presentation.Present.VisualClip(e));

        e.SetStressLevel(0);
        e.SimulateBreak();
        Assert.True(e.S.Score > calm);
        Assert.True(e.S.BandIdx <= 1);

        e.CallMilo(false);
        e.Advance(20, false);
        Assert.NotEqual(PresenceState.Visit, e.Presence());
    }
}
