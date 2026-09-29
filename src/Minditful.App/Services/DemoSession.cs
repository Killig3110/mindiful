using System.Windows;
using Minditful.Core.Engine;
using Minditful.Core.Scenario;
using Minditful.Integrations;
using Minditful.Integrations.Llm;

namespace Minditful.App.Services;

/// <summary>
/// Môi trường Demo: vẫn là app thật (Milo sống trên desktop, khay hệ thống, dashboard, thẻ…) như Sandbox/Production,
/// nhưng đồng hồ, dữ liệu và thao tác của người dùng lấy từ kịch bản ngày mẫu Thứ Năm 24/9 để chạy đủ mọi case của prototype.
/// Không kết nối mạng, không đọc tín hiệu của máy (khoá máy, gõ phím…) để khỏi lẫn với kịch bản.
/// </summary>
internal sealed class DemoSession : IMiloSession
{
    public AppEnvironment Env => AppEnvironment.Demo;
    public MiloEngine Engine { get; } = DemoScenario.CreateEngine();
    public bool ContentProtection => false;
    public bool Playing { get; set; } = true;
    /// <summary>Tốc độ tua khi Milo ẩn; lúc Milo hiện luôn chạy 1× để xem trọn hoạt ảnh.</summary>
    public int Speed { get; set; } = 120;
    public string LlmStatus { get; }
    /// <summary>LLM cho công tắc Luật ↔ AI và bộ kiểm chứng (có key hoặc Ollama thì dùng được, kể cả khi UseInDemo = false).</summary>
    public IMiloLlm Llm { get; }
    public LlmOptions LlmOptions { get; }

    /// <summary>Trạng thái kịch bản vừa đổi (nhảy mốc, bật case…) — bảng điều khiển vẽ lại.</summary>
    public event Action? Changed;

    public DemoSession(MinditfulOptions opt)
    {
        // Demo không đụng tới thế giới thật: hành động của Milo chỉ ghi vào nhật ký (engine đã log sẵn).
        var key = new SecretStore(AppEnvironment.Demo, "claude-api-key");
        LlmOptions = opt.Llm;
        Engine.Cfg.Personality = PersonalitySetting.Load(AppEnvironment.Demo, opt.Wellbeing);
        Llm = MiloLlm.Create(opt.Llm, key.Read);
        // Câu thoại / chat bằng AI chỉ khi UseInDemo; chấm mood và cuộc họp thì bật được bằng công tắc trên bảng điều khiển
        var features = new LlmFeatures
        {
            Lines = opt.Llm.UseInDemo && opt.Llm.Features.Lines, Chat = opt.Llm.UseInDemo && opt.Llm.Features.Chat,
            Mood = opt.Llm.Features.Mood, Meetings = opt.Llm.Features.Meetings,
            MoodIntervalMinutes = opt.Llm.Features.MoodIntervalMinutes, IncludeChatInMood = opt.Llm.Features.IncludeChatInMood,
        };
        LlmBridge.Attach(Engine, Llm, Application.Current.Dispatcher, features, opt.Llm.DemoMoodMinSeconds);
        if (opt.Llm.Enabled && opt.Llm.UseInDemo) LlmStatus = "Lớp 2: " + LlmBridge.ApplyModes(Engine, Llm, opt.Llm);
        else LlmStatus = Llm.Available
            ? $"Có AI ({Llm.Name}). Ngày mẫu mặc định chấm điểm bằng luật; bật AI ở trang Mood Engine."
            : "Chưa có AI: câu mẫu, mood và đánh giá cuộc họp theo luật — giống prototype. Có key hoặc Ollama thì bật ở trang Mood Engine.";
    }

    public void Pump(double realDt)
    {
        if (!Playing || Engine.S.Ended) return;
        Engine.Advance(realDt * (Engine.Busy ? 1 : Speed));
        if (Engine.S.Ended)
        {
            Playing = false;
            Changed?.Invoke();
        }
    }

    public void TogglePlay()
    {
        if (Engine.S.Ended) return;
        Playing = !Playing;
        Changed?.Invoke();
    }

    public void Restart()
    {
        Engine.Reset();
        Playing = true;
        Changed?.Invoke();
    }

    public void JumpTo(Milestone m)
    {
        Engine.RunTo(Math.Max(Engine.Cfg.DayOpen, Tm.T(m.Time) - m.LeadSeconds));
        Playing = true;
        Changed?.Invoke();
    }

    /// <summary>Bật ngay 1 case. Nếu lúc đó Milo không được phép nói (khoá máy, họp, tập trung) thì tua tới khoảng trống 13:02 trước.</summary>
    public void RunCase(CaseId c)
    {
        DemoTour.RunCase(Engine, c);
        Playing = true;
        Changed?.Invoke();
    }

    /// <summary>Kịch bản trình diễn: áp 1 bước (tua tới mốc / cho Milo làm case / trình chiếu / mood realtime).</summary>
    public void RunTourStep(TourStep step)
    {
        DemoTour.Apply(Engine, step);
        Playing = true;
        Changed?.Invoke();
    }

    public void Dispose() { }
}
