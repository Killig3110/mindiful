using Microsoft.Identity.Client;
using Minditful.Core.Engine;
using Minditful.Integrations.AzureDevOps;
using Minditful.Integrations.Graph;

namespace Minditful.Integrations.Live;

/// <summary>
/// Gom dữ liệu Graph + Azure Boards thành <see cref="WorkSnapshot"/> cho Rule Engine.
/// Thiếu quyền hay mất mạng thì chỉ tắt đúng phần đó và ghi 1 dòng vào dashboard (mục 14).
/// </summary>
public sealed class LiveWorkDataProvider(
    ConnectionOptions conn, WorkDayOptions workDay, MicrosoftAuth auth, GraphClient? graph, AzureBoardsClient? boards, DayHistoryStore history)
{
    private static readonly string[] Palette = ["#5471B0", "#7FA65A", "#B85A34", "#7261B0", "#2B7A4B", "#0B4F8A", "#C07A2C"];
    private readonly Dictionary<string, string?> _attachmentCache = [];
    private string? _me;

    public async Task<WorkSnapshot> LoadAsync(DateTime now, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(now);
        var notes = new List<string>();
        var calendar = new List<CalendarEvent>();
        TomorrowInfo? tomorrow = null;
        var mails = new List<MailItem>();
        var unread = 0;
        var mailOk = false;
        var tasks = new List<WorkTask>();
        var completed = new List<CompletedTask>();
        SprintInfo? sprint = null;
        var boardsOk = false;

        if (graph is not null && auth.IsConfigured)
        {
            try
            {
                _me ??= (await graph.MeAsync(ct)).Mail;
                var dayStart = now.Date;
                var events = await graph.CalendarViewAsync(dayStart, dayStart.AddDays(2), ct);
                foreach (var e in events.Where(Relevant))
                {
                    if (e.Start.Date == dayStart) calendar.Add(await MapEventAsync(e, dayStart, ct));
                    else if (tomorrow is null && e.Start.Date == dayStart.AddDays(1)) tomorrow = new TomorrowInfo(e.Start.ToString("H:mm"), e.Subject);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                notes.Add("Lịch: " + Describe(ex));
            }

            if (auth.Has("Mail.Read"))
            {
                try
                {
                    (mails, unread) = await LoadMailAsync(now, ct);
                    mailOk = true;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    notes.Add("Email: " + Describe(ex));
                }
            }
            else if (auth.GrantedScopes.Count > 0) notes.Add("Chưa có quyền Mail.Read → tắt Email chờ");
        }
        else notes.Add("Chưa đăng nhập Microsoft → chưa có lịch và email");

        if (boards is { IsConfigured: true })
        {
            try
            {
                var active = await boards.MyActiveAsync(ct);
                tasks = active.Select(b => new WorkTask
                {
                    Id = b.Id.ToString(), Title = b.Title, Url = b.Url,
                    Days = b.StateChanged is { } sc ? BusinessDays.Between(sc, now) : 0,
                }).ToList();
                completed = (await boards.MyCompletedTodayAsync(ct)).Select(b => new CompletedTask(b.Id.ToString(), b.Title)).ToList();
                if (await boards.CurrentIterationAsync(ct) is { } it)
                {
                    var (done, total) = await boards.IterationProgressAsync(it, ct);
                    sprint = new SprintInfo(it.Name, done, total, it.Finish is { } f ? BusinessDays.Between(now, f.ToLocalTime()) : 0);
                }
                boardsOk = true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                notes.Add("Azure Boards: " + Describe(ex));
            }
        }

        return new WorkSnapshot
        {
            Calendar = calendar,
            Tomorrow = tomorrow,
            Emails = mails,
            Unread = unread,
            Tasks = tasks,
            CompletedToday = completed,
            Sprint = sprint,
            AvgInProgress = history.AvgInProgress(today) ?? workDay.AvgInProgressBaseline,
            Week = history.Week(today),
            YesterdayScore = history.Yesterday(today),
            MailAvailable = mailOk,
            BoardsAvailable = boardsOk,
            StatusNote = notes.Count > 0 ? string.Join(" · ", notes) : null,
        };
    }

    private static bool Relevant(GraphEvent e) =>
        !e.IsAllDay && !e.IsCancelled && !string.Equals(e.ShowAs, "free", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(e.MyResponse, "declined", StringComparison.OrdinalIgnoreCase)
        && !e.Categories.Contains("Milo");

    private async Task<CalendarEvent> MapEventAsync(GraphEvent e, DateTime dayStart, CancellationToken ct)
    {
        var mine = e.Attendees.FirstOrDefault(a => string.Equals(a.Address, _me, StringComparison.OrdinalIgnoreCase));
        // Meeting Classifier đơn giản: người tổ chức = trình bày, được mời tuỳ chọn = tuỳ chọn.
        var role = e.IsOrganizer ? "Trình bày" : string.Equals(mine.Type, "optional", StringComparison.OrdinalIgnoreCase) ? "Tuỳ chọn" : "Bắt buộc";
        string? attach = null;
        if (e.HasAttachments && role == "Trình bày" && graph is not null)
        {
            if (!_attachmentCache.TryGetValue(e.Id, out attach))
            {
                try { attach = await graph.FirstAttachmentNameAsync(e.Id, ct); }
                catch (GraphException) { attach = null; }
                _attachmentCache[e.Id] = attach;
            }
        }
        var others = e.Attendees.Where(a => !string.Equals(a.Address, _me, StringComparison.OrdinalIgnoreCase)).ToList();
        var people = others.Take(others.Count > 3 ? 2 : 3).Select(a => new Person(Initials(a.Name), ColorFor(a.Address))).ToList();
        if (others.Count > 3) people.Add(new Person("+" + (others.Count - 2), "#B85A34"));
        var start = Math.Max(0, (e.Start - dayStart).TotalSeconds);
        var end = Math.Min(86400, (e.End - dayStart).TotalSeconds);
        return new CalendarEvent
        {
            Id = e.Id, Subject = e.Subject, Start = start, End = end, Role = role, Attachment = attach, People = people,
            IsOnline = e.IsOnlineMeeting || !conn.RequireOnlineMeeting, JoinUrl = e.JoinUrl, WebLink = e.WebLink,
        };
    }

    private async Task<(List<MailItem>, int)> LoadMailAsync(DateTime now, CancellationToken ct)
    {
        var since = now.ToUniversalTime().AddDays(-10);
        var unread = await graph!.UnreadCountAsync(ct);
        var inbox = await graph.InboxSinceAsync(since, ct);
        var replied = await graph.RepliedConversationsSinceAsync(since, ct);
        var list = inbox
            .Where(m => m.To.Any(t => string.Equals(t, _me, StringComparison.OrdinalIgnoreCase)))
            .Where(m => conn.IncludeSelfSentMail || !string.Equals(m.FromAddress, _me, StringComparison.OrdinalIgnoreCase))
            .Where(m => m.Flagged || m.Subject.Contains('?') || m.BodyPreview.Contains('?'))
            .Where(m => !replied.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId).Select(g => g.First())
            .Select(m => new MailItem
            {
                Id = m.Id, From = Initials(m.FromName), Color = ColorFor(m.FromAddress), Subject = m.Subject, WebLink = m.WebLink,
                // Days ≥ 1 là điều kiện "đang chờ" của engine; MailMinWaitDays = 0 để Sandbox thử ngay.
                Days = Math.Max(BusinessDays.Between(m.Received.ToLocalTime(), now), conn.MailMinWaitDays == 0 ? 1 : 0),
            })
            .Where(m => m.Days >= Math.Max(1, conn.MailMinWaitDays))
            .ToList();
        return (list, unread);
    }

    public static string Initials(string name)
    {
        var parts = name.Split([' ', '.', '_', '-', '@'], StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant(),
            _ => (char.ToUpperInvariant(parts[0][0]).ToString() + char.ToUpperInvariant(parts[^1][0])),
        };
    }

    private static string ColorFor(string key) => Palette[(int)((uint)StableHash(key) % Palette.Length)];

    private static int StableHash(string s)
    {
        unchecked
        {
            var h = 17;
            foreach (var c in s.ToLowerInvariant()) h = h * 31 + c;
            return h;
        }
    }

    public static string Describe(Exception ex) => ex switch
    {
        MsalUiRequiredException => "cần đăng nhập lại",
        HttpRequestException { Message: var m } => m,
        GraphException { Status: System.Net.HttpStatusCode.Forbidden } => "thiếu quyền (chờ admin consent?)",
        _ => ex.Message,
    };
}
