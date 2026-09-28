using Microsoft.Extensions.Configuration;
using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;
using Minditful.Integrations;
using Xunit;
using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Tests;

/// <summary>Đánh giá cảm xúc (mood) và đánh giá cuộc họp: 2 hướng luật / Claude, bật tắt bằng cấu hình.</summary>
public class InsightTests
{
    private static MiloEngine Engine(MoodMode mood = MoodMode.Rules, MeetingMode meet = MeetingMode.Rules, string at = "09:00") =>
        new(new EngineConfig { MoodMode = mood, MeetingMode = meet, MoodIntervalMinutes = 30 }, DemoScenario.Snapshot(), null, DemoScenario.Day, T(at));

    // ================= đánh giá cuộc họp =================
    [Fact]
    public void Every_meeting_gets_a_rule_assessment_without_llm()
    {
        var e = Engine();
        foreach (var m in e.Meetings) Assert.Equal("Luật", e.Assessment(m.Id)!.Source);
        var planning = e.Assessment("e1")!;   // 60', 8 người, trình bày
        Assert.Equal("Trình bày", planning.Kind);
        Assert.Equal(4, planning.Load);
        var oneOnOne = e.Assessment("e3")!;   // 1:1 với lead, cuộc thứ 2 trong chuỗi
        Assert.Equal("1:1", oneOnOne.Kind);
        var demo = e.Assessment("e4")!;       // cuộc thứ 3 liền nhau, trình bày → nặng
        Assert.True(demo.Load >= 4);
        Assert.Contains("3/3", demo.Note);
    }

    [Fact]
    public void Meeting_request_never_contains_the_title()
    {
        var e = Engine();
        foreach (var m in e.Meetings)
        {
            var req = e.BuildMeetingRequest(m);
            Assert.DoesNotContain(m.Subject, req.ToString());
        }
        Assert.Equal(3, e.BuildMeetingRequest(e.Meetings.Single(m => m.Id == "e4")).ChainLength);
    }

    [Fact]
    public void Llm_meeting_mode_asks_claude_and_keeps_its_answer()
    {
        var e = Engine(meet: MeetingMode.Llm);
        var asked = new List<MeetingRequest>();
        e.MeetingWanted += asked.Add;
        e.ReassessMeetings();
        Assert.Equal(4, asked.Count);
        e.SetMeetingAssessment(new MeetingAssessment("e2", 2, "Trao đổi", 0, "sync ngắn, ít người", "Claude"));
        Assert.Equal("Claude", e.Assessment("e2")!.Source);
        e.ReassessMeetings(); // chỉ hỏi lại những cuộc còn là "Luật"
        Assert.Equal(7, asked.Count);
    }

    [Fact]
    public void Dashboard_shows_meeting_load()
    {
        var e = new MiloEngine(new EngineConfig(), new WorkSnapshot { Calendar = DemoScenario.Snapshot().Calendar }, null, DemoScenario.Day, T("11:00"));
        e.SetAuto(false);
        e.SetLocked(false);
        for (var i = 0; i < 200 && e.S.Ep is not { Phase: Phase.Show }; i++) e.Advance(0.25, false);
        e.UserReply("gotIt");
        for (var i = 0; i < 200 && e.S.Ep is not null; i++) e.Advance(0.25, false);
        e.S.Queue.Clear();
        e.TailClick();
        for (var i = 0; i < 40 && e.S.Ep is not { C: CaseId.Dashboard, Phase: Phase.Show }; i++) e.Advance(0.25, false);
        e.UserReply("week");
        var rows = Present.Dashboard(e)!.Events.Where(r => r.Load is not null).ToList();
        Assert.Equal(4, rows.Count);
        Assert.All(rows, r => Assert.Contains("nặng", r.Note));
    }

    [Fact]
    public void Grape_cluster_always_has_7_slots_even_without_history()
    {
        var e = new MiloEngine(new EngineConfig(), new WorkSnapshot(), null, DemoScenario.Day, T("11:00"));
        e.SetAuto(false);
        e.SetLocked(false);
        for (var i = 0; i < 200 && e.S.Ep is not { Phase: Phase.Show }; i++) e.Advance(0.25, false);
        e.UserReply("gotIt");
        for (var i = 0; i < 200 && e.S.Ep is not null; i++) e.Advance(0.25, false);
        e.TailClick();
        for (var i = 0; i < 40 && e.S.Ep is not { C: CaseId.Dashboard, Phase: Phase.Show }; i++) e.Advance(0.25, false);
        var d = Present.Dashboard(e)!;
        Assert.Equal(7, d.Days.Count);
        Assert.Equal(6, d.Days.Count(x => x.Score < 0));   // máy mới: 6 quả mờ + quả hôm nay
        Assert.Equal("Nay", d.Days[^1].Label);
        Assert.Contains("Chưa có dữ liệu hôm qua", d.YesterdayLine);
    }

    // ================= đánh giá cảm xúc (mood) =================
    [Fact]
    public void Rules_mode_never_asks_claude()
    {
        var e = Engine();
        var asked = 0;
        e.MoodWanted += _ => asked++;
        e.SetLocked(false);
        e.Advance(3 * 3600, false);
        Assert.Equal(0, asked);
    }

    [Fact]
    public void Hybrid_asks_every_interval_and_adjusts_at_most_10_points()
    {
        var e = Engine(MoodMode.Hybrid);
        var asked = new List<MoodRequest>();
        e.MoodWanted += asked.Add;
        e.SetAuto(false);
        e.SetLocked(false);
        e.Advance(65 * 60, false);
        Assert.Equal(3, asked.Count);  // lúc mở máy, +30', +60'
        Assert.DoesNotContain("Sprint Planning", asked[^1].Facts);   // không lộ tiêu đề
        var rule = e.S.RuleScore;
        e.SetMoodInsight(new MoodInsight(40, -25, 2, 2, 4, "Mệt dần", "Bạn họp căng cả sáng, đứng dậy duỗi vai nhé.", "Claude", 0));
        Assert.Equal(Math.Max(0, rule - 10), e.S.Score);            // chỉnh bị chặn ở ±10
        Assert.Equal((2, 2, 4), e.S.Vibe);
        Assert.Equal(10, e.S.Pen["llm"]);
    }

    [Fact]
    public void Llm_mode_uses_claude_score_and_falls_back_to_rules_when_stale()
    {
        var e = Engine(MoodMode.Llm);
        e.SetAuto(false);
        e.SetLocked(false);
        e.Advance(60, false);
        e.SetMoodInsight(new MoodInsight(47, 0, 3, 2, 3, "Mệt dần", "Nghỉ 5 phút cho mắt nhé.", "Claude", 0));
        Assert.Equal(47, e.S.Score);
        e.Advance(61 * 60, false); // quá 2 chu kỳ không có nhận xét mới → về điểm luật
        Assert.Equal(e.S.RuleScore, e.S.Score);
    }

    [Fact]
    public void Chat_is_sent_for_mood_only_when_enabled()
    {
        var e = Engine(MoodMode.Hybrid);
        e.S.ChatHistory.Add("mệt quá trời");
        Assert.Empty(e.BuildMoodRequest().Chat);
        e.Cfg.IncludeChatInMood = true;
        Assert.Equal(["mệt quá trời"], e.BuildMoodRequest().Chat);
    }

    // ================= công tắc =================
    [Fact]
    public void Feature_toggles_bind_from_env_style_keys()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Minditful:Llm:Features:Mood"] = "hybrid",
            ["Minditful:Llm:Features:Meetings"] = "Llm",
            ["Minditful:Llm:Features:Lines"] = "false",
        }).Build();
        var opt = cfg.GetSection("Minditful").Get<MinditfulOptions>()!;
        Assert.Equal(MoodMode.Hybrid, opt.Llm.Features.MoodMode);
        Assert.Equal(MeetingMode.Llm, opt.Llm.Features.MeetingMode);
        Assert.False(opt.Llm.Features.Lines);
        Assert.Equal(MoodMode.Rules, new LlmFeatures { Mood = "gì đó" }.MoodMode); // sai tên → luật
    }
}
