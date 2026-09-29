using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;
using Minditful.Integrations.Llm;
using Xunit;

namespace Minditful.Core.Tests;

/// <summary>
/// Milo đoán loại cuộc họp từ tiêu đề + agenda ngay trên máy; luật và AI chỉ nhận nhãn, không bao giờ nhận chữ gốc.
/// </summary>
public class MeetingIntentTests
{
    [Theory]
    [InlineData("1:1 với lead", null, MeetingIntent.OneOnOne)]
    [InlineData("Weekly 1-1 Minh/Lan", null, MeetingIntent.OneOnOne)]
    [InlineData("Demo sprint 42", null, MeetingIntent.Present)]
    [InlineData("Thuyết trình kết quả Q3", null, MeetingIntent.Present)]
    [InlineData("thuyet trinh ket qua", null, MeetingIntent.Present)]      // gõ không dấu
    [InlineData("Sprint 43 planning", null, MeetingIntent.Decision)]
    [InlineData("Chốt scope release", null, MeetingIntent.Decision)]
    [InlineData("Architecture review", null, MeetingIntent.Decision)]
    [InlineData("Daily standup", null, MeetingIntent.Listen)]
    [InlineData("All-hands tháng 10", null, MeetingIntent.Listen)]
    [InlineData("Đào tạo an toàn thông tin", null, MeetingIntent.Listen)]
    [InlineData("Retro sprint 42", null, MeetingIntent.Workshop)]
    [InlineData("Brainstorm tính năng mới", null, MeetingIntent.Workshop)]
    [InlineData("Customer call", null, MeetingIntent.Unknown)]
    [InlineData("Họp với khách hàng", "Agenda: 1. Demo bản build mới 2. Hỏi đáp", MeetingIntent.Present)] // agenda bổ sung khi tiêu đề chung chung
    [InlineData("Daily", "Agenda: demo nhanh của An", MeetingIntent.Listen)]                              // tiêu đề rõ thì ưu tiên tiêu đề
    public void Classifies_title_and_agenda(string title, string? agenda, MeetingIntent expected) =>
        Assert.Equal(expected, MeetingIntents.Classify(title, agenda));

    [Fact]
    public void Organizing_a_daily_is_not_presenting()
    {
        var daily = new MeetingRequest("d", 15, 8, "Trình bày", true, "09:00", 0, 1, 60, false, false, "Ngồi nghe");
        var a = MeetingRules.Assess(daily);
        Assert.Equal("Ngồi nghe", a.Kind);
        Assert.Equal(1, a.Load);
        var unknown = MeetingRules.Assess(daily with { Intent = "" });
        Assert.Equal("Trình bày", unknown.Kind); // không đoán được thì giữ cách cũ: người tổ chức = trình bày
    }

    [Fact]
    public void Decision_and_workshop_are_heavier_and_need_a_short_break()
    {
        var baseReq = new MeetingRequest("x", 60, 5, "Bắt buộc", true, "10:00", 0, 1, 60, false, false);
        var plain = MeetingRules.Assess(baseReq);
        var decision = MeetingRules.Assess(baseReq with { Intent = "Ra quyết định" });
        var listen = MeetingRules.Assess(baseReq with { Intent = "Ngồi nghe" });
        Assert.True(decision.Load > plain.Load);
        Assert.True(listen.Load < plain.Load);
        Assert.True(decision.RecoveryMin >= 5);
    }

    [Fact]
    public void Ai_gets_the_label_but_never_the_title_or_agenda()
    {
        var e = DemoScenario.CreateEngine();
        var ev = e.Meetings.First(m => m.Id == "e1"); // "Sprint Planning"
        var req = e.BuildMeetingRequest(ev);
        Assert.Equal("Ra quyết định", req.Intent);
        var prompt = ClaudeLineWriter.MeetingUser(req);
        Assert.Contains("Ra quyết định", prompt);
        Assert.DoesNotContain("Sprint", prompt);
        Assert.DoesNotContain("Planning", prompt);
    }

    [Fact]
    public void Meeting_soon_card_shows_a_tip_for_the_kind_of_meeting()
    {
        var e = DemoScenario.CreateEngine();
        e.RunTo(DemoTour.FreeMoment);
        e.ForceCase(CaseId.MeetingSoon);
        for (var i = 0; i < 30 && e.S.Ep is not { C: CaseId.MeetingSoon, Phase: Phase.Show }; i++) e.Advance(1, false);
        var tips = Present.Card(e).Blocks.OfType<ParagraphBlock>().Select(p => p.Text).ToList();
        var ev = e.S.Ep!.Data.Ev!;
        var expected = MeetingIntents.Tip(MeetingIntents.Of(ev), MeetingRules.YouPresent(ev.Role, MeetingIntents.Label(MeetingIntents.Of(ev))));
        Assert.NotNull(expected);
        Assert.Contains(expected, tips);
    }
}
