using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;
using Minditful.Integrations.Llm;
using Xunit;

namespace Minditful.Core.Tests;

/// <summary>
/// Chat đề nghị tính năng: người dùng nói "mệt mà còn nhiều task" thì Milo hiện nút tính năng hợp (AI chọn, hoặc từ khoá khi không có AI).
/// AI chỉ được chọn trong danh sách cho phép và đang dùng được; bấm nút mới chạy.
/// </summary>
public class ChatActionTests
{
    private static MiloEngine OpenTalk()
    {
        var e = DemoScenario.CreateEngine();
        DemoTour.RunCase(e, CaseId.Talk);
        for (var i = 0; i < 20 && e.S.Ep?.Phase != Phase.Show; i++) e.Advance(1, stopWhenBusy: false);
        return e;
    }

    [Fact]
    public void Tired_and_too_many_tasks_suggests_one_feature_for_each()
    {
        var e = OpenTalk();
        var s = Talk.Suggest(e, "tui cũng khá mệt mà còn nhiều task quá");
        Assert.Equal(2, s.Count);
        Assert.Contains(s[0], new[] { "planFocus", "focus30", "stuck" });
        Assert.Equal("breathe", s[1]);
        Assert.Empty(Talk.Suggest(e, "cảm ơn Milo nha"));
    }

    [Fact]
    public void Without_ai_the_talk_reply_carries_feature_buttons()
    {
        var e = OpenTalk();
        e.UserReply("chat", "mệt quá, deadline dí sát nút");
        Assert.NotEmpty(e.S.Ep!.Chat[^1].Actions!);
        var chat = Present.Card(e).Blocks.OfType<ChatBlock>().Single();
        Assert.All(chat.Suggested!, b => Assert.Equal("do", b.Act));
        Assert.Contains(chat.Suggested!, b => b.Label == Talk.Actions[e.S.Ep.Chat[^1].Actions![0]].Label);
    }

    [Fact]
    public void Ai_actions_are_filtered_to_the_allowlist_and_what_is_available()
    {
        var e = OpenTalk();
        var asked = new List<(int Ep, int Idx, ChatRequest Req)>();
        e.ChatWanted += (ep, idx, req) => asked.Add((ep, idx, req));
        e.UserReply("chat", "hơi đuối");
        Assert.Equal(Talk.Available(e), asked[0].Req.Offer);
        e.ResolveChat(asked[0].Ep, asked[0].Idx, "Nghe đuối thật, thở chậm cùng Milo 1 phút nha.", ["deleteAllMail", "breathe", "breathe", "break15", "focus30"]);
        Assert.Equal(["breathe", "break15"], e.S.Ep!.Chat[^1].Actions);
    }

    [Fact]
    public void Ai_failure_falls_back_to_keyword_features()
    {
        var e = OpenTalk();
        var asked = new List<(int Ep, int Idx)>();
        e.ChatWanted += (ep, idx, _) => asked.Add((ep, idx));
        e.UserReply("chat", "mệt quá");
        e.ResolveChat(asked[0].Ep, asked[0].Idx, null);
        Assert.Contains("breathe", e.S.Ep!.Chat[^1].Actions!);
    }

    [Fact]
    public void Pressing_breathe_starts_breathing_and_dashboard_opens_the_dashboard()
    {
        var e = OpenTalk();
        e.UserReply("do", "breathe");
        Assert.Equal(Phase.Breathe, e.S.Ep!.Phase);

        var d = OpenTalk();
        d.UserReply("do", "dashboard");
        Assert.Equal(CaseId.Dashboard, d.S.Ep!.C);
    }

    [Fact]
    public void Pressing_plan_focus_brings_the_focus_plan_card_right_after()
    {
        // Tìm giờ trong ngày mẫu còn khoảng trống ≥ 60 phút và Milo đang rảnh
        MiloEngine? e = null;
        for (var t = Tm.T("09:00"); t < Tm.T("16:00") && e is null; t += 900)
        {
            var x = DemoScenario.CreateEngine();
            x.RunTo(t);
            if (x.HardGate() is not null || x.S.Ep is not null || x.FocusSlot() is null) continue;
            x.OpenTalk();
            for (var i = 0; i < 20 && x.S.Ep?.Phase != Phase.Show; i++) x.Advance(1, stopWhenBusy: false);
            if (x.S.Ep?.C == CaseId.Talk && Talk.Available(x).Contains("planFocus")) e = x;
        }
        Assert.NotNull(e);
        e.UserReply("do", "planFocus");
        var delivered = false;
        for (var i = 0; i < 120 && !delivered; i++)
        {
            e.Advance(1, stopWhenBusy: false);
            delivered = e.S.Ep?.C == CaseId.FocusPlan;
        }
        Assert.True(delivered);
    }

    [Fact]
    public void Unknown_or_unavailable_action_does_nothing_harmful()
    {
        var e = OpenTalk();
        e.UserReply("do", "formatDisk");
        Assert.Equal((CaseId.Talk, Phase.Show), (e.S.Ep!.C, e.S.Ep.Phase));
    }

    [Fact]
    public void Reminder_chat_without_ai_also_suggests_features()
    {
        var e = DemoScenario.CreateEngine();
        DemoTour.RunCase(e, CaseId.NoBreak);
        for (var i = 0; i < 20 && e.S.Ep?.Phase != Phase.Show; i++) e.Advance(1, stopWhenBusy: false);
        e.UserReply("chat", "deadline dí sát nút rồi");
        Assert.NotEmpty(e.S.Ep!.Chat[^1].Actions!);
    }

    [Fact]
    public void Llm_json_is_parsed_and_plain_text_still_works()
    {
        var r = new ChatRequest(CaseId.Talk, "Trò chuyện", "x", "mệt quá", Offer: ["breathe", "planFocus"]);
        var ok = ClaudeLineWriter.ParseChat("{\"reply\":\"Nghe mệt thật, mình thở chậm 1 phút nha.\",\"actions\":[\"breathe\",\"stuck\",\"hack\"]}", r)!;
        Assert.Equal("Nghe mệt thật, mình thở chậm 1 phút nha.", ok.Text);
        Assert.Equal(["breathe"], ok.Actions); // "stuck" không có trong Offer, "hack" không tồn tại
        var leaked = ClaudeLineWriter.ParseChat("{\"reply\":\"Mình thử breathe nhé, nút ở ngay bên dưới.\",\"actions\":[\"breathe\"]}", r)!;
        Assert.Equal("Mình thử Thở 1 phút nhé, nút ở ngay bên dưới.", leaked.Text);
        var plain = ClaudeLineWriter.ParseChat("Nghe mệt thật, mình thở chậm 1 phút nha.", r)!;
        Assert.Empty(plain.Actions);
        Assert.Contains("breathe: Thở 1 phút", ClaudeLineWriter.ChatUser(r));
    }
}
