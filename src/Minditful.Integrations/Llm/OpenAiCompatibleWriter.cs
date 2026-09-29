using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Minditful.Core.Engine;

namespace Minditful.Integrations.Llm;

/// <summary>
/// Lớp 2 qua API chuẩn OpenAI Chat Completions — dùng được với Ollama (chạy trên máy, miễn phí, không giới hạn),
/// Groq, Google Gemini (endpoint tương thích OpenAI), OpenRouter… Cùng lời nhắc và cùng cách đọc kết quả như bản Claude.
/// Nhiều key (ngăn bởi dấu phẩy): key nào báo hết lượt (429) hoặc sai (401/403) thì tạm nghỉ và chuyển ngay sang key kế tiếp.
/// </summary>
public sealed class OpenAiCompatibleWriter(LlmOptions opt, Func<string?> apiKey) : IMiloLlm
{
    private static readonly HttpClient Http = new() { Timeout = Timeout.InfiniteTimeSpan };
    private string? _lastLine;
    private readonly object _gate = new();
    private readonly Dictionary<string, DateTime> _restUntil = [];
    private int _next;

    public string Name => $"{Host()} · {opt.Model}";

    /// <summary>Máy chủ chạy trên máy (Ollama, LM Studio) không cần API key.</summary>
    public bool Available => opt.Enabled && !string.IsNullOrWhiteSpace(opt.BaseUrl) && (IsLocal || Keys().Length > 0);

    /// <summary>Key đang dùng được / tổng số key (key hết lượt được nghỉ tới khi dịch vụ cho phép gọi lại).</summary>
    public (int Ready, int Total) KeyStatus
    {
        get
        {
            var keys = Keys();
            lock (_gate) return (keys.Count(k => !Resting(k)), keys.Length);
        }
    }

    /// <summary>LLM_API_KEY có thể chứa nhiều key: gsk_a,gsk_b,gsk_c (dấu phẩy, chấm phẩy hoặc khoảng trắng).</summary>
    private string[] Keys() =>
        (apiKey() ?? "").Split([',', ';', ' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();

    private bool Resting(string key) => _restUntil.TryGetValue(key, out var t) && t > DateTime.UtcNow;

    /// <summary>Lần lượt từng key còn dùng được, bắt đầu từ key kế tiếp (chia đều lượt cho các key).</summary>
    private List<string?> KeyOrder()
    {
        var keys = Keys();
        if (keys.Length == 0) return [null];
        lock (_gate)
        {
            var start = _next++ % keys.Length;
            return Enumerable.Range(0, keys.Length).Select(i => keys[(start + i) % keys.Length]).Where(k => !Resting(k)).Cast<string?>().ToList();
        }
    }

    private void Rest(string? key, TimeSpan span)
    {
        if (key is null) return;
        lock (_gate) _restUntil[key] = DateTime.UtcNow + span;
    }

    private static TimeSpan RetryAfter(HttpResponseMessage res) =>
        res.Headers.RetryAfter?.Delta is { } d ? d
        : res.Headers.RetryAfter?.Date is { } at ? at - DateTimeOffset.UtcNow
        : TimeSpan.FromSeconds(60);

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
        var payload = JsonSerializer.Serialize(body);
        var order = KeyOrder();
        if (order.Count == 0)
        {
            LastError = $"Cả {Keys().Length} key {Host()} đang hết lượt → dùng {(json ? "luật" : "câu mẫu")}, tự thử lại khi dịch vụ cho phép";
            return null;
        }
        try
        {
            foreach (var key in order)
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, opt.BaseUrl.TrimEnd('/') + "/chat/completions")
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                };
                if (key is not null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                using var res = await Http.SendAsync(req, cts.Token);
                var text = await res.Content.ReadAsStringAsync(cts.Token);
                if (res.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    Rest(key, RetryAfter(res));
                    LastError = $"{Host()} báo hết lượt (429) → dùng luật/câu mẫu. Gói miễn phí giới hạn số request mỗi phút/ngày";
                    continue; // thử key kế tiếp
                }
                if (res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden && key is not null)
                {
                    Rest(key, TimeSpan.FromHours(1));
                    LastError = $"{Host()} từ chối 1 key ({(int)res.StatusCode}) — key sai hoặc đã bị thu hồi";
                    continue;
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
            if (order.Count > 1) LastError = $"Cả {order.Count} key {Host()} đều hết lượt hoặc bị từ chối → dùng {(json ? "luật" : "câu mẫu")}";
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
