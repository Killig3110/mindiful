using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Minditful.Integrations.Graph;

namespace Minditful.Integrations.AzureDevOps;

public sealed record BoardItem(int Id, string Title, string State, DateTime? StateChanged, double? Points, string Url);

public sealed record Iteration(string Id, string Name, string Path, DateTime? Start, DateTime? Finish);

/// <summary>Azure Boards qua REST 7.1: WIQL cho Task kẹt / Task xong / workload, iteration cho thanh sprint.</summary>
public sealed class AzureBoardsClient(AzureDevOpsOptions opt, HttpClient http, Func<CancellationToken, Task<AuthenticationHeaderValue>> authHeader)
{
    private const string Api = "api-version=7.1";

    private string OrgUrl => $"https://dev.azure.com/{Uri.EscapeDataString(opt.Organization)}";
    private string ProjectUrl => $"{OrgUrl}/{Uri.EscapeDataString(opt.Project)}";
    private string TeamUrl => $"{ProjectUrl}/{Uri.EscapeDataString(string.IsNullOrWhiteSpace(opt.Team) ? opt.Project + " Team" : opt.Team)}";

    public bool IsConfigured => opt.Enabled && !string.IsNullOrWhiteSpace(opt.Organization) && !string.IsNullOrWhiteSpace(opt.Project);

    /// <summary>Header xác thực: PAT (Basic) hoặc token Entra (Bearer).</summary>
    public static Func<CancellationToken, Task<AuthenticationHeaderValue>> PatAuth(Func<string?> pat) => _ =>
    {
        var p = pat() ?? throw new InvalidOperationException("Chưa có Personal Access Token cho Azure DevOps.");
        return Task.FromResult(new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + p))));
    };

    public static Func<CancellationToken, Task<AuthenticationHeaderValue>> EntraAuth(MicrosoftAuth auth) => async ct =>
        new AuthenticationHeaderValue("Bearer", await auth.AzureDevOpsTokenAsync(false, ct));

    private async Task<JsonNode> SendAsync(HttpMethod method, string url, HttpContent? content, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, url) { Content = content };
        req.Headers.Authorization = await authHeader(ct);
        using var res = await http.SendAsync(req, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
        {
            string msg = text;
            try { msg = JsonNode.Parse(text)?["message"]?.GetValue<string>() ?? text; }
            catch (JsonException) { }
            if (res.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.NonAuthoritativeInformation)
                msg = "PAT/token không hợp lệ hoặc hết hạn";
            throw new HttpRequestException($"Azure DevOps {(int)res.StatusCode}: {(msg.Length > 200 ? msg[..200] : msg)}");
        }
        // PAT sai có thể trả 203 + trang HTML đăng nhập
        if (res.Content.Headers.ContentType?.MediaType?.Contains("html") == true)
            throw new HttpRequestException("Azure DevOps: PAT/token không hợp lệ (nhận trang đăng nhập HTML)");
        return JsonNode.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text)!;
    }

    private static string Quote(string s) => "'" + s.Replace("'", "''") + "'";
    private static string InList(IEnumerable<string> xs) => "(" + string.Join(", ", xs.Select(Quote)) + ")";

    private async Task<IReadOnlyList<int>> WiqlAsync(string query, CancellationToken ct)
    {
        var n = await SendAsync(HttpMethod.Post, $"{ProjectUrl}/_apis/wit/wiql?{Api}&$top=200", JsonContent.Create(new { query }), ct);
        return n["workItems"]?.AsArray().Select(w => w!["id"]!.GetValue<int>()).ToList() ?? [];
    }

    private async Task<IReadOnlyList<BoardItem>> ItemsAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return [];
        var list = new List<BoardItem>();
        foreach (var chunk in ids.Chunk(200))
        {
            // Không truyền danh sách field: org nào không có StoryPoints/Effort (tuỳ process) sẽ báo TF51535.
            var n = await SendAsync(HttpMethod.Get, $"{OrgUrl}/_apis/wit/workitems?ids={string.Join(',', chunk)}&errorPolicy=omit&{Api}", null, ct);
            foreach (var w in n["value"]!.AsArray())
            {
                if (w?["fields"] is not { } f) continue;
                var changed = f["Microsoft.VSTS.Common.StateChangeDate"] ?? f["Microsoft.VSTS.Common.ActivatedDate"];
                var pts = f["Microsoft.VSTS.Scheduling.StoryPoints"] ?? f["Microsoft.VSTS.Scheduling.Effort"];
                var id = w!["id"]!.GetValue<int>();
                list.Add(new BoardItem(id, f["System.Title"]?.GetValue<string>() ?? "", f["System.State"]?.GetValue<string>() ?? "",
                    changed is null ? null : DateTime.Parse(changed.GetValue<string>(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).ToLocalTime(),
                    pts?.GetValue<double>(), $"{ProjectUrl}/_workitems/edit/{id}"));
            }
        }
        return list;
    }

    /// <summary>Work item của bạn đang Active/In Progress.</summary>
    public async Task<IReadOnlyList<BoardItem>> MyActiveAsync(CancellationToken ct = default)
    {
        var q = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = @project AND [System.AssignedTo] = @Me " +
                $"AND [System.State] IN {InList(opt.ActiveStates)} AND [System.WorkItemType] IN {InList(opt.WorkItemTypes)} ORDER BY [System.ChangedDate] DESC";
        return await ItemsAsync(await WiqlAsync(q, ct), ct);
    }

    /// <summary>Work item của bạn chuyển sang Done hôm nay (Task xong, mục 7.5).</summary>
    public async Task<IReadOnlyList<BoardItem>> MyCompletedTodayAsync(CancellationToken ct = default)
    {
        var q = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = @project AND [System.AssignedTo] = @Me " +
                $"AND [System.State] IN {InList(opt.DoneStates)} AND [System.ChangedDate] >= @Today";
        return await ItemsAsync(await WiqlAsync(q, ct), ct);
    }

    public async Task<Iteration?> CurrentIterationAsync(CancellationToken ct = default)
    {
        var n = await SendAsync(HttpMethod.Get, $"{TeamUrl}/_apis/work/teamsettings/iterations?$timeframe=current&{Api}", null, ct);
        var it = n["value"]?.AsArray().FirstOrDefault();
        if (it is null) return null;
        DateTime? D(string k) => it["attributes"]?[k]?.GetValue<string>() is { } s ? DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal) : null;
        return new Iteration(it["id"]!.GetValue<string>(), it["name"]?.GetValue<string>() ?? "", it["path"]?.GetValue<string>() ?? "", D("startDate"), D("finishDate"));
    }

    /// <summary>(điểm xong, tổng điểm). Không có story point thì đếm số item.</summary>
    public async Task<(double Done, double Total)> IterationProgressAsync(Iteration it, CancellationToken ct = default)
    {
        var n = await SendAsync(HttpMethod.Get, $"{TeamUrl}/_apis/work/teamsettings/iterations/{it.Id}/workitems?{Api}", null, ct);
        var ids = n["workItemRelations"]?.AsArray().Select(r => r?["target"]?["id"]?.GetValue<int>() ?? 0).Where(i => i > 0).Distinct().ToList() ?? [];
        var items = await ItemsAsync(ids, ct);
        var withPts = items.Where(i => i.Points is > 0).ToList();
        bool IsDone(BoardItem i) => opt.DoneStates.Contains(i.State, StringComparer.OrdinalIgnoreCase);
        return withPts.Count > 0
            ? (withPts.Where(IsDone).Sum(i => i.Points!.Value), withPts.Sum(i => i.Points!.Value))
            : (items.Count(IsDone), items.Count);
    }

    // ---------- Seeder cho Sandbox ----------
    public async Task<string> MyUniqueNameAsync(CancellationToken ct = default)
    {
        var n = await SendAsync(HttpMethod.Get, $"{OrgUrl}/_apis/connectionData", null, ct);
        var u = n["authenticatedUser"];
        return u?["properties"]?["Account"]?["$value"]?.GetValue<string>() ?? u?["providerDisplayName"]?.GetValue<string>() ?? "";
    }

    /// <summary>
    /// Tạo work item đang làm, lùi ngày chuyển trạng thái để có "Task kẹt".
    /// Cần quyền "Bypass rules on work item updates" (chủ org cá nhân mặc định có).
    /// </summary>
    public async Task<int> CreateActiveTaskAsync(string title, string assignee, int daysAgo, string? iterationPath, CancellationToken ct = default)
    {
        var when = DateTime.UtcNow.AddDays(-daysAgo).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        var ops = new List<object>
        {
            new { op = "add", path = "/fields/System.Title", value = title },
            new { op = "add", path = "/fields/System.AssignedTo", value = assignee },
            new { op = "add", path = "/fields/System.State", value = opt.SeedState },
            new { op = "add", path = "/fields/System.Tags", value = "Minditful; Sandbox" },
            new { op = "add", path = "/fields/Microsoft.VSTS.Common.StateChangeDate", value = when },
            new { op = "add", path = "/fields/Microsoft.VSTS.Common.ActivatedDate", value = when },
        };
        if (iterationPath is not null) ops.Add(new { op = "add", path = "/fields/System.IterationPath", value = iterationPath });
        var content = new StringContent(JsonSerializer.Serialize(ops), Encoding.UTF8, "application/json-patch+json");
        var n = await SendAsync(HttpMethod.Post, $"{ProjectUrl}/_apis/wit/workitems/$Task?bypassRules=true&{Api}", content, ct);
        return n["id"]!.GetValue<int>();
    }
}
