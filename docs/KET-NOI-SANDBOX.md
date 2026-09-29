# Kết nối Minditful (Milo) với môi trường Sandbox

> Hướng dẫn nối app WPF của Minditful với sandbox **Mindiful** (Microsoft 365 + Azure DevOps) để test Milo bằng dữ liệu thật: Teams presence, lịch Outlook, email và Azure Boards.
> Đọc kèm tài liệu **Kịch bản hành vi Milo** (các số "mục X" bên dưới trỏ về tài liệu đó) và prototype **Milo sống**.

---

## 0. Tóm tắt nhanh

| Thành phần | Giá trị sandbox | Ghi chú |
|---|---|---|
| Tenant Microsoft 365 | `mindiful.onmicrosoft.com` | 4 user, license Microsoft 365 Business Standard (có Teams + Exchange) |
| Người dùng chính (máy chạy Milo) | `thulu@mindiful.onmicrosoft.com` | Đăng nhập app bằng account này |
| "Đồng nghiệp" tạo dữ liệu | `killig@` (Duy, admin), `qk76@` (Quân), `milo@` | Mời họp, gửi email, kéo task |
| App registration | **Minditful Milo (Sandbox)**, single tenant, public client | Redirect URI `http://localhost` |
| Directory (tenant) ID | `093be8d4-f285-4982-a198-db10d74e61e2` | Entra → App registrations → Overview |
| Application (client) ID | `aa4ae2a6-4d2a-474c-a6f5-d7d2ebf5de06` | Như trên |
| Azure DevOps org | `https://dev.azure.com/mindiful-sandbox` (ví dụ `mindiful-sandbox`) | Gắn Azure subscription free trial, ≤ 5 user Basic = 0đ |
| Project / Team | `Milo-Sandbox` / `Milo-Sandbox Team` | Process **Agile**, sprint `Sprint 1` |
| PAT | Do **thulu@** tạo, scope *Work Items R&W* + *Project and Team R* | **Bí mật**, không commit, không gửi qua chat |

Ba môi trường của app:

| Môi trường | Dữ liệu | Đăng nhập | Ngưỡng hành vi |
|---|---|---|---|
| `Scenario` (Test kịch bản) | Ngày mẫu giả lập, tua giờ được | Không cần | Chuẩn |
| `Sandbox` (tài liệu này) | Thật, tenant Mindiful | MSAL + PAT | **Rút gọn** để test nhanh |
| `Prod` | Thật, tenant Bosch | MSAL + PAT | Chuẩn |

---

## 1. Kiểm tra lại phần đã setup

Trước khi viết code, xác nhận 4 việc dưới đây đều đã xong.

- [ ] **App registration**: Entra → App registrations → *Minditful Milo (Sandbox)*
  - *Authentication* → Redirect URI loại **Mobile and desktop applications** có `http://localhost`
  - *Authentication → Settings* → **Allow public client flows = Yes**
  - *API permissions*: các quyền dưới đây đều *Granted for Mindiful*

| Quyền Graph (Delegated) | Dùng cho | Bắt buộc? |
|---|---|---|
| `User.Read` | Đăng nhập, lấy ID người dùng | Có |
| `Presence.Read` | Cổng im lặng (mục 4) | Có |
| `Presence.ReadWrite` | Tự bật Không làm phiền khi khoá tập trung (mục 7.4) | Có |
| `Calendars.Read` | Sắp họp, chuỗi họp, bản tin sáng | Có |
| `Calendars.ReadWrite` | Giữ chỗ nghỉ, khoá tập trung, khoá trưa (mục 7.2, 7.4, 8) | Có |
| `Mail.Read` | Email chờ, số email chưa đọc (mục 7.3) | Có |
| `Mail.Send` | Không cần. Email mẫu phải do *người khác* gửi tới Thư (xem mục 7) | Không |

- [ ] **Teams**: thulu@ đã đăng nhập Teams desktop hoặc web ít nhất 1 lần. Nếu không, presence luôn trả `Offline`.
- [ ] **Outlook**: time zone **(UTC+07:00) Bangkok, Hanoi, Jakarta**, work hours 09:00–18:00.
- [ ] **Azure DevOps**: project `Milo-Sandbox` (Agile), `Sprint 1` đã đặt ngày và được chọn cho team, có 3 task *Active* giao cho Thu Ly.

### Kiểm tra bằng tay trong 2 phút

**Graph Explorer** (https://developer.microsoft.com/graph/graph-explorer), đăng nhập thulu@:

```
GET https://graph.microsoft.com/v1.0/me/presence
GET https://graph.microsoft.com/v1.0/me/calendarView?startDateTime=2026-09-29T00:00:00&endDateTime=2026-09-30T00:00:00&$select=subject,start,end,isOnlineMeeting
GET https://graph.microsoft.com/v1.0/me/mailFolders/inbox?$select=unreadItemCount
```

**Azure DevOps** (terminal):

```bash
PAT='<dán PAT vào đây, đừng lưu lịch sử>'
ORG='mindiful-sandbox'
curl -s -u ":$PAT" -H "Content-Type: application/json" \
  -d '{"query":"SELECT [System.Id],[System.Title],[System.State] FROM WorkItems WHERE [System.AssignedTo] = @Me AND [System.State] = '\''Active'\''"}' \
  "https://dev.azure.com/$ORG/Milo-Sandbox/_apis/wit/wiql?api-version=7.1"
```

Kết quả phải có `"workItems":[{"id":...}]` với 3 task Active. Nếu nhận về trang HTML đăng nhập (HTTP 203), tức là PAT sai hoặc hết hạn.

---

## 2. Cấu trúc project đề xuất

```
Minditful.sln
├─ src/
│  ├─ Minditful.Core/            # net8.0 — engine hành vi (port từ prototype), không phụ thuộc Windows
│  │   ├─ Engine/                # Signals, MoodEngine, RuleEngine, Arbiter, EpisodePlayer
│  │   ├─ Model/                 # CalendarEvent, MailItem, WorkItemInfo, PresenceInfo, SprintInfo
│  │   └─ Sources/               # interface ICalendarSource, IPresenceSource, IMailSource, IBoardsSource, IActionSink
│  ├─ Minditful.Integrations/    # net8.0 — Graph + Azure DevOps (REST thuần qua HttpClient)
│  │   ├─ Auth/GraphAuth.cs
│  │   ├─ Graph/GraphClient.cs
│  │   ├─ Graph/GraphSources.cs  # hiện thực các interface ở Core
│  │   ├─ DevOps/AzureDevOpsClient.cs
│  │   └─ Secrets/PatStore.cs    # DPAPI
│  └─ Minditful.App/             # net8.0-windows — WPF overlay, cửa sổ điều khiển, ActivityMonitor
│      ├─ appsettings.json
│      ├─ appsettings.Scenario.json
│      ├─ appsettings.Sandbox.json
│      └─ appsettings.Prod.json
└─ docs/KET-NOI-SANDBOX.md       # file này
```

NuGet cần thêm (ở `Minditful.Integrations`):

```
dotnet add package Microsoft.Identity.Client
dotnet add package Microsoft.Identity.Client.Extensions.Msal
dotnet add package Microsoft.Extensions.Configuration.Json
```

App gọi Graph bằng `HttpClient` và `System.Text.Json` thay vì Microsoft Graph SDK: nhẹ hơn, và dễ đổi giữa Sandbox và Prod.

---

## 3. Cấu hình

### 3.1 Chọn môi trường khi chạy

Thứ tự ưu tiên khi chọn môi trường:
1. Tham số dòng lệnh `--env Sandbox`
2. Biến môi trường `MINDITFUL_ENV=Sandbox`
3. Giá trị `DefaultEnvironment` trong `appsettings.json`

Trong Visual Studio: Project → Properties → Debug → *Command line arguments*, thêm `--env Sandbox`. Nên tạo sẵn 3 launch profile (`Scenario`, `Sandbox`, `Prod`).

### 3.2 `appsettings.Sandbox.json` (được commit)

```json
{
  "Environment": "Sandbox",
  "Graph": {
    "TenantId": "<ĐIỀN_TENANT_ID>",
    "ClientId": "<ĐIỀN_CLIENT_ID>",
    "RedirectUri": "http://localhost",
    "Scopes": [
      "User.Read", "Presence.Read", "Presence.ReadWrite",
      "Calendars.Read", "Calendars.ReadWrite", "Mail.Read"
    ],
    "TimeZone": "SE Asia Standard Time",
    "TreatAllEventsAsMeetings": false
  },
  "AzureDevOps": {
    "Organization": "mindiful-sandbox",
    "Project": "Milo-Sandbox",
    "Team": "Milo-Sandbox Team",
    "ActiveStates": ["Active"],
    "DoneStates": ["Closed", "Resolved"],
    "AvgInProgressFallback": 2.5
  },
  "Polling": {
    "PresenceSeconds": 30,
    "CalendarSeconds": 120,
    "MailSeconds": 300,
    "BoardsSeconds": 180
  },
  "User": {
    "WorkWindowStart": "09:00",
    "WorkWindowEnd": "18:00"
  },
  "BehaviorOverrides": {
    "stuckTaskMinBusinessDays": 0,
    "emailMinBusinessDaysWaiting": 0,
    "emailNotBefore": "00:00",
    "noBreakStreakMin": 20,
    "overloadMinChainCount": 3,
    "budgetGapMin": 3,
    "visitEveryMin": [3, 5],
    "parkedReminderTtlMin": 5
  }
}
```

`BehaviorOverrides` chỉ có ở Sandbox. Nó rút ngắn các ngưỡng phải chờ lâu (task Active 3 ngày, email chờ 1 ngày, làm liền 2 giờ, ngân sách 15 phút, nhắc uống nước 50 phút, khoảng trống giữ giờ tập trung 60 phút) để test trong một buổi. Prod không có khối này, nên dùng đúng ngưỡng trong tài liệu (mục 4, 8, 11).

Ngưỡng rút gọn chỉ áp dụng khi Sandbox ở **chế độ test** (`"TestMode": true`, mặc định). Bảng điều khiển → *Tổng quan* → *Chế độ Sandbox* (hoặc menu khay → *Chế độ test*) chuyển sang **Chạy như Production**: về ngưỡng chuẩn, ẩn trang *Thử tình huống*, bỏ tín hiệu giả lập. Việc chuyển chế độ không cần mở lại app và được nhớ cho lần sau.

### 3.3 Bí mật (không commit)

| Bí mật | Lưu ở đâu | Cách nhập |
|---|---|---|
| PAT Azure DevOps | `%AppData%\Minditful\Sandbox\pat.bin`, mã hoá DPAPI (`ProtectedData`, scope CurrentUser) | Cửa sổ **Cài đặt → Azure DevOps → Dán PAT** |
| Token Microsoft | `%LocalAppData%\Minditful\Sandbox\msal_cache.bin`, MSAL tự mã hoá bằng DPAPI | Tự động sau lần đăng nhập đầu |

Thêm vào `.gitignore`:

```gitignore
appsettings.*.local.json
*.pat
msal_cache*.bin
```

---

## 4. Đăng nhập Microsoft (MSAL)

```csharp
// Minditful.Integrations/Auth/GraphAuth.cs
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

public sealed class GraphAuth
{
    private readonly IPublicClientApplication _app;
    private readonly string[] _scopes;

    private GraphAuth(IPublicClientApplication app, string[] scopes) { _app = app; _scopes = scopes; }

    public static async Task<GraphAuth> CreateAsync(GraphOptions o, string envName)
    {
        var app = PublicClientApplicationBuilder.Create(o.ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, o.TenantId)   // single tenant
            .WithRedirectUri(o.RedirectUri)                              // http://localhost
            .Build();

        // Cache token đã mã hoá, mỗi môi trường một file để không lẫn Sandbox với Prod
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Minditful", envName);
        Directory.CreateDirectory(dir);
        var storage = new StorageCreationPropertiesBuilder("msal_cache.bin", dir).Build();
        var helper = await MsalCacheHelper.CreateAsync(storage);
        helper.RegisterCache(app.UserTokenCache);

        return new GraphAuth(app, o.Scopes);
    }

    /// Lấy token im lặng; chỉ mở trình duyệt khi thật sự cần (lần đầu, hết hạn refresh token, thiếu consent).
    public async Task<string> GetTokenAsync(bool allowInteractive, CancellationToken ct = default)
    {
        var account = (await _app.GetAccountsAsync()).FirstOrDefault();
        try
        {
            var r = await _app.AcquireTokenSilent(_scopes, account).ExecuteAsync(ct);
            return r.AccessToken;
        }
        catch (MsalUiRequiredException) when (allowInteractive)
        {
            var r = await _app.AcquireTokenInteractive(_scopes)
                .WithUseEmbeddedWebView(false)            // mở trình duyệt hệ thống, về lại http://localhost
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync(ct);
            return r.AccessToken;
        }
    }

    public async Task SignOutAsync()
    {
        foreach (var a in await _app.GetAccountsAsync()) await _app.RemoveAsync(a);
    }
}
```

Quy tắc khi dùng:
- **Chỉ gọi `allowInteractive: true`** khi người dùng bấm *Đăng nhập* hoặc lúc app mở lần đầu. Vòng polling nền luôn dùng `false`. Nếu gặp `MsalUiRequiredException`, Milo hiện 1 dòng "Cần đăng nhập lại" trong dashboard, **không** tự bật trình duyệt giữa giờ làm.
- Lần đăng nhập đầu, chọn **thulu@mindiful.onmicrosoft.com**. Nếu trình duyệt đang đăng nhập account Bosch hay account cá nhân, chọn *Use another account*.

---

## 5. Microsoft Graph: Teams presence và Outlook

### 5.1 Client dùng chung

```csharp
// Minditful.Integrations/Graph/GraphClient.cs
public sealed class GraphClient
{
    private static readonly HttpClient Http = new() { BaseAddress = new Uri("https://graph.microsoft.com/v1.0/") };
    private readonly GraphAuth _auth; private readonly string _tz;
    public GraphClient(GraphAuth auth, string windowsTimeZone) { _auth = auth; _tz = windowsTimeZone; }

    public async Task<JsonDocument?> SendAsync(HttpMethod m, string url, object? body = null, CancellationToken ct = default)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            using var req = new HttpRequestMessage(m, url);
            req.Headers.Authorization = new("Bearer", await _auth.GetTokenAsync(allowInteractive: false, ct));
            req.Headers.Add("Prefer", $"outlook.timezone=\"{_tz}\"");   // giờ trả về theo UTC+7
            if (body != null) req.Content = JsonContent.Create(body);

            using var res = await Http.SendAsync(req, ct);
            if ((int)res.StatusCode == 429 || (int)res.StatusCode >= 500)
            {   // bị throttle / lỗi tạm thời: chờ theo Retry-After rồi thử lại
                var wait = res.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(5 * (attempt + 1));
                await Task.Delay(wait, ct); continue;
            }
            if (res.StatusCode == HttpStatusCode.NoContent) return null;
            if (!res.IsSuccessStatusCode) throw new GraphException(res.StatusCode, await res.Content.ReadAsStringAsync(ct));
            return await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        }
        throw new GraphException(HttpStatusCode.TooManyRequests, "Graph throttled");
    }
}
```

### 5.2 Presence Teams: cổng im lặng (mục 4)

```csharp
// GET /me/presence  → { availability, activity }
var doc = await graph.SendAsync(HttpMethod.Get, "me/presence");
var activity = doc!.RootElement.GetProperty("activity").GetString();       // InACall, Presenting, InAMeeting, DoNotDisturb, Available, Away, Offline...
var availability = doc.RootElement.GetProperty("availability").GetString();
```

Map sang tín hiệu của engine:

| `activity` | Tín hiệu Milo | Cổng |
|---|---|---|
| `InACall`, `InAConferenceCall`, `InAMeeting` | `inCall = true` | Đang họp |
| `Presenting` | `inCall = true`, `presenting = true` | Đang họp |
| `DoNotDisturb`, `UrgentInterruptionsOnly` | `userDnd = true`, trừ khi do chính Milo bật (7.4) | Không làm phiền |
| `Offline`, lỗi 403/404 | Dùng **dự phòng**: `inCall` = đang có sự kiện `isOnlineMeeting` diễn ra trong lịch | — |
| Còn lại | `inCall = false` | — |

Polling mỗi **30 giây**. Nếu presence hiện `Offline` suốt 10 phút trong giờ làm, ghi 1 dòng log "Teams chưa đăng nhập, đang dùng lịch để đoán cuộc họp".

**Bật Không làm phiền khi khoá tập trung (mục 7.4):**

```csharp
// POST /users/{userId}/presence/setUserPreferredPresence  (Presence.ReadWrite)
await graph.SendAsync(HttpMethod.Post, $"users/{me.Id}/presence/setUserPreferredPresence", new
{
    availability = "DoNotDisturb",
    activity = "DoNotDisturb",
    expirationDuration = $"PT{focusMin}M"      // ví dụ PT90M, hết hạn tự trả về trạng thái cũ
});
```

Lưu ý: *preferred presence* chỉ có tác dụng khi thulu@ **đang mở Teams**. App nhớ cờ `dndSetByMilo = true` để không nhầm thành DND do người dùng tự bật.

### 5.3 Lịch Outlook: Sắp họp, chuỗi họp, bản tin sáng (mục 6.1, 7.1, 7.2, 8)

```csharp
var today = DateTime.Today;
var url = "me/calendarView"
        + $"?startDateTime={today:yyyy-MM-dd}T00:00:00&endDateTime={today.AddDays(1):yyyy-MM-dd}T23:59:59"
        + "&$select=id,subject,start,end,isOnlineMeeting,onlineMeeting,organizer,attendees,showAs,isCancelled,hasAttachments,responseStatus,categories"
        + "&$orderby=start/dateTime&$top=100";
var doc = await graph.SendAsync(HttpMethod.Get, url);
```

Chuyển mỗi sự kiện sang `CalendarEvent` như sau:
- **Bỏ qua**: `isCancelled = true`; `responseStatus.response = "declined"`; `showAs = "free"`; sự kiện có category `Milo` (đó là khối Milo tự tạo).
- **Là cuộc họp** khi `isOnlineMeeting = true`. Nếu `TreatAllEventsAsMeetings = true` thì mọi sự kiện có người tham dự đều tính là họp.
- **Link tham gia**: `onlineMeeting.joinUrl`, dùng cho nút *Tham gia*.
- **Vai trò** (Meeting Classifier đơn giản; nếu nhóm đã có classifier riêng thì cắm vào đây):
  - `organizer.emailAddress.address` = mình → **Chủ trì / Trình bày**
  - mình nằm trong `attendees` với `type = "required"` → **Bắt buộc**; `type = "optional"` → **Tuỳ chọn**
- **File đính kèm** (nút *Mở slide*): khi `hasAttachments = true`, gọi `GET me/events/{id}/attachments?$select=name,contentType` và lấy file đầu tiên.
- Ngày mai, lấy **sự kiện đầu tiên** để viết dòng "Mai 9:00 có Daily" trong thẻ Tan tầm (mục 6.2).

Polling mỗi **2 phút**, và gọi thêm ngay khi máy mở khoá hoặc resume.

**Tạo khối lịch (Giữ chỗ nghỉ / Khoá tập trung / Khoá trưa):**

```csharp
// POST /me/events  (Calendars.ReadWrite)
await graph.SendAsync(HttpMethod.Post, "me/events", new
{
    subject = "Nghỉ cùng Milo",                       // hoặc "Tập trung: #4821", "Nghỉ trưa"
    start = new { dateTime = start.ToString("s"), timeZone = tz },
    end   = new { dateTime = end.ToString("s"),   timeZone = tz },
    showAs = "tentative",                             // khoá tập trung dùng "busy"
    isReminderOn = false,
    categories = new[] { "Milo" }                     // để lần đọc sau bỏ qua và dashboard gắn nhãn "Nghỉ"/"Tập trung"
});
```

### 5.4 Outlook mail: Email chờ bạn (mục 7.3)

Graph không có cờ "đã trả lời", nên phải so với thư mục **Sent Items** theo `conversationId`:

```csharp
var since = DateTime.UtcNow.AddDays(-14).ToString("yyyy-MM-ddTHH:mm:ssZ");

var inbox = await graph.SendAsync(HttpMethod.Get,
    $"me/mailFolders/inbox/messages?$filter=receivedDateTime ge {since}"
  + "&$select=id,subject,from,toRecipients,receivedDateTime,flag,conversationId,isRead,bodyPreview,webLink"
  + "&$orderby=receivedDateTime desc&$top=50");

var sent = await graph.SendAsync(HttpMethod.Get,
    $"me/mailFolders/sentitems/messages?$filter=sentDateTime ge {since}&$select=conversationId,sentDateTime&$top=100");

var unread = (await graph.SendAsync(HttpMethod.Get, "me/mailFolders/inbox?$select=unreadItemCount"))!
    .RootElement.GetProperty("unreadItemCount").GetInt32();              // cho bản tin sáng
```

Một email được tính là **đang chờ bạn** khi thoả cả 4 điều kiện:
1. Mình nằm trong `toRecipients` (chỉ CC thì không tính) và người gửi không phải mình.
2. `subject` hoặc `bodyPreview` có dấu `?`, **hoặc** `flag.flagStatus = "flagged"`.
3. Trong Sent Items không có thư nào cùng `conversationId` gửi **sau** `receivedDateTime`.
4. Số ngày làm việc (bỏ T7, CN) từ `receivedDateTime` tới nay ≥ `emailMinBusinessDaysWaiting` (Sandbox = 0, Prod = 1).

Nút *Mở Outlook* mở `webLink` của email đầu tiên. Polling mỗi **5 phút**.

**Riêng tư:** tiêu đề và nội dung email chỉ dùng trên máy để lọc và hiển thị. Chúng **không bao giờ** được gửi cho LLM (xem tài liệu, mục 14).

---

## 6. Azure DevOps: Azure Boards (mục 7.4, 7.5, 11)

### 6.1 Client

```csharp
// Minditful.Integrations/DevOps/AzureDevOpsClient.cs
public sealed class AzureDevOpsClient
{
    private readonly HttpClient _http;
    private readonly DevOpsOptions _o;

    public AzureDevOpsClient(DevOpsOptions o, string pat)
    {
        _o = o;
        _http = new HttpClient { BaseAddress = new Uri($"https://dev.azure.com/{o.Organization}/") };
        var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + pat));
        _http.DefaultRequestHeaders.Authorization = new("Basic", basic);
    }

    // Danh sách ID task của mình theo điều kiện state
    public async Task<int[]> QueryIdsAsync(string whereState, CancellationToken ct = default)
    {
        var wiql = new { query =
            "SELECT [System.Id] FROM WorkItems " +
            $"WHERE [System.TeamProject] = '{_o.Project}' AND [System.AssignedTo] = @Me AND {whereState}" };
        using var res = await _http.PostAsJsonAsync($"{_o.Project}/_apis/wit/wiql?api-version=7.1", wiql, ct);
        EnsureNotSignInPage(res);
        var doc = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return doc.GetProperty("workItems").EnumerateArray().Select(w => w.GetProperty("id").GetInt32()).ToArray();
    }

    public async Task<JsonElement> GetItemsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var list = string.Join(",", ids.Take(200));
        const string fields = "System.Id,System.Title,System.State,System.IterationPath,"
                            + "Microsoft.VSTS.Common.ActivatedDate,Microsoft.VSTS.Common.StateChangeDate,Microsoft.VSTS.Common.ClosedDate";
        using var res = await _http.GetAsync($"_apis/wit/workitems?ids={list}&fields={fields}&api-version=7.1", ct);
        EnsureNotSignInPage(res);
        return await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
    }

    public async Task<JsonElement> GetCurrentIterationAsync(CancellationToken ct = default)
    {
        var team = Uri.EscapeDataString(_o.Team);
        using var res = await _http.GetAsync($"{_o.Project}/{team}/_apis/work/teamsettings/iterations?$timeframe=current&api-version=7.1", ct);
        EnsureNotSignInPage(res);
        return await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
    }

    // PAT sai/hết hạn → Azure DevOps trả 203 + trang HTML đăng nhập thay vì 401
    private static void EnsureNotSignInPage(HttpResponseMessage res)
    {
        if (res.StatusCode == HttpStatusCode.NonAuthoritativeInformation || res.StatusCode == HttpStatusCode.Unauthorized)
            throw new DevOpsAuthException("PAT không hợp lệ hoặc đã hết hạn");
        res.EnsureSuccessStatusCode();
    }
}
```

### 6.2 Từ dữ liệu Boards ra tín hiệu Milo

Chạy mỗi **3 phút**:

```csharp
var activeIds = await ado.QueryIdsAsync("[System.State] IN ('Active')");
var doneTodayIds = await ado.QueryIdsAsync(
    "[System.State] IN ('Closed','Resolved') AND [Microsoft.VSTS.Common.StateChangeDate] >= @Today");
```

| Tín hiệu | Cách tính | Dùng cho |
|---|---|---|
| `inProgress` | Số ID trong `activeIds` | Workload (mục 11), bản tin sáng |
| `stuckTasks` | Task Active có số ngày làm việc kể từ `ActivatedDate` (nếu thiếu thì `StateChangeDate`) ≥ `stuckTaskMinBusinessDays` | Task kẹt → khoá tập trung (7.4) |
| `taskDone` | ID có trong `doneTodayIds` mà lần poll trước chưa có | Task xong: nhảy tưng +2 (7.5) |
| `sprint` | `GetCurrentIterationAsync` → `attributes.startDate/finishDate` → số ngày còn lại; done/total = đếm task trong iteration | Thanh sprint trong thẻ Task kẹt và dashboard tuần |
| `avgInProgress` | Trung bình `inProgress` lúc 17:00 mỗi ngày trong lịch sử local; khi chưa đủ 5 ngày dữ liệu thì dùng `AvgInProgressFallback` | Workload spike |

Lần poll đầu tiên sau khi mở app chỉ **ghi nhận** `doneTodayIds`, không bắn *Task xong*. Làm vậy để mở app giữa ngày không bị một loạt Milo nhảy tưng.

### 6.3 PAT

```csharp
// Minditful.Integrations/Secrets/PatStore.cs  — DPAPI, chỉ user hiện tại giải mã được
public static class PatStore
{
    static string PathFor(string env) => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Minditful", env, "pat.bin");

    public static void Save(string env, string pat)
    {
        var p = PathFor(env); Directory.CreateDirectory(System.IO.Path.GetDirectoryName(p)!);
        File.WriteAllBytes(p, ProtectedData.Protect(Encoding.UTF8.GetBytes(pat), null, DataProtectionScope.CurrentUser));
    }

    public static string? Load(string env)
    {
        var p = PathFor(env);
        return File.Exists(p) ? Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(p), null, DataProtectionScope.CurrentUser)) : null;
    }
}
```

(`ProtectedData` nằm trong package `System.Security.Cryptography.ProtectedData`, chỉ chạy trên Windows.)

---

## 7. Checklist test sandbox: dựng lại từng hành vi

Chạy app với `--env Sandbox`, đăng nhập thulu@, dán PAT (bảng điều khiển → trang *Kết nối*). Trang **Thử tình huống** có công cụ test; trang **Bộ não Milo** để theo dõi trạng thái, lý do im lặng, lời nhắc đang chờ và nhật ký, giống cột phải của prototype.

| # | Làm gì (ai làm) | Milo phải… | Mục |
|---|---|---|---|
| 1 | Khoá máy rồi mở lại lần đầu trong ngày *(Thư)*. Muốn test lại thì bấm **Reset ngày** ở trang *Thử tình huống* của bảng điều khiển | Bám mép → leo lên → Hello → bản tin: số họp, số email chưa đọc, số task đang dở | 6.1 |
| 2 | Tạo Teams meeting mời Thư, bắt đầu sau **7 phút**, đính kèm 1 file .pptx *(Duy)* | Khi còn 5 phút: thẻ Sắp họp, vai trò Bắt buộc, có nút Mở slide / Tham gia | 7.1 |
| 3 | Thư bấm **Tham gia**, vào họp và share màn hình | Milo thụt xuống. Presence thành `InACall`/`Presenting`. Milo và chóp đuôi đều ẩn, và **không lộ trên màn hình share** | 4 |
| 4 | Tạo **3 cuộc họp 15 phút liền nhau**, bắt đầu sau 20 phút *(Duy)* | Trước giờ họp: thẻ **Lịch kín** → bấm Giữ chỗ → Outlook của Thư có sự kiện "Nghỉ cùng Milo" (tentative) | 7.2 |
| 5 | Thư tham gia cả 3 cuộc, rời cuộc cuối | Chờ 2 phút → **JumpIn** → "Họp liên tục" → Đồng ý → thở 4-4-4 | 8, B12–B14 |
| 6 | Gửi Thư 2 email có dấu "?" *(Quân)*, Thư không trả lời | Ở khoảng trống tiếp theo: thẻ **Email chờ** liệt kê 2 email | 7.3 |
| 7 | Thư trả lời 1 email, chờ 5 phút | Lần nhắc sau chỉ còn 1 email | 7.3 |
| 8 | Để 1 task ở **Active**, lịch trống ≥ 45 phút | Thẻ **Task kẹt** → Khoá → có sự kiện "Tập trung: #id" (busy) + Teams chuyển **Do not disturb** + Milo im lặng tới hết khối | 7.4 |
| 9 | Kéo task sang **Closed** *(Thư hoặc Duy)* | Trong ≤ 3 phút: Milo ló lên nhảy tưng "Xong #id rồi!" | 7.5 |
| 10 | Thư tự đặt Teams **Do not disturb** | Cổng *Không làm phiền* sáng; lời nhắc mới thành chấm chờ | 4 |
| 11 | Làm liên tục 20 phút không rời máy (ngưỡng sandbox) | Thẻ **Làm liền** | 8 |
| 12 | Đặt giờ kết thúc khung làm việc = giờ hiện tại + 2 phút | Thẻ **Tan tầm** → Thêm 30 phút → nhắc lại → Về thôi → chạy ra xe | 6.2 |
| 13 | Đăng xuất Teams, chờ 10 phút | Log ghi "đang dùng lịch để đoán cuộc họp"; cổng họp vẫn đúng giờ theo lịch | 5.2 |
| 14 | Xoá PAT (bảng điều khiển → *Kết nối* → *Xoá PAT*) | Các tính năng Boards tắt, không popup lỗi; dashboard có dòng "Chưa kết nối Azure Boards" | 14 |

**Tính năng mở rộng** (bật bằng mục `Wellbeing`; ở chế độ test Sandbox tự rút ngắn: nhắc uống nước mỗi 5 phút, giữ giờ tập trung từ 30 phút trống):

| # | Làm gì | Milo phải… |
|---|---|---|
| 15 | Mở máy, chờ 20 phút, lịch còn khoảng trống ≥ 30 phút (hoặc *Thử tình huống* → *Giữ giờ tập trung*) | Thẻ **Giữ giờ tập trung** → Giữ → Outlook có "Tập trung · Milo giữ chỗ" (busy). Tới giờ: Teams chuyển **Do not disturb**, Milo im lặng tới hết khối |
| 16 | *Thử tình huống* → *Báo cáo tuần* (cần app đã chạy ít nhất 1 ngày tuần trước) | Thẻ "Tuần trước của bạn" + 1 mẹo → *Xem chùm nho* mở dashboard trang Tuần |
| 17 | Ngồi máy liên tục 5 phút (ngưỡng test) | Milo ló lên 5 giây "Uống ngụm nước nha!", lần sau là "Đứng dậy vươn vai 1 phút…" |
| 18 | Trong cuộc họp Teams, bấm **Share / Present** (hoặc công tắc *Đang trình chiếu* ở *Giả vờ bạn đang…*) | Milo trốn hẳn, **cả chóp đuôi**; thôi trình chiếu thì chóp đuôi hiện lại |
| 19 | Tạo 3 cuộc họp liền nhau cho **ngày mai**, rồi bấm *Giờ về = bây giờ + 2'* | Thẻ Tan tầm có dòng "Mai … có 3 cuộc họp liền" → *Giữ 10' nghỉ* → Outlook ngày mai có "Nghỉ cùng Milo" (tentative) |
| 20 | Trên thẻ Tan tầm bấm *Mệt* | Nút Mệt tô đậm, điểm giảm 6; tuần sau dashboard chi tiết có dòng "Bạn tự thấy" |
| 21 | Bảng điều khiển → *Milo của bạn* → *Tủ đồ* | Hiện chuỗi ngày về đúng giờ; món chưa mở khoá bị mờ. Về đúng giờ 3 ngày liền thì sáng thứ 4 thẻ Chào sáng báo "Milo được tặng khăn quàng" |
| 22 | Bấm chóp đuôi → *Chi tiết* | Bảng nhỏ trên đầu Milo: dòng thời gian, Office Vibe, cuộc họp sắp tới; tab Tuần có 7 quả nho |
| 23 | *Tổng quan* → *Chế độ Sandbox* → **Chạy như Production** | Trang *Thử tình huống* biến mất, nhãn thanh bên thành "SANDBOX · NHƯ PRODUCTION", Bộ não ghi "ngưỡng chuẩn"; ngồi 25 phút không còn thẻ Làm liền. Chuyển lại **Chế độ test** thì mọi thứ quay về |

**Dọn dẹp sau khi test:** trong Outlook, tìm và xoá các sự kiện có category **Milo** (gồm cả "Tập trung · Milo giữ chỗ" và "Nghỉ cùng Milo" ngày mai). Azure Boards thì để nguyên, sprint sau dùng tiếp.

---

## 8. Lỗi thường gặp

| Hiện tượng | Nguyên nhân | Cách sửa |
|---|---|---|
| `AADSTS50011` redirect URI mismatch | App chưa có `http://localhost` ở loại *Mobile and desktop* | Entra → Authentication → thêm đúng URI, đúng loại nền tảng |
| `AADSTS7000218` cần client secret | Chưa bật public client | Authentication → Settings → **Allow public client flows = Yes** |
| `AADSTS65001` chưa consent | Thiếu admin consent cho quyền mới thêm | API permissions → **Grant admin consent for Mindiful** |
| `AADSTS50020` user không thuộc tenant | Đăng nhập nhầm account Bosch/cá nhân | Đăng xuất (Cài đặt → Đăng xuất Microsoft) → đăng nhập thulu@ |
| Presence luôn `Offline` | thulu@ chưa mở Teams | Mở Teams desktop/web bằng thulu@ |
| `setUserPreferredPresence` chạy nhưng DND không lên | Teams không mở | Mở Teams; hoặc chấp nhận Milo chỉ tạo sự kiện lịch |
| calendarView lệch 7 tiếng | Thiếu header `Prefer: outlook.timezone` | Dùng `GraphClient` ở mục 5.1 |
| `403` khi gọi `/me/messages` | Chưa có `Mail.Read` hoặc chưa consent | Thêm quyền → Grant admin consent → đăng nhập lại (Cài đặt → Đăng xuất) |
| DevOps trả **203** + HTML | PAT sai, hết hạn, hoặc tạo nhầm org | Tạo PAT mới cho đúng org `mindiful-sandbox` |
| WIQL trả 0 task | PAT do người khác tạo (`@Me` sai người), hoặc task không *Assigned To* Thu Ly | PAT phải do **thulu@** tạo; kiểm tra Assigned To |
| Không lấy được sprint | Team chưa chọn `Sprint 1` | Project settings → Team configuration → Iterations → Select iteration(s) |
| `429 Too Many Requests` | Poll quá dày | Giữ đúng chu kỳ ở `Polling`; `GraphClient` đã tự chờ theo `Retry-After` |

---

## 9. Từ Sandbox lên Prod (Bosch)

Chỉ đổi **cấu hình**, code giữ nguyên:

| | Sandbox | Prod |
|---|---|---|
| TenantId | `<Mindiful>` | `0ae51e19-07c8-4e0e-bb9d-648ee58410f4` |
| ClientId | `<Minditful Milo (Sandbox)>` | `55b3153f-ddfd-4120-9408-2f9e874fe660` |
| Scopes | 6 quyền | Bắt đầu với `User.Read`, `Presence.Read`, `Calendars.Read`. Các quyền còn lại thêm khi IT duyệt |
| Tính năng thiếu quyền | — | App tự hạ cấp: Giữ chỗ → "Nhắc tôi lúc đó"; Email chờ tắt; khoá tập trung chỉ nhắc, không bật DND |
| Azure DevOps | `mindiful-sandbox / Milo-Sandbox` | Org/project Bosch; PAT chỉ cần **Work Items: Read** + **Project and Team: Read** |
| `BehaviorOverrides` | Có (ngưỡng rút gọn, chỉ ở chế độ test) | **Không có**, dùng ngưỡng chuẩn |
| Chế độ test (trang *Thử tình huống*) | Có, bật/tắt được | **Không có** |

Trước khi chạy Prod, kiểm tra 2 việc: org Azure DevOps của Bosch có cho tạo PAT không, và máy công ty có chặn đăng nhập MSAL qua trình duyệt hệ thống với `http://localhost` không. Nếu bị chặn, chuyển sang WAM broker (package `Microsoft.Identity.Client.Broker`, thêm redirect URI `ms-appx-web://microsoft.aad.brokerplugin/<ClientId>`).
