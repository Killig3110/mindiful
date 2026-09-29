using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;
using Xunit;
using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Tests;

public class PresentationTests
{
    /// <summary>Dựng mọi thứ UI sẽ vẽ ở từng bước của ngày mẫu — không được ném lỗi.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Every_frame_of_the_sample_day_renders(bool auto)
    {
        var e = DemoScenario.CreateEngine();
        e.SetAuto(auto);
        var cards = new HashSet<CaseId>();
        while (!e.S.Ended)
        {
            e.Advance(e.Busy ? 0.25 : 5);
            var clip = Present.VisualClip(e);
            _ = Present.PoseFor(e, clip);
            _ = ClipAnimation.Evaluate(clip, 0.3);
            _ = Present.Caption(e);
            _ = Present.Whisper(e);
            _ = Present.TailBadge(e);
            _ = Present.DotPill(e);
            _ = Present.Breathe(e);
            var card = Present.Card(e);
            if (card.Variant == CardVariant.Card && e.S.Ep is { } ep) cards.Add(ep.C);
            _ = Present.Dashboard(e);
            _ = Present.Fruits(e);
            // Không tự trả lời: bấm nút đầu tiên của thẻ như một người dùng thật
            if (!auto && e.S.Ep is { Phase: Phase.Show } cur && e.S.T - cur.PhaseStart > 3)
            {
                var first = card.Blocks.OfType<ButtonsBlock>().SelectMany(b => b.Buttons).FirstOrDefault(b => b.Enabled);
                e.UserReply(first?.Act ?? (cur.C == CaseId.Dashboard ? "close" : "dismiss"));
            }
        }
        Assert.Contains(CaseId.MorningHello, cards);
        Assert.Contains(CaseId.MeetingSoon, cards);
        Assert.Contains(CaseId.EodWrapup, cards);
    }

    [Fact]
    public void Chat_intents_follow_spec_section_10()
    {
        var e = DemoScenario.CreateEngine();
        e.RunTo(T("16:12") - 12); // ngay trước khi Milo nhảy ra với Họp liên tục
        e.SetAuto(false);
        while (e.S.Ep is not { Phase: Phase.Show }) e.Advance(0.25);
        Assert.Equal(CaseId.MeetingOverload, e.S.Ep.C);
        e.UserReply("chat", "mệt quá");
        e.Advance(2);
        Assert.Equal(Phase.Breathe, e.S.Ep!.Phase);
    }

    [Fact]
    public void Clip_animations_start_hidden_and_end_on_stage()
    {
        Assert.Equal(175, ClipAnimation.Evaluate(Clip.Gone, 0).Ty);
        Assert.Equal(0, ClipAnimation.Evaluate(Clip.ClimbIn, 5).Ty, 3);
        Assert.Equal(170, ClipAnimation.Evaluate(Clip.ClimbIn, 0).Ty, 3);
        Assert.Equal(175, ClipAnimation.Evaluate(Clip.ClimbOut, 10).Ty, 3);
        Assert.Equal(-1, ClipAnimation.Evaluate(Clip.LookAround, 2).Sx, 3);
    }

    [Fact]
    public void Forced_case_survives_rule_filter_until_delivered()
    {
        var e = new MiloEngine(new EngineConfig(), new WorkSnapshot(), null, DemoScenario.Day, T("11:00"));
        e.SetLocked(false);
        e.Advance(30);
        while (e.S.Ep is not null) { e.UserReply("gotIt"); e.Advance(1); }
        e.ForceCase(CaseId.NoBreak);
        e.Advance(90, stopWhenBusy: false);
        Assert.True(e.S.Log.Any(l => l.Text.StartsWith("**Giao Làm liền**")), string.Join("\n", e.S.Log.Select(l => Hm(l.T) + " " + l.Text)));
    }
}
