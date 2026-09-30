using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Minditful.Integrations.Graph;

public sealed record GraphEvent(
    string Id, string Subject, DateTime Start, DateTime End, bool IsOnlineMeeting, string? JoinUrl, string? WebLink,
    bool IsOrganizer, bool HasAttachments, string ShowAs, bool IsAllDay, bool IsCancelled, string? MyResponse,
    IReadOnlyList<string> Categories, IReadOnlyList<(string Name, string Address, string Type)> Attendees, string? OrganizerName,
    string? BodyPreview = null);

public sealed record GraphMessage(
    string Id, string Subject, string FromName, string FromAddress, DateTime Received, string ConversationId,
    string BodyPreview, bool Flagged, string? WebLink, IReadOnlyList<string> To);

public sealed record GraphPresence(string Availability, string Activity);

/// <summary>Client Graph mỏng trên HttpClient: chỉ đúng các endpoint Milo cần (mục 14).</summary>
public sealed class GraphClient(MicrosoftAuth auth, HttpClient http)
{
    private const string Base = "https://graph.microsoft.com/v1.0";

    private static string TimeZoneId => TimeZoneInfo.Local.HasIanaId && TimeZoneInfo.TryConvertIanaIdToWindowsId(TimeZoneInfo.Local.Id, out var w)
        ? w : TimeZoneInfo.Local.Id;

    private async Task<HttpRequestMessage> RequestAsync(HttpMethod method, string path, CancellationToken ct)
    {
        var token = await auth.GraphTokenAsync(false, ct);
        var req = new HttpRequestMessage(method, path.StartsWith("http") ? path : Base + path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.TryAddWithoutValidation("Prefer", $"outlook.timezone=\"{TimeZoneId}\"");
        return req;
    }

    /// <summary>Gửi request; bị throttle (429) hay lỗi tạm (5xx) thì chờ theo Retry-After rồi thử lại, tối đa 3 lần.</summary>
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var req = await RequestAsync(method, path, ct);
            if (body is not null) req.Content = JsonContent.Create(body);
            var res = await http.SendAsync(req, ct);
            var transient = (int)res.StatusCode == 429 || (int)res.StatusCode >= 500;
            if (!transient || attempt >= 2) return res;
            var wait = res.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(5 * (attempt + 1));
            res.Dispose();
            await Task.Delay(wait, ct);
        }
    }

    private async Task<JsonNode> GetAsync(string path, CancellationToken ct)
    {
        using var res = await SendAsync(HttpMethod.Get, path, null, ct);
        await EnsureAsync(res, ct);
        return (await JsonNode.ParseAsync(await res.Content.ReadAsStreamAsync(ct), cancellationToken: ct))!;
    }

    private async Task<JsonNode?> PostAsync(string path, object? body, CancellationToken ct)
    {
        using var res = await SendAsync(HttpMethod.Post, path, body, ct);
        await EnsureAsync(res, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        return string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
    }

    private static async Task EnsureAsync(HttpResponseMessage res, CancellationToken ct)
    {
        if (res.IsSuccessStatusCode) return;
        var body = await res.Content.ReadAsStringAsync(ct);
        throw new GraphException(res.StatusCode, body);
    }

    private static string Iso(DateTime d) => d.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

    private static DateTime ParseDt(JsonNode? n) =>
        DateTime.Parse(n?["dateTime"]?.GetValue<string>() ?? "", CultureInfo.InvariantCulture, DateTimeStyles.None);

    // ---------- lịch ----------
    public async Task<IReadOnlyList<GraphEvent>> CalendarViewAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var path = $"/me/calendarView?startDateTime={Iso(from)}&endDateTime={Iso(to)}&$top=100&$orderby=start/dateTime" +
                   "&$select=id,subject,start,end,isOnlineMeeting,onlineMeeting,webLink,isOrganizer,hasAttachments,showAs,isAllDay,isCancelled,responseStatus,categories,attendees,organizer,bodyPreview";
        var list = new List<GraphEvent>();
        string? next = path;
        while (next is not null)
        {
            var root = await GetAsync(next, ct);
            foreach (var e in root["value"]!.AsArray())
            {
                if (e is null) continue;
                list.Add(new GraphEvent(
                    e["id"]!.GetValue<string>(),
                    e["subject"]?.GetValue<string>() ?? "(không tiêu đề)",
                    ParseDt(e["start"]), ParseDt(e["end"]),
                    e["isOnlineMeeting"]?.GetValue<bool>() ?? false,
                    e["onlineMeeting"]?["joinUrl"]?.GetValue<string>(),
                    e["webLink"]?.GetValue<string>(),
                    e["isOrganizer"]?.GetValue<bool>() ?? false,
                    e["hasAttachments"]?.GetValue<bool>() ?? false,
                    e["showAs"]?.GetValue<string>() ?? "busy",
                    e["isAllDay"]?.GetValue<bool>() ?? false,
                    e["isCancelled"]?.GetValue<bool>() ?? false,
                    e["responseStatus"]?["response"]?.GetValue<string>(),
                    e["categories"]?.AsArray().Select(c => c!.GetValue<string>()).ToList() ?? [],
                    e["attendees"]?.AsArray().Select(a => (
                        a?["emailAddress"]?["name"]?.GetValue<string>() ?? "?",
                        a?["emailAddress"]?["address"]?.GetValue<string>() ?? "",
                        a?["type"]?.GetValue<string>() ?? "required")).ToList() ?? [],
                    e["organizer"]?["emailAddress"]?["name"]?.GetValue<string>(),
                    e["bodyPreview"]?.GetValue<string>()));
            }
            next = root["@odata.nextLink"]?.GetValue<string>();
        }
        return list;
    }

    public async Task<string?> FirstAttachmentNameAsync(string eventId, CancellationToken ct = default)
    {
        var root = await GetAsync($"/me/events/{Uri.EscapeDataString(eventId)}/attachments?$select=name", ct);
        return root["value"]?.AsArray().FirstOrDefault()?["name"]?.GetValue<string>();
    }

    private static readonly HashSet<string> Executable = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".com", ".bat", ".cmd", ".ps1", ".psm1", ".vbs", ".vbe", ".js", ".jse", ".wsf", ".wsh", ".hta", ".msi", ".msp", ".scr", ".lnk", ".url", ".jar", ".reg", ".cpl", ".pif", ".appref-ms",
    };

    /// <summary>Trả về đường dẫn file tạm (fileAttachment) hoặc URL (referenceAttachment) để mở. File chạy được thì trả về null.</summary>
    public async Task<string?> DownloadFirstAttachmentAsync(string eventId, CancellationToken ct = default)
    {
        var root = await GetAsync($"/me/events/{Uri.EscapeDataString(eventId)}/attachments", ct);
        var a = root["value"]?.AsArray().FirstOrDefault();
        if (a is null) return null;
        var type = a["@odata.type"]?.GetValue<string>() ?? "";
        if (type.EndsWith("referenceAttachment")) return a["sourceUrl"]?.GetValue<string>();
        var bytes = a["contentBytes"]?.GetValue<string>();
        if (bytes is null) return null;
        var dir = Path.Combine(Path.GetTempPath(), "Minditful");
        Directory.CreateDirectory(dir);
        // Chỉ lấy tên file (tên kiểu "..\\..\\x" không được ghi ra ngoài thư mục tạm); không mở file chạy được
        var name = Path.GetFileName(a["name"]?.GetValue<string>() ?? "");
        if (name.Length == 0) name = "attachment";
        if (Executable.Contains(Path.GetExtension(name))) return null;
        var file = Path.Combine(dir, name);
        await File.WriteAllBytesAsync(file, Convert.FromBase64String(bytes), ct);
        return file;
    }

    /// <summary>POST /me/events. Milo gắn category "Milo" để lần đọc lịch sau bỏ qua sự kiện của chính mình.</summary>
    public async Task<string?> CreateEventAsync(string subject, DateTime start, DateTime end, string showAs, CancellationToken ct = default)
    {
        var tz = TimeZoneId;
        var body = new
        {
            subject,
            start = new { dateTime = Iso(start), timeZone = tz },
            end = new { dateTime = Iso(end), timeZone = tz },
            showAs,
            isReminderOn = false,
            categories = new[] { "Milo" },
            body = new { contentType = "text", content = "Milo giữ chỗ giúp bạn · Minditful" },
        };
        var n = await PostAsync("/me/events", body, ct);
        return n?["id"]?.GetValue<string>();
    }

    public async Task CreateMeetingAsync(string subject, DateTime start, DateTime end, bool online, CancellationToken ct = default)
    {
        var tz = TimeZoneId;
        object body = online
            ? new { subject, start = new { dateTime = Iso(start), timeZone = tz }, end = new { dateTime = Iso(end), timeZone = tz }, isOnlineMeeting = true, onlineMeetingProvider = "teamsForBusiness" }
            : new { subject, start = new { dateTime = Iso(start), timeZone = tz }, end = new { dateTime = Iso(end), timeZone = tz } };
        await PostAsync("/me/events", body, ct);
    }

    // ---------- mail ----------
    private string? _meId;

    public async Task<(string Mail, string Name)> MeAsync(CancellationToken ct = default)
    {
        var me = await GetAsync("/me?$select=id,mail,userPrincipalName,displayName", ct);
        _meId = me["id"]?.GetValue<string>();
        return (me["mail"]?.GetValue<string>() ?? me["userPrincipalName"]?.GetValue<string>() ?? "", me["displayName"]?.GetValue<string>() ?? "");
    }

    private async Task<string> MeIdAsync(CancellationToken ct)
    {
        if (_meId is null) await MeAsync(ct);
        return _meId ?? throw new InvalidOperationException("Không lấy được id người dùng từ /me");
    }

    public async Task<int> UnreadCountAsync(CancellationToken ct = default)
    {
        var f = await GetAsync("/me/mailFolders/inbox?$select=unreadItemCount", ct);
        return f["unreadItemCount"]?.GetValue<int>() ?? 0;
    }

    public async Task<IReadOnlyList<GraphMessage>> InboxSinceAsync(DateTime sinceUtc, CancellationToken ct = default)
    {
        var path = $"/me/mailFolders/inbox/messages?$filter=receivedDateTime ge {sinceUtc:yyyy-MM-ddTHH:mm:ssZ}&$orderby=receivedDateTime desc&$top=100" +
                   "&$select=id,subject,from,toRecipients,receivedDateTime,conversationId,bodyPreview,flag,webLink";
        var root = await GetAsync(path, ct);
        return root["value"]!.AsArray().Where(m => m is not null).Select(m => new GraphMessage(
            m!["id"]!.GetValue<string>(),
            m["subject"]?.GetValue<string>() ?? "",
            m["from"]?["emailAddress"]?["name"]?.GetValue<string>() ?? "?",
            m["from"]?["emailAddress"]?["address"]?.GetValue<string>() ?? "",
            DateTime.Parse(m["receivedDateTime"]!.GetValue<string>(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
            m["conversationId"]?.GetValue<string>() ?? "",
            m["bodyPreview"]?.GetValue<string>() ?? "",
            m["flag"]?["flagStatus"]?.GetValue<string>() == "flagged",
            m["webLink"]?.GetValue<string>(),
            m["toRecipients"]?.AsArray().Select(r => r?["emailAddress"]?["address"]?.GetValue<string>() ?? "").ToList() ?? [])).ToList();
    }

    public async Task<IReadOnlySet<string>> RepliedConversationsSinceAsync(DateTime sinceUtc, CancellationToken ct = default)
    {
        var path = $"/me/mailFolders/sentitems/messages?$filter=sentDateTime ge {sinceUtc:yyyy-MM-ddTHH:mm:ssZ}&$top=200&$select=conversationId";
        var root = await GetAsync(path, ct);
        return root["value"]!.AsArray().Select(m => m?["conversationId"]?.GetValue<string>() ?? "").ToHashSet();
    }

    public Task SendMailToSelfAsync(string me, string subject, string body, CancellationToken ct = default) =>
        PostAsync("/me/sendMail", new
        {
            message = new
            {
                subject,
                body = new { contentType = "text", content = body },
                toRecipients = new[] { new { emailAddress = new { address = me } } },
            },
            saveToSentItems = false,
        }, ct);

    // ---------- presence ----------
    public async Task<GraphPresence> PresenceAsync(CancellationToken ct = default)
    {
        var p = await GetAsync("/me/presence", ct);
        return new GraphPresence(p["availability"]?.GetValue<string>() ?? "", p["activity"]?.GetValue<string>() ?? "");
    }

    /// <summary>Chỉ có tác dụng khi người dùng đang mở Teams; hết hạn thì Teams tự về trạng thái cũ.</summary>
    public async Task SetDoNotDisturbAsync(TimeSpan duration, CancellationToken ct = default) =>
        await PostAsync($"/users/{await MeIdAsync(ct)}/presence/setUserPreferredPresence", new
        {
            availability = "DoNotDisturb",
            activity = "DoNotDisturb",
            expirationDuration = System.Xml.XmlConvert.ToString(duration),
        }, ct);

    public async Task ClearPreferredPresenceAsync(CancellationToken ct = default) =>
        await PostAsync($"/users/{await MeIdAsync(ct)}/presence/clearUserPreferredPresence", null, ct);
}

public sealed class GraphException(System.Net.HttpStatusCode status, string body)
    : Exception($"Graph {(int)status}: {Shorten(body)}")
{
    public System.Net.HttpStatusCode Status { get; } = status;

    private static string Shorten(string s)
    {
        try
        {
            var msg = JsonNode.Parse(s)?["error"]?["message"]?.GetValue<string>();
            if (msg is not null) return msg;
        }
        catch (JsonException) { }
        return s.Length > 200 ? s[..200] : s;
    }
}

