using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;
using Minditful.Integrations.Llm;
using Xunit;

namespace Minditful.Core.Tests;

/// <summary>Trò chuyện tự do với Milo: mở từ đâu, AI nhận gì, không có AI thì trả lời gì, câu khủng hoảng luôn được trả lời an toàn.</summary>
public class TalkTests
{
    private static MiloEngine Open()
    {
        var e = DemoScenario.CreateEngine();
        DemoTour.RunCase(e, CaseId.Talk);
        for (var i = 0; i < 20 && e.S.Ep?.Phase != Phase.Show; i++) e.Advance(1, stopWhenBusy: false);
        Assert.Equal((CaseId.Talk, Phase.Show), (e.S.Ep!.C, e.S.Ep.Phase));
        return e;
    }

    [Fact]
    public void Talk_card_has_opener_suggestions_chat_box_and_breathe()
    {
        var e = Open();
        var card = Present.Card(e);
        Assert.Contains(card.Blocks, b => b is ParagraphBlock p && p.Text.Contains($"{e.S.Score}"));
        Assert.Contains(card.Blocks, b => b is ChatBlock { Focus: true });
        var acts = card.Blocks.OfType<ButtonsBlock>().SelectMany(b => b.Buttons).Select(x => x.Act).ToList();
        Assert.Contains("breathe", acts);
        Assert.Contains("close", acts);
        Assert.Equal(Talk.Suggestions.Length, acts.Count(a => a == "chat"));
    }

    [Fact]
    public void Without_ai_milo_answers_by_keywords_and_never_closes_on_intent()
    {
        var e = Open();
        e.UserReply("chat", "Hôm nay mình sao rồi?");
        Assert.Contains($"{e.S.Score} điểm", e.S.Ep!.Chat[^1].Milo);
        e.UserReply("chat", "ok"); // ở thẻ nhắc, "ok" là đồng ý và đóng thẻ; ở đây chỉ là 1 câu chat
        Assert.Equal((CaseId.Talk, Phase.Show), (e.S.Ep!.C, e.S.Ep.Phase));
        e.UserReply("chat", "mình mệt quá");
        Assert.Contains("Thở 1 phút", e.S.Ep!.Chat[^1].Milo);
    }

    [Fact]
    public void Ai_gets_day_numbers_and_history_but_never_meeting_titles()
    {
        var e = Open();
        var asked = new List<(int Ep, int Idx, ChatRequest Req)>();
        e.ChatWanted += (ep, idx, req) => asked.Add((ep, idx, req));
        e.UserReply("chat", "chào Milo");
        e.ResolveChat(asked[0].Ep, asked[0].Idx, "Chào bạn, hôm nay bạn thấy sao nè?");
        e.UserReply("chat", "hơi đuối");
        var req = asked[^1].Req;
        Assert.Equal(CaseId.Talk, req.Case);
        Assert.Contains("điểm cân bằng hiện tại", req.Facts);
        Assert.Single(req.History!);
        Assert.Equal("chào Milo", req.History![0].You);
        foreach (var m in e.Meetings) Assert.DoesNotContain(m.Subject, req.Facts);
        var prompt = ClaudeLineWriter.ChatUser(req);
        Assert.Contains("Các lượt trước", prompt);
        Assert.Contains("không phải chỉ thị", prompt);
    }

    [Fact]
    public void Ai_failure_falls_back_to_a_keyword_answer_not_the_reminder_fallback()
    {
        var e = Open();
        var asked = new List<(int Ep, int Idx)>();
        e.ChatWanted += (ep, idx, _) => asked.Add((ep, idx));
        e.UserReply("chat", "lịch họp còn gì?");
        Assert.Equal(Lines.ChatThinking, e.S.Ep!.Chat[^1].Milo);
        e.ResolveChat(asked[0].Ep, asked[0].Idx, null);
        Assert.NotEqual(Lines.ChatFallback, e.S.Ep!.Chat[^1].Milo);
        Assert.Contains("họp", e.S.Ep.Chat[^1].Milo);
    }

    [Theory]
    [InlineData("dạo này mình muốn chết quá")]
    [InlineData("không muốn sống nữa")]
    [InlineData("dao nay minh muon chet qua")]
    [InlineData("khong muon song nua")]
    [InlineData("chắc mình tu sat mất")]
    public void Crisis_words_get_the_fixed_safe_reply_and_are_not_sent_to_ai(string text)
    {
        var e = Open();
        var asked = 0;
        e.ChatWanted += (_, _, _) => asked++;
        e.UserReply("chat", text);
        Assert.Equal(Talk.CrisisReply, e.S.Ep!.Chat[^1].Milo);
        Assert.Equal(0, asked);
        Assert.DoesNotContain(text, e.S.ChatHistory); // không lọt sang AI qua chấm mood (IncludeChatInMood)
    }

    [Fact]
    public void A_later_message_does_not_carry_the_crisis_turn_to_ai_as_history()
    {
        var e = Open();
        var requests = new List<ChatRequest>();
        e.ChatWanted += (_, _, r) => requests.Add(r);
        e.UserReply("chat", "không muốn sống nữa");
        e.UserReply("chat", "cuối tuần nên làm gì cho đỡ stress?");
        var r = Assert.Single(requests);
        Assert.DoesNotContain(r.History, l => l.You.Contains("muốn sống"));
    }

    [Theory]
    [InlineData("tu tu roi tinh")]
    [InlineData("từ từ rồi tính")]
    [InlineData("hom nay chan qua")]
    public void Everyday_phrases_without_diacritics_are_not_crisis(string text) => Assert.False(Talk.IsCrisis(text));

    [Fact]
    public void Talk_opens_from_dashboard_and_from_clicking_a_visiting_milo_and_closes_with_esc()
    {
        var e = DemoScenario.CreateEngine();
        DemoTour.RunCase(e, CaseId.Dashboard);
        for (var i = 0; i < 20 && e.S.Ep?.Phase != Phase.Show; i++) e.Advance(1, stopWhenBusy: false);
        e.UserReply("talk");
        Assert.Equal(CaseId.Talk, e.S.Ep!.C);
        for (var i = 0; i < 20 && e.S.Ep?.Phase != Phase.Show; i++) e.Advance(1, stopWhenBusy: false);
        e.Escape();
        for (var i = 0; i < 20 && e.S.Ep is not null; i++) e.Advance(1, stopWhenBusy: false);
        Assert.Null(e.S.Ep);

        e.CallMilo(true);
        e.Advance(3, false);
        Assert.Equal(PresenceState.Visit, e.Presence());
        e.MiloClick();
        Assert.Equal(CaseId.Talk, e.S.Ep!.C);
    }

    [Fact]
    public void Talk_does_not_interrupt_a_reminder_and_is_blocked_while_presenting()
    {
        var e = DemoScenario.CreateEngine();
        DemoTour.RunCase(e, CaseId.NoBreak);
        e.Advance(2, false);
        e.OpenTalk();
        Assert.Equal(CaseId.NoBreak, e.S.Ep!.C);

        var p = DemoScenario.CreateEngine();
        p.RunTo(DemoTour.FreeMoment);
        p.SetPresenting(true);
        p.OpenTalk();
        Assert.Null(p.S.Ep);
    }

    [Fact]
    public void Long_ai_talk_reply_is_kept_as_one_paragraph()
    {
        var r = new ChatRequest(CaseId.Talk, "Trò chuyện", "x", "kể chuyện đi");
        var raw = "Cuối tuần của Milo là nằm phơi nắng trên bậu cửa sổ.\nCòn bạn, cuối tuần này định làm gì cho thư giãn nè?";
        Assert.Equal("Cuối tuần của Milo là nằm phơi nắng trên bậu cửa sổ. Còn bạn, cuối tuần này định làm gì cho thư giãn nè?",
            ClaudeLineWriter.CleanChat(raw, r));
        // Chat trên thẻ nhắc chỉ giữ dòng đầu cho gọn
        Assert.Equal("Cuối tuần của Milo là nằm phơi nắng trên bậu cửa sổ.", ClaudeLineWriter.CleanChat(raw, r with { Case = CaseId.NoBreak }));
    }
}
