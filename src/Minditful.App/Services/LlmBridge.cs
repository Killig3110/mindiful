using System.Windows.Threading;
using Minditful.Core.Engine;
using Minditful.Integrations.Llm;

namespace Minditful.App.Services;

/// <summary>Nối engine (đồng bộ) với Claude (bất đồng bộ): viết sẵn câu khi case vào hàng đợi, trả lời chat tự do.</summary>
internal static class LlmBridge
{
    public static void Attach(MiloEngine engine, ClaudeLineWriter writer, Dispatcher ui)
    {
        engine.LineWanted += (item, req) =>
        {
            if (!writer.Available) return;
            _ = WriteAsync();

            async Task WriteAsync()
            {
                var line = await writer.WriteLineAsync(req);
                _ = ui.BeginInvoke(() =>
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

        engine.ChatWanted += (epId, index, req) =>
        {
            _ = ReplyAsync();

            async Task ReplyAsync()
            {
                var reply = writer.Available ? await writer.ReplyChatAsync(req) : null;
                _ = ui.BeginInvoke(() =>
                {
                    engine.ResolveChat(epId, index, reply);
                    if (reply is null && writer.LastError is { } err) engine.LogExternal("Chat: " + err, LogKind.Error);
                });
            }
        };
    }
}
