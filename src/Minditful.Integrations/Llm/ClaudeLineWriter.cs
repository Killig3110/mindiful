using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using Minditful.Core.Engine;

namespace Minditful.Integrations.Llm;

/// <summary>
/// Lớp 2 (§14 · Lời thoại): Claude viết lại câu chính của thẻ và trả lời chat tự do.
/// LLM chỉ nhận tên case + số liệu (và câu người dùng tự gõ cho Milo) — không bao giờ nhận tiêu đề email, cuộc họp hay task.
/// Hết giờ (mặc định 2.5 giây), lỗi mạng, bị từ chối hay câu sai ràng buộc → trả null để engine dùng template.
/// </summary>
public sealed class ClaudeLineWriter(LlmOptions opt, Func<string?> apiKey)
{
    private const string Voice =
        "Bạn là Milo, chú cáo nhỏ sống ở góc màn hình của một kỹ sư phần mềm, nhắc họ chăm sóc bản thân trong ngày làm việc. " +
        "Viết tiếng Việt, giọng ấm áp, gần gũi, không dạy đời, không phán xét. Milo xưng \"Milo\" hoặc \"mình\", gọi người dùng là \"bạn\". " +
        "Không dùng emoji, markdown hay dấu ngoặc kép.";

    private const string LineRules =
        " Nhiệm vụ: viết đúng 1 câu nhắc (tối đa 25 từ) có đúng 1 hành động cụ thể khớp với nút chính. " +
        "Chỉ dùng số liệu được cho, không bịa thêm con số. Chỉ trả về câu đó, không giải thích.";

    private const string ChatRules =
        " Nhiệm vụ: trả lời câu người dùng vừa gõ trong 1–2 câu ngắn (tối đa 30 từ), liên quan tới lời nhắc đang hiện. " +
        "Nếu họ có vẻ mệt, bận hay không muốn, hãy tôn trọng và gợi ý họ chọn một nút trên thẻ. Chỉ trả về câu trả lời.";

    private AnthropicClient? _client;
    private string? _clientKey;
    private string? _lastLine;

    public bool Available => opt.Enabled && !string.IsNullOrWhiteSpace(apiKey());

    /// <summary>Lý do lần gọi gần nhất không dùng được (hiện trong bảng điều khiển).</summary>
    public string? LastError { get; private set; }

    private AnthropicClient? Client()
    {
        var key = apiKey();
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (_client is null || _clientKey != key)
        {
            // Không retry: quá 2.5 giây là dùng template (§14), retry chỉ làm trễ thêm.
            _client = new AnthropicClient { ApiKey = key, MaxRetries = 0, Timeout = TimeSpan.FromMilliseconds(opt.TimeoutMs) };
            _clientKey = key;
        }
        return _client;
    }

    public async Task<string?> WriteLineAsync(LineRequest r, CancellationToken ct = default)
    {
        var user =
            $"Lời nhắc: {r.CaseName}\nSố liệu: {r.Facts}\nNút chính: {r.Action}\n" +
            $"Câu mẫu để tham khảo giọng (đừng chép lại): {r.Template}" +
            (_lastLine is null ? "" : $"\nĐừng lặp lại câu vừa dùng: {_lastLine}");
        var line = Lines.Clean(await AskAsync(Voice + LineRules, user, ct));
        if (line is not null) _lastLine = line;
        return line;
    }

    public async Task<string?> ReplyChatAsync(ChatRequest r, CancellationToken ct = default)
    {
        var user = $"Lời nhắc đang hiện: {r.CaseName} ({r.Facts})\nNgười dùng gõ: {r.UserText}";
        return Lines.Clean(await AskAsync(Voice + ChatRules, user, ct));
    }

    private async Task<string?> AskAsync(string system, string user, CancellationToken ct)
    {
        var client = Client();
        if (client is null || !opt.Enabled) return null;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(opt.TimeoutMs);
        try
        {
            var p = new MessageCreateParams
            {
                Model = opt.Model,
                MaxTokens = 1024,
                System = system,
                Messages = [new() { Role = Role.User, Content = user }],
                OutputConfig = new BetaOutputConfig { Effort = ParseEffort(opt.Effort) },
            };
            if (opt.RefusalFallback)
                p = p with { Betas = ["server-side-fallback-2026-07-01"], Fallbacks = new Default() };
            var res = await client.Beta.Messages.Create(p, cts.Token);
            if (res.StopReason == BetaStopReason.Refusal)
            {
                LastError = "Claude từ chối viết câu này → dùng template";
                return null;
            }
            LastError = null;
            return string.Concat(res.Content.Select(b => b.Value).OfType<BetaTextBlock>().Select(t => t.Text));
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            LastError = $"Quá {opt.TimeoutMs} ms → dùng template";
        }
        catch (AnthropicRateLimitException)
        {
            LastError = "Claude đang giới hạn tốc độ (429) → dùng template";
        }
        catch (AnthropicApiException ex)
        {
            LastError = $"Claude API lỗi: {ex.Message}";
        }
        catch (HttpRequestException ex)
        {
            LastError = "Mất mạng: " + ex.Message;
        }
        return null;
    }

    private static Effort ParseEffort(string s) => s.ToLowerInvariant() switch
    {
        "medium" => Effort.Medium,
        "high" => Effort.High,
        _ => Effort.Low,
    };
}
