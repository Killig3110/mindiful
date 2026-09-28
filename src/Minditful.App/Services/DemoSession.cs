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
    /// <summary>Khoảng trống an toàn của ngày mẫu (giữa Lịch kín 12:55 và Email chờ 13:10) để bật thử 1 case.</summary>
    private static readonly double FreeMoment = Tm.T("13:02");

    public AppEnvironment Env => AppEnvironment.Demo;
    public MiloEngine Engine { get; } = DemoScenario.CreateEngine();
    public bool ContentProtection => false;
    public bool Playing { get; set; } = true;
    /// <summary>Tốc độ tua khi Milo ẩn; lúc Milo hiện luôn chạy 1× để xem trọn hoạt ảnh.</summary>
    public int Speed { get; set; } = 120;
    public string LlmStatus { get; }

    /// <summary>Trạng thái kịch bản vừa đổi (nhảy mốc, bật case…) — bảng điều khiển vẽ lại.</summary>
    public event Action? Changed;

    public DemoSession(MinditfulOptions opt)
    {
        // Demo không đụng tới thế giới thật: hành động của Milo chỉ ghi vào nhật ký (engine đã log sẵn).
        if (opt.Llm.Enabled && opt.Llm.UseInDemo)
        {
            var key = new SecretStore(AppEnvironment.Demo, "claude-api-key");
            var writer = new ClaudeLineWriter(opt.Llm, () => key.Read() ?? Environment.GetEnvironmentVariable(opt.Llm.ApiKeyEnvVar));
            LlmBridge.Attach(Engine, writer, Application.Current.Dispatcher, opt.Llm.Features);
            LlmStatus = "Lớp 2: " + LlmBridge.ApplyModes(Engine, writer, opt.Llm);
        }
        else LlmStatus = "Lớp 2 tắt trong Demo (Llm.UseInDemo = false): câu mẫu, mood và đánh giá cuộc họp theo luật — giống prototype.";
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
        if (Engine.HardGate() is not null || Engine.S.Ended) Engine.RunTo(FreeMoment);
        Engine.ForceCase(c);
        Playing = true;
        Changed?.Invoke();
    }

    public void Dispose() { }
}
