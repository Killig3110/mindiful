using Minditful.Core.Engine;

namespace Minditful.Integrations.Llm;

/// <summary>
/// Lớp 2 của Milo: viết câu thoại, trả lời chat, chấm mood, đánh giá cuộc họp. Mọi hàm trả null khi hết giờ / lỗi
/// để engine dùng luật và câu mẫu. Chỉ gửi tên tình huống và số liệu, không bao giờ gửi tiêu đề hay nội dung công việc.
/// </summary>
public interface IMiloLlm
{
    /// <summary>Nhà cung cấp + model, vd. "Claude · claude-opus-5", "Ollama · qwen3:8b".</summary>
    string Name { get; }
    bool Available { get; }
    string? LastError { get; }
    Task<string?> WriteLineAsync(LineRequest r, CancellationToken ct = default);
    /// <summary>Câu trả lời chat + tối đa 2 tính năng đề nghị (đã lọc theo <see cref="ChatRequest.Offer"/>).</summary>
    Task<ChatReply?> ReplyChatAsync(ChatRequest r, CancellationToken ct = default);
    Task<MoodInsight?> AssessMoodAsync(MoodRequest r, CancellationToken ct = default);
    Task<MeetingAssessment?> AssessMeetingAsync(MeetingRequest r, CancellationToken ct = default);
}

public static class MiloLlm
{
    /// <summary>Claude (SDK Anthropic) hoặc dịch vụ tương thích OpenAI (Ollama, Groq, Gemini, OpenRouter…) theo Llm.Provider.</summary>
    public static IMiloLlm Create(LlmOptions opt, Func<string?> storedKey) => opt.IsOpenAiCompatible
        ? new OpenAiCompatibleWriter(opt, () => storedKey() ?? Environment.GetEnvironmentVariable(opt.KeyEnvVar))
        : new ClaudeLineWriter(opt, () => storedKey() ?? Environment.GetEnvironmentVariable(opt.KeyEnvVar));
}
