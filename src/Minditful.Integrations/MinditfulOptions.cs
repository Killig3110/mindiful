using Minditful.Core.Engine;

namespace Minditful.Integrations;

public enum AppEnvironment
{
    /// <summary>Ngày mẫu chạy theo kịch bản prototype, không kết nối gì.</summary>
    Demo,
    /// <summary>Tài khoản cá nhân (Microsoft account + Azure DevOps org cá nhân) để thử tích hợp thật.</summary>
    Sandbox,
    /// <summary>Tenant Bosch: Teams presence, Outlook, Azure Boards thật.</summary>
    Production,
}

public static class AppEnvironments
{
    /// <summary>Nhận cả tên trong docs/KET-NOI-SANDBOX.md: Scenario (= Demo), Sandbox, Prod (= Production).</summary>
    public static AppEnvironment? Parse(string? s) => s?.Trim().ToLowerInvariant() switch
    {
        "demo" or "scenario" => AppEnvironment.Demo,
        "sandbox" => AppEnvironment.Sandbox,
        "production" or "prod" => AppEnvironment.Production,
        _ => null,
    };
}

/// <summary>Section "Minditful" trong appsettings.json.</summary>
public sealed class MinditfulOptions
{
    /// <summary>Demo | Sandbox | Production. Để trống thì mở màn hình chọn môi trường.</summary>
    public string? Environment { get; set; }
    public WorkDayOptions WorkDay { get; set; } = new();
    public ConnectionOptions Sandbox { get; set; } = new();
    public ConnectionOptions Production { get; set; } = new();
    public LlmOptions Llm { get; set; } = new();
    public Storage.StorageOptions Storage { get; set; } = new();
    public WellbeingOptions Wellbeing { get; set; } = new();

    public ConnectionOptions For(AppEnvironment env) => env == AppEnvironment.Production ? Production : Sandbox;
}

/// <summary>Section "Wellbeing": các tính năng mở rộng ngoài prototype, bật/tắt từng cái (Sandbox và Production).</summary>
public sealed class WellbeingOptions
{
    /// <summary>Đề nghị giữ khoảng trống dài nhất trong ngày làm khối tập trung; tới giờ tự bật Không làm phiền.</summary>
    public bool FocusPlan { get; set; } = true;
    public int FocusPlanMinMinutes { get; set; } = 60;
    /// <summary>Sáng thứ Hai tóm tắt tuần trước + 1 mẹo.</summary>
    public bool WeekReport { get; set; } = true;
    /// <summary>Nhắc uống nước / 20-20-20 sau mỗi N phút ngồi máy liên tục. 0 = tắt.</summary>
    public int MicroBreakEveryMinutes { get; set; } = 50;
    public int MicroBreakMaxPerDay { get; set; } = 6;
    /// <summary>Thẻ tan tầm hỏi "Hôm nay thấy sao?" và gợi ý nghỉ giữa chuỗi họp ngày mai.</summary>
    public bool EveningCheck { get; set; } = true;
    /// <summary>Teams báo đang trình chiếu (Presenting) → Milo trốn hẳn, kể cả chóp đuôi.</summary>
    public bool HideWhenPresenting { get; set; } = true;
    /// <summary>Tủ đồ: Milo có phụ kiện mới khi bạn về đúng giờ 3/5/10 ngày liền.</summary>
    public bool Wardrobe { get; set; } = true;
}

public sealed class WorkDayOptions
{
    /// <summary>
    /// "Flexible" (mặc định, kiểu Bosch): giờ bắt đầu = lần mở máy đầu ngày trong [FlexEarliestStart, FlexLatestStart],
    /// giờ về = bắt đầu + FlexHours. "Fixed": dùng đúng Start–End.
    /// </summary>
    public string Mode { get; set; } = "Flexible";
    public string FlexEarliestStart { get; set; } = "08:00";
    public string FlexLatestStart { get; set; } = "10:00";
    public double FlexHours { get; set; } = 9;
    public bool IsFlexible => !string.Equals(Mode, "Fixed", StringComparison.OrdinalIgnoreCase);
    public string Start { get; set; } = "09:00";
    public string End { get; set; } = "18:00";
    /// <summary>Idle bao lâu thì coi là rời máy (spec: một lần nghỉ = idle ≥ 5 phút).</summary>
    public int AwayAfterMinutes { get; set; } = 5;
    /// <summary>Idle dưới bao nhiêu giây thì coi là đang gõ (spec: dưới 3 giây).</summary>
    public int TypingIdleSeconds { get; set; } = 3;
    /// <summary>Số giây liên tục có thao tác mới tính là "đang gõ phím liên tục".</summary>
    public int TypingSustainSeconds { get; set; } = 20;
    /// <summary>Ngưỡng chuyển việc/giờ. Spec là 8 cho "chuyển giữa họp, IDE, việc khác"; đếm cửa sổ thật nên để cao hơn.</summary>
    public int FragmentationPerHour { get; set; } = 30;
    public double AvgInProgressBaseline { get; set; } = 2.8;
    public int RefreshMinutes { get; set; } = 5;
    public int PresencePollSeconds { get; set; } = 30;
}

public sealed class ConnectionOptions
{
    public GraphOptions Graph { get; set; } = new();
    public AzureDevOpsOptions AzureDevOps { get; set; } = new();
    /// <summary>"Graph" dùng /me/presence (tài khoản công ty). "Local" giả lập presence (tài khoản cá nhân không có presence).</summary>
    public string PresenceMode { get; set; } = "Graph";
    /// <summary>Ẩn Milo khỏi share màn hình (SetWindowDisplayAffinity).</summary>
    public bool ContentProtection { get; set; } = true;
    /// <summary>Chỉ nhắc Sắp họp với sự kiện isOnlineMeeting (Teams). Tài khoản cá nhân nên để false.</summary>
    public bool RequireOnlineMeeting { get; set; } = true;
    /// <summary>Email chờ ≥ N ngày làm việc (spec: 1). Sandbox có thể để 0 để thử ngay.</summary>
    public int MailMinWaitDays { get; set; } = 1;
    /// <summary>Sandbox: tính cả email tự gửi cho mình (dữ liệu mẫu do Seeder gửi).</summary>
    public bool IncludeSelfSentMail { get; set; }
    /// <summary>Mở cửa sổ Bộ não Milo khi khởi động.</summary>
    public bool ShowControlCenter { get; set; }
    /// <summary>Baseline workload khi lịch sử local chưa đủ 5 ngày (null = WorkDay.AvgInProgressBaseline).</summary>
    public double? AvgInProgressFallback { get; set; }
    public PollingOptions Polling { get; set; } = new();
    /// <summary>Chỉ Sandbox: rút gọn các ngưỡng phải chờ lâu để test trong 1 buổi. Prod để trống → ngưỡng chuẩn của tài liệu.</summary>
    public BehaviorOverrides? BehaviorOverrides { get; set; }
}

/// <summary>Chu kỳ đọc từng nguồn (docs/KET-NOI-SANDBOX.md mục 5–6).</summary>
public sealed class PollingOptions
{
    public int PresenceSeconds { get; set; } = 30;
    public int CalendarSeconds { get; set; } = 120;
    public int MailSeconds { get; set; } = 300;
    public int BoardsSeconds { get; set; } = 180;
}

/// <summary>Ngưỡng rút gọn cho Sandbox. Trường nào để trống thì giữ ngưỡng chuẩn.</summary>
public sealed class BehaviorOverrides
{
    public int? StuckTaskMinBusinessDays { get; set; }
    public int? EmailMinBusinessDaysWaiting { get; set; }
    /// <summary>"HH:mm" — Email chờ không giao trước giờ này (chuẩn 10:00).</summary>
    public string? EmailNotBefore { get; set; }
    public int? NoBreakStreakMin { get; set; }
    public int? OverloadMinChainCount { get; set; }
    /// <summary>Khoảng cách tối thiểu giữa 2 lời nhắc chủ động (chuẩn 15 phút).</summary>
    public int? BudgetGapMin { get; set; }
    /// <summary>[min, max] phút giữa 2 lần ghé ngang (chuẩn [30, 60]).</summary>
    public int[]? VisitEveryMin { get; set; }
    /// <summary>Chấm "1" trên đuôi giữ bao lâu (chuẩn 30 phút).</summary>
    public int? ParkedReminderTtlMin { get; set; }

    public string Describe() => string.Join(" · ", new[]
    {
        StuckTaskMinBusinessDays is { } s ? $"task kẹt ≥ {s} ngày" : null,
        EmailMinBusinessDaysWaiting is { } e ? $"email chờ ≥ {e} ngày" : null,
        EmailNotBefore is { } n ? $"email từ {n}" : null,
        NoBreakStreakMin is { } b ? $"làm liền {b}'" : null,
        OverloadMinChainCount is { } o ? $"họp liên tục ≥ {o} cuộc" : null,
        BudgetGapMin is { } g ? $"cách nhau {g}'" : null,
        VisitEveryMin is [var a, var z] ? $"ghé ngang {a}–{z}'" : null,
        ParkedReminderTtlMin is { } p ? $"chấm chờ {p}'" : null,
    }.Where(x => x is not null));
}

public sealed class GraphOptions
{
    public bool Enabled { get; set; } = true;
    /// <summary>Application (client) ID của app registration (public client, redirect http://localhost).</summary>
    public string ClientId { get; set; } = "";
    /// <summary>Tenant ID của Bosch cho Production; "consumers" cho tài khoản Microsoft cá nhân.</summary>
    public string TenantId { get; set; } = "organizations";
    public string RedirectUri { get; set; } = "http://localhost";
    /// <summary>Đăng nhập một chạm bằng tài khoản Windows (WAM). Chỉ có tác dụng với tài khoản công ty.</summary>
    public bool UseBroker { get; set; }
    public string[] Scopes { get; set; } = ["User.Read", "Calendars.ReadWrite", "Mail.Read", "Presence.ReadWrite"];
}

public sealed class AzureDevOpsOptions
{
    public bool Enabled { get; set; }
    /// <summary>Tên organization, ví dụ "bosch-xyz" trong https://dev.azure.com/bosch-xyz.</summary>
    public string Organization { get; set; } = "";
    public string Project { get; set; } = "";
    /// <summary>Team để lấy sprint hiện tại. Trống = team mặc định "{Project} Team".</summary>
    public string? Team { get; set; }
    /// <summary>"Pat" (spec: PAT lưu local) hoặc "Entra" (dùng chung đăng nhập Microsoft).</summary>
    public string Auth { get; set; } = "Pat";
    /// <summary>Biến môi trường chứa PAT nếu chưa nhập trong ứng dụng.</summary>
    public string PatEnvVar { get; set; } = "MINDITFUL_ADO_PAT";
    public string[] ActiveStates { get; set; } = ["Active", "In Progress", "Doing", "Committed"];
    public string[] DoneStates { get; set; } = ["Done", "Closed", "Resolved", "Completed"];
    public string[] WorkItemTypes { get; set; } = ["Task", "Bug", "User Story", "Product Backlog Item", "Issue"];
    /// <summary>Trạng thái "đang làm" khi Seeder tạo work item mẫu (Agile: Active, Scrum: In Progress, Basic: Doing).</summary>
    public string SeedState { get; set; } = "Active";
}

/// <summary>Lớp 2 (§8b, §14): LLM viết lại câu chính và trả lời chat tự do. Tắt hoặc lỗi thì dùng template.</summary>
public sealed class LlmOptions
{
    public bool Enabled { get; set; } = true;
    /// <summary>Dùng cả trong Demo (ngày mẫu sẽ không còn giống hệt prototype từng chữ).</summary>
    public bool UseInDemo { get; set; }
    public string Model { get; set; } = "claude-opus-5";
    /// <summary>Câu ngắn nên để effort thấp cho nhanh.</summary>
    public string Effort { get; set; } = "low";
    /// <summary>Spec: 2.5 giây, quá thì dùng template.</summary>
    public int TimeoutMs { get; set; } = 2500;
    /// <summary>Biến môi trường chứa API key nếu chưa nhập trong ứng dụng.</summary>
    public string ApiKeyEnvVar { get; set; } = "ANTHROPIC_API_KEY";
    /// <summary>Server-side fallback khi model từ chối (beta server-side-fallback-2026-07-01).</summary>
    public bool RefusalFallback { get; set; } = true;
    /// <summary>Đánh giá mood/cuộc họp chạy nền nên được chờ lâu hơn câu thoại.</summary>
    public int InsightTimeoutMs { get; set; } = 20000;
    public LlmFeatures Features { get; set; } = new();
}

/// <summary>Bật/tắt từng tính năng LLM. Không có API key thì mọi thứ tự về luật.</summary>
public sealed class LlmFeatures
{
    /// <summary>Claude viết câu chính của thẻ nhắc.</summary>
    public bool Lines { get; set; } = true;
    /// <summary>Claude trả lời chat tự do khi từ khoá không khớp.</summary>
    public bool Chat { get; set; } = true;
    /// <summary>Rules | Hybrid | Llm — xem MoodMode.</summary>
    public string Mood { get; set; } = "Rules";
    /// <summary>Rules | Llm — đánh giá mức nặng từng cuộc họp.</summary>
    public string Meetings { get; set; } = "Rules";
    public int MoodIntervalMinutes { get; set; } = 30;
    /// <summary>Gửi kèm câu người dùng tự gõ cho Milo để Claude đọc cảm xúc (mặc định tắt).</summary>
    public bool IncludeChatInMood { get; set; }

    public MoodMode MoodMode => Enum.TryParse<MoodMode>(Mood, true, out var m) ? m : MoodMode.Rules;
    public MeetingMode MeetingMode => Enum.TryParse<MeetingMode>(Meetings, true, out var m) ? m : MeetingMode.Rules;
}
