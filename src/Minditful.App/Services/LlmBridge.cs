using System.Windows.Threading;
using Minditful.Core.Engine;
using Minditful.Integrations;
using Minditful.Integrations.Llm;

namespace Minditful.App.Services;

/// <summary>
/// Nối engine (đồng bộ) với Claude (bất đồng bộ): câu thoại, chat, đánh giá mood, đánh giá cuộc họp.
/// Mỗi tính năng bật/tắt riêng trong Llm.Features; không có API key thì engine chạy bằng luật.
/// </summary>
internal static class LlmBridge
{
    public static void Attach(MiloEngine engine, ClaudeLineWriter writer, Dispatcher ui, LlmFeatures features)
    {
        void OnUi(Action a) => _ = ui.BeginInvoke(a);

        if (features.Lines)
            engine.LineWanted += (item, req) =>
            {
                if (!writer.Available) return;
                _ = Run();

                async Task Run()
                {
                    var line = await writer.WriteLineAsync(req);
                    OnUi(() =>
                    {
                        if (line is not null)
                        {
                            engine.SetLine(item, line);
                            engine.LogExternal($"Claude viết sẵn câu cho {req.CaseName}: {line}", LogKind.Action);
                        }
                        else if (writer.LastError is { } err) engine.LogExternal($"{req.CaseName}: {err}", LogKind.Error);
                    });
                }
            };

        if (features.Chat)
            engine.ChatWanted += (epId, index, req) =>
            {
                _ = Run();

                async Task Run()
                {
                    var reply = writer.Available ? await writer.ReplyChatAsync(req) : null;
                    OnUi(() =>
                    {
                        engine.ResolveChat(epId, index, reply);
                        if (reply is null && writer.LastError is { } err) engine.LogExternal("Chat: " + err, LogKind.Error);
                    });
                }
            };

        engine.MoodWanted += req =>
        {
            if (!writer.Available) return;
            _ = Run();

            async Task Run()
            {
                var insight = await writer.AssessMoodAsync(req);
                OnUi(() =>
                {
                    if (insight is not null) engine.SetMoodInsight(insight);
                    else engine.LogExternal("Mood: " + (writer.LastError ?? "Claude không trả lời") + " → giữ điểm theo luật", LogKind.Error);
                });
            }
        };

        engine.MeetingWanted += req =>
        {
            if (!writer.Available) return;
            _ = Run();

            async Task Run()
            {
                var a = await writer.AssessMeetingAsync(req);
                OnUi(() =>
                {
                    if (a is not null) engine.SetMeetingAssessment(a);
                    else if (writer.LastError is { } err) engine.LogExternal("Đánh giá cuộc họp: " + err, LogKind.Error);
                });
            }
        };
    }

    /// <summary>Đặt chế độ mood/cuộc họp theo cấu hình; không có key thì về luật.</summary>
    public static string ApplyModes(MiloEngine engine, ClaudeLineWriter writer, LlmOptions llm)
    {
        var on = llm.Enabled && writer.Available;
        var f = llm.Features;
        engine.Cfg.MoodMode = on ? f.MoodMode : MoodMode.Rules;
        engine.Cfg.MeetingMode = on ? f.MeetingMode : MeetingMode.Rules;
        engine.Cfg.MoodIntervalMinutes = Math.Max(5, f.MoodIntervalMinutes);
        engine.Cfg.IncludeChatInMood = f.IncludeChatInMood;
        if (engine.Cfg.MeetingMode == MeetingMode.Llm) engine.ReassessMeetings();
        var text = Describe(engine, llm, on);
        engine.LogExternal("Lớp 2: " + text, LogKind.Sig);
        return text;
    }

    public static string Describe(MiloEngine engine, LlmOptions llm, bool on)
    {
        string Mode(bool enabled) => enabled && on ? "Claude" : "câu mẫu";
        var mood = engine.Cfg.MoodMode switch { MoodMode.Hybrid => "luật + Claude (±10)", MoodMode.Llm => "Claude", _ => "luật" };
        var meet = engine.Cfg.MeetingMode == MeetingMode.Llm ? "Claude" : "luật";
        var reason = !llm.Enabled ? " (Llm.Enabled = false)"
            : !on && (llm.Features.MoodMode != MoodMode.Rules || llm.Features.MeetingMode != MeetingMode.Rules || llm.Features.Lines) ? " (chưa có API key → dùng luật/câu mẫu)" : "";
        return $"câu thoại: {Mode(llm.Features.Lines)} · chat: {Mode(llm.Features.Chat)} · mood: {mood} · đánh giá cuộc họp: {meet}{reason}";
    }
}
