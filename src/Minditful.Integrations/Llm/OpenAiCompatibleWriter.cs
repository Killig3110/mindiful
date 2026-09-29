using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Minditful.Core.Engine;

namespace Minditful.Integrations.Llm;

/// <summary>
/// Lớp 2 qua API chuẩn OpenAI Chat Completions — dùng được với Ollama (chạy trên máy, miễn phí, không giới hạn),
/// Groq, Google Gemini (endpoint tương thích OpenAI), OpenRouter… Cùng lời nhắc và cùng cách đọc kết quả như bản Claude.
/// </summary>
public sealed class OpenAiCompatibleWriter(LlmOptions opt, Func<string?> apiKey) : IMiloLlm
{
    private static readonly HttpClient Http = new() { Timeout = Timeout.InfiniteTimeSpan };
    private string? _lastLine;

    public string Name => $"{Host()} · {opt.Model}";

    /// <summary>Máy chủ chạy trên máy (Ollama, LM Studio) không cần API key.</summary>
    public bool Available => opt.Enabled && !string.IsNullOrWhiteSpace(opt.BaseUrl) && (IsLocal || !string.IsNullOrWhiteSpace(apiKey()));

    public string? LastError { get; private set; }

    private bool IsLocal => Uri.TryCreate(opt.BaseUrl, UriKind.Absolute, out var u) && u.IsLoopback;

    private string Host()
    {
        if (!Uri.TryCreate(opt.BaseUrl, UriKind.Absolute, out var u)) return "OpenAI-compatible";
        if (u.IsLoopback) return u.Port == 11434 ? "Ollama" : "Máy này";
        return u.Host switch
        {
            var h when h.Contains("groq", StringComparison.OrdinalIgnoreCase) => "Groq",
            var h when h.Contains("googleapis", StringComparison.OrdinalIgnoreCase) => "Gemini",
            var h when h.Contains("openrouter", StringComparison.OrdinalIgnoreCase) => "OpenRouter",
            var h => h,
        };
    }

    // Không có structured output bắt buộc như Claude → mô tả đúng các trường trong lời nhắc và yêu cầu JSON object
    private const string MoodJson =
        " Chỉ trả về đúng 1 object JSON, không giải thích, với các trường: " +
        "score (số nguyên 0–100), adjust (số nguyên -10 tới 10), focus (0–5), energy (0–5), stress (0–5), " +
        "label (một trong \"Mọng\", \"Cân bằng\", \"Mệt dần\", \"Kiệt sức\"), insight (chuỗi).";

    private const string MeetingJson =
        " Chỉ trả về đúng 1 object JSON, không giải thích, với các trường: load (số nguyên 1–5), " +
        "kind (một trong \"Trình bày\", \"1:1\", \"Họp đông\", \"Trao đổi\", \"Ra quyết định\", \"Cập nhật\"), recovery_min (số nguyên 0–15), note (chuỗi).";

    public async Task<MoodInsight?> AssessMoodAsync(MoodRequest r, CancellationToken ct = default)
    {
        var json = await AskAsync(ClaudeLineWriter.Voice + ClaudeLineWriter.MoodRules + MoodJson, ClaudeLineWriter.MoodUser(r), opt.InsightTimeoutMs, true, ct);
        if (json is null) return null;
        var m = ClaudeLineWriter.ParseMood(json, Host());
        if (m is null) LastError = $"{Host()} trả về JSON mood không đúng dạng → dùng luật";
        return m;
    }

    public async Task<MeetingAssessment?> AssessMeetingAsync(MeetingRequest r, CancellationToken ct = default)
    {
        var json = await AskAsync(ClaudeLineWriter.Voice + ClaudeLineWriter.MeetingRulesPrompt + MeetingJson, ClaudeLineWriter.MeetingUser(r), opt.InsightTimeoutMs, true, ct);
        if (json is null) return null;
        var a = ClaudeLineWriter.ParseMeeting(json, r.EventId, Host());
        if (a is null) LastError = $"{Host()} trả về JSON cuộc họp không đúng dạng → dùng luật";
        return a;
    }

    public async Task<string?> WriteLineAsync(LineRequest r, CancellationToken ct = default)
    {
        var line = Lines.Clean(await AskAsync(ClaudeLineWriter.Voice + ClaudeLineWriter.LineRules, ClaudeLineWriter.LineUser(r, _lastLine), opt.TimeoutMs, false, ct));
        if (line is not null) _lastLine = line;
        return line;
    }

    public async Task<string?> ReplyChatAsync(ChatRequest r, CancellationToken ct = default) =>
        Lines.Clean(await AskAsync(ClaudeLineWriter.Voice + ClaudeLineWriter.ChatRules, ClaudeLineWriter.ChatUser(r), opt.TimeoutMs, false, ct));

    private async Task<string?> AskAsync(string system, string user, int timeoutMs, bool json, CancellationToken ct)
    {
        if (!Available) return null;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeoutMs);
        var body = new Dictionary<string, object>
        {
            ["model"] = opt.Model,
            ["messages"] = new object[] { new { role = "system", content = system }, new { role = "user", content = user } },
            ["temperature"] = json ? 0.2 : 0.7,
            ["max_tokens"] = 600,
        };
        if (json) body["response_format"] = new { type = "json_object" };
        using var req = new HttpRequestMessage(HttpMethod.Post, opt.BaseUrl.TrimEnd('/') + "/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
        if (apiKey() is { Length: > 0 } key) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        try
        {
            using var res = await Http.SendAsync(req, cts.Token);
            var text = await res.Content.ReadAsStringAsync(cts.Token);
            if (res.StatusCode == HttpStatusCode.TooManyRequests)
            {
                LastError = $"{Host()} báo hết lượt (429) → dùng luật/câu mẫu. Gói miễn phí giới hạn số request mỗi phút/ngày";
                return null;
            }
            if (!res.IsSuccessStatusCode)
            {
                LastError = $"{Host()} lỗi {(int)res.StatusCode}: {text[..Math.Min(160, text.Length)]}";
                return null;
            }
            var content = JsonDocument.Parse(text).RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            LastError = null;
            return content;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            LastError = $"Quá {timeoutMs} ms → dùng {(json ? "luật" : "câu mẫu")}";
        }
        catch (HttpRequestException ex)
        {
            LastError = IsLocal ? $"Không kết nối được {opt.BaseUrl} (Ollama đã chạy chưa?): {ex.Message}" : "Mất mạng: " + ex.Message;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            LastError = $"{Host()} trả về dữ liệu không đúng chuẩn OpenAI";
        }
        return null;
    }
}
