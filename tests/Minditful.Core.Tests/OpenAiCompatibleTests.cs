using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Minditful.Core.Engine;
using Minditful.Integrations;
using Minditful.Integrations.Llm;
using Xunit;

namespace Minditful.Core.Tests;

/// <summary>
/// AI tương thích OpenAI (Ollama, Groq, Gemini…) thử với 1 máy chủ giả trên máy: gửi đúng định dạng Chat Completions,
/// đọc được JSON kể cả khi model bọc ```json hoặc trả số dạng chuỗi, báo lỗi 429 rõ ràng.
/// </summary>
public sealed class OpenAiCompatibleTests : IDisposable
{
    private readonly HttpListener _server = new();
    private readonly string _base;
    private string _reply = "";
    private HttpStatusCode _status = HttpStatusCode.OK;
    public string? LastBody;
    public string? LastAuth;

    public OpenAiCompatibleTests()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        _base = $"http://localhost:{port}/v1";
        _server.Prefixes.Add($"http://localhost:{port}/v1/");
        _server.Start();
        _ = Serve();
    }

    private async Task Serve()
    {
        while (_server.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx = await _server.GetContextAsync(); } catch { return; }
            using (var r = new StreamReader(ctx.Request.InputStream)) LastBody = await r.ReadToEndAsync();
            LastAuth = ctx.Request.Headers["Authorization"];
            ctx.Response.StatusCode = (int)_status;
            var body = _status == HttpStatusCode.OK
                ? JsonSerializer.Serialize(new { choices = new[] { new { message = new { role = "assistant", content = _reply } } } })
                : "{\"error\":\"rate limit\"}";
            var bytes = Encoding.UTF8.GetBytes(body);
            await ctx.Response.OutputStream.WriteAsync(bytes);
            ctx.Response.Close();
        }
    }

    public void Dispose() => _server.Close();

    private IMiloLlm Writer(string? key = null) =>
        MiloLlm.Create(new LlmOptions { Provider = "OpenAI", BaseUrl = _base, Model = "qwen2.5:7b", InsightTimeoutMs = 5000, TimeoutMs = 5000 }, () => key);

    [Fact]
    public async Task Local_ollama_needs_no_key_and_mood_json_is_read()
    {
        var w = Writer();
        Assert.True(w.Available);
        Assert.StartsWith("Ollama", w.Name.Replace("Máy này", "Ollama")); // cổng ngẫu nhiên nên không phải 11434
        _reply = "```json\n{\"score\":\"72\",\"adjust\":-3,\"focus\":3,\"energy\":3.0,\"stress\":2,\"label\":\"Cân bằng\",\"insight\":\"Bạn họp liền khá lâu, nghỉ 5 phút nha.\"}\n```";
        var m = await w.AssessMoodAsync(new MoodRequest(80, "đã làm 8h00 hôm nay", []));
        Assert.NotNull(m);
        Assert.Equal((72, -3, "Cân bằng"), (m!.Score!.Value, m.Adjust, m.Label));
        using var doc = JsonDocument.Parse(LastBody!);
        Assert.Equal("qwen2.5:7b", doc.RootElement.GetProperty("model").GetString());
        Assert.Equal("json_object", doc.RootElement.GetProperty("response_format").GetProperty("type").GetString());
        Assert.Equal("system", doc.RootElement.GetProperty("messages")[0].GetProperty("role").GetString());
        Assert.Null(LastAuth);
    }

    [Fact]
    public async Task Api_key_is_sent_as_bearer_and_rate_limit_is_explained()
    {
        var w = Writer("gsk_test");
        _status = HttpStatusCode.TooManyRequests;
        Assert.Null(await w.AssessMoodAsync(new MoodRequest(80, "x", [])));
        Assert.Equal("Bearer gsk_test", LastAuth);
        Assert.Contains("429", w.LastError);
    }

    [Fact]
    public async Task Bad_json_falls_back_to_rules()
    {
        var w = Writer();
        _reply = "Hôm nay bạn ổn đó!";
        Assert.Null(await w.AssessMoodAsync(new MoodRequest(80, "x", [])));
        Assert.Contains("không đúng dạng", w.LastError);
    }

    [Fact]
    public async Task Meeting_assessment_and_lines_work()
    {
        var w = Writer();
        _reply = "{\"load\":4,\"kind\":\"Trình bày\",\"recovery_min\":10,\"note\":\"Trình bày trước nhiều người\"}";
        var a = await w.AssessMeetingAsync(new MeetingRequest("e1", 60, 9, "Trình bày", true, "15:30", 2, 3, 0, false, false));
        Assert.Equal((4, 10), (a!.Load, a.RecoveryMin));
        _reply = "Bạn họp liền 2h40 rồi, đứng dậy vươn vai 5 phút nha.";
        Assert.NotNull(await w.WriteLineAsync(new LineRequest(CaseId.MeetingOverload, "Họp liên tục", "2h40", "nghỉ", "mẫu")));
        Assert.False(JsonDocument.Parse(LastBody!).RootElement.TryGetProperty("response_format", out _)); // câu thoại không ép JSON
    }

    [Fact]
    public void Provider_needs_base_url_and_remote_needs_a_key()
    {
        Assert.False(MiloLlm.Create(new LlmOptions { Provider = "OpenAI", BaseUrl = "" }, () => null).Available);
        Assert.False(MiloLlm.Create(new LlmOptions { Provider = "OpenAI", BaseUrl = "https://api.groq.com/openai/v1" }, () => null).Available);
        Assert.True(MiloLlm.Create(new LlmOptions { Provider = "OpenAI", BaseUrl = "https://api.groq.com/openai/v1" }, () => "k").Available);
        Assert.Equal("LLM_API_KEY", new LlmOptions { Provider = "OpenAI" }.KeyEnvVar);
        Assert.Equal("ANTHROPIC_API_KEY", new LlmOptions().KeyEnvVar);
    }
}
