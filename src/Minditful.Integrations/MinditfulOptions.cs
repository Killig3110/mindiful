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

/// <summary>Section "Minditful" trong appsettings.json.</summary>
public sealed class MinditfulOptions
{
    /// <summary>Demo | Sandbox | Production. Để trống thì mở màn hình chọn môi trường.</summary>
    public string? Environment { get; set; }
    public WorkDayOptions WorkDay { get; set; } = new();
    public ConnectionOptions Sandbox { get; set; } = new();
    public ConnectionOptions Production { get; set; } = new();

    public ConnectionOptions For(AppEnvironment env) => env == AppEnvironment.Production ? Production : Sandbox;
}

public sealed class WorkDayOptions
{
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
