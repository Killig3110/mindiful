using System.Diagnostics;
using System.Windows.Threading;
using Minditful.Core.Engine;
using Minditful.Integrations;
using Minditful.Integrations.Llm;

namespace Minditful.App.Services;

/// <summary>
/// Nối engine (đồng bộ) với LLM (bất đồng bộ): câu thoại, chat, đánh giá mood, đánh giá cuộc họp.
/// Mỗi tính năng bật/tắt riêng trong Llm.Features; không có API key thì engine chạy bằng luật.
/// Đếm số request đã gửi (gói miễn phí giới hạn theo ngày) và giãn nhịp chấm mood khi Demo tua nhanh.
/// </summary>
internal static class LlmBridge
{
    private static int _requests;
    private static readonly Stopwatch LastMood = Stopwatch.StartNew();
    private static bool _moodAskedOnce;

    /// <summary>Số request đã gửi tới LLM từ lúc mở app (câu thoại, chat, mood, cuộc họp, kiểm chứng).</summary>
    public static int Requests => _requests;

    public static void Count() => Interlocked.Increment(ref _requests);

    /// <param name="moodMinSeconds">Khoảng cách tối thiểu (giây thật) giữa 2 lần chấm mood. Demo dùng để không đốt hết lượt miễn phí.</param>
    public static void Attach(MiloEngine engine, IMiloLlm llm, Dispatcher ui, LlmFeatures features, int moodMinSeconds = 0)
    {
        void OnUi(Action a) => _ = ui.BeginInvoke(a);

        if (features.Lines)
            engine.LineWanted += (item, req) =>
            {
                if (!llm.Available) return;
                Count();
                _ = Run();

                async Task Run()
                {
                    var line = await llm.WriteLineAsync(req);
                    OnUi(() =>
                    {
                        if (line is not null)
                        {
                            engine.SetLine(item, line);
                            engine.LogExternal($"{llm.Name} viết sẵn câu cho {req.CaseName}: {line}", LogKind.Action);
                        }
                        else if (llm.LastError is { } err) engine.LogExternal($"{req.CaseName}: {err}", LogKind.Error);
                    });
                }
            };

        if (features.Chat)
            engine.ChatWanted += (epId, index, req) =>
            {
                if (llm.Available) Count();
                _ = Run();

                async Task Run()
                {
                    var reply = llm.Available ? await llm.ReplyChatAsync(req) : null;
                    OnUi(() =>
                    {
                        engine.ResolveChat(epId, index, reply?.Text, reply?.Actions);
                        if (reply is null && llm.LastError is { } err) engine.LogExternal("Chat: " + err, LogKind.Error);
                        else if (reply is { Actions.Count: > 0 })
                            engine.LogExternal($"{llm.Name} đề nghị tính năng: {string.Join(", ", reply.Actions.Select(a => Talk.Actions[a].Label))}", LogKind.Action);
                    });
                }
            };

        engine.MoodWanted += req =>
        {
            if (!llm.Available) return;
            if (_moodAskedOnce && LastMood.Elapsed.TotalSeconds < moodMinSeconds) return; // Demo tua nhanh: giữ nhịp theo giờ thật
            _moodAskedOnce = true;
            LastMood.Restart();
            Count();
            _ = Run();

            async Task Run()
            {
                var insight = await llm.AssessMoodAsync(req);
                OnUi(() =>
                {
                    if (insight is not null) engine.SetMoodInsight(insight);
                    else engine.LogExternal("Mood: " + (llm.LastError ?? "AI không trả lời") + " → giữ điểm theo luật", LogKind.Error);
                });
            }
        };

        engine.MeetingWanted += req =>
        {
            if (!llm.Available) return;
            Count();
            _ = Run();

            async Task Run()
            {
                var a = await llm.AssessMeetingAsync(req);
                OnUi(() =>
                {
                    if (a is not null) engine.SetMeetingAssessment(a);
                    else if (llm.LastError is { } err) engine.LogExternal("Đánh giá cuộc họp: " + err, LogKind.Error);
                });
            }
        };
    }

    /// <summary>Đặt chế độ mood/cuộc họp theo cấu hình; không có key thì về luật.</summary>
    public static string ApplyModes(MiloEngine engine, IMiloLlm llm, LlmOptions opt)
    {
        var on = opt.Enabled && llm.Available;
        var f = opt.Features;
        engine.Cfg.MoodIntervalMinutes = Math.Max(5, f.MoodIntervalMinutes);
        engine.Cfg.IncludeChatInMood = f.IncludeChatInMood;
        return SetModes(engine, llm, opt, on ? f.MoodMode : MoodMode.Rules, on ? f.MeetingMode : MeetingMode.Rules);
    }

    /// <summary>Công tắc trên bảng điều khiển: đổi cách chấm mood / đánh giá cuộc họp ngay lúc đang chạy.</summary>
    public static string SetModes(MiloEngine engine, IMiloLlm llm, LlmOptions opt, MoodMode mood, MeetingMode meeting)
    {
        var on = opt.Enabled && llm.Available;
        var changedMeeting = engine.Cfg.MeetingMode != (on ? meeting : MeetingMode.Rules);
        engine.Cfg.MoodMode = on ? mood : MoodMode.Rules;
        engine.Cfg.MeetingMode = on ? meeting : MeetingMode.Rules;
        if (engine.Cfg.MoodMode == MoodMode.Rules) engine.S.MoodInsight = null;
        engine.ComputeMood();
        if (changedMeeting && engine.Cfg.MeetingMode == MeetingMode.Llm) engine.ReassessMeetings();
        if (engine.Cfg.MoodMode != MoodMode.Rules) engine.AskMoodNow();
        var text = Describe(engine, llm, opt, on);
        engine.LogExternal("Lớp 2: " + text, LogKind.Sig);
        return text;
    }

    public static string Describe(MiloEngine engine, IMiloLlm llm, LlmOptions opt, bool on)
    {
        var ai = llm.Name;
        string Mode(bool enabled) => enabled && on ? ai : "câu mẫu";
        var mood = engine.Cfg.MoodMode switch { MoodMode.Hybrid => $"luật + {ai} (±10)", MoodMode.Llm => ai, _ => "luật" };
        var meet = engine.Cfg.MeetingMode == MeetingMode.Llm ? ai : "luật";
        var reason = !opt.Enabled ? " (Llm.Enabled = false)"
            : !on && (opt.Features.MoodMode != MoodMode.Rules || opt.Features.MeetingMode != MeetingMode.Rules || opt.Features.Lines) ? " (chưa có API key → dùng luật/câu mẫu)" : "";
        return $"câu thoại: {Mode(opt.Features.Lines)} · chat: {Mode(opt.Features.Chat)} · mood: {mood} · đánh giá cuộc họp: {meet}{reason}";
    }
}
