using Microsoft.Identity.Client;
using Minditful.Core.Engine;
using Minditful.Integrations.AzureDevOps;
using Minditful.Integrations.Graph;
using Minditful.Integrations.Storage;

namespace Minditful.Integrations.Live;

/// <summary>
/// Gom dữ liệu Graph + Azure Boards thành <see cref="WorkSnapshot"/> cho Rule Engine.
/// Thiếu quyền hay mất mạng thì chỉ tắt đúng phần đó và ghi 1 dòng vào dashboard (mục 14).
/// </summary>
public sealed class LiveWorkDataProvider(
    ConnectionOptions conn, WorkDayOptions workDay, MicrosoftAuth auth, GraphClient? graph, AzureBoardsClient? boards, LocalStore history)
{
    private static readonly string[] Palette = ["#5471B0", "#7FA65A", "#B85A34", "#7261B0", "#2B7A4B", "#0B4F8A", "#C07A2C"];
    private readonly Dictionary<string, string?> _attachmentCache = [];
    private string? _me;

    // Mỗi nguồn có chu kỳ riêng (lịch 2', mail 5', Boards 3' — docs/KET-NOI-SANDBOX.md); giữa 2 lần đọc dùng lại kết quả cũ.
    private DateTime _calAt = DateTime.MinValue, _mailAt = DateTime.MinValue, _boardsAt = DateTime.MinValue;
    private List<CalendarEvent> _calendar = [];
    private TomorrowInfo? _tomorrow;
    private List<CalendarEvent> _tomorrowCal = [];

    /// <summary>Thống kê tuần trước chụp lại trước khi tự xoá (khi chỉ giữ tuần hiện tại) để sáng thứ Hai vẫn có báo cáo tuần.</summary>
    public WeekStats? LastWeekFallback { get; set; }
    public bool WardrobeEnabled { get; set; } = true;
    private string? _calNote, _mailNote, _boardsNote;
    private List<MailItem> _mails = [];
    private int _unread;
    private bool _mailOk;
    private List<WorkTask> _tasks = [];
    private List<CompletedTask> _completed = [];
    private SprintInfo? _sprint;
    private bool _boardsOk;

    /// <param name="force">Đọc lại tất cả ngay (mở khoá máy, đăng nhập, bấm Làm mới).</param>
    public async Task<WorkSnapshot> LoadAsync(DateTime now, bool force = false, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(now);
        var poll = conn.Polling;
        bool Due(DateTime at, int seconds) => force || (now - at).TotalSeconds >= seconds || at.Date != now.Date;

        if (graph is not null && auth.IsConfigured)
        {
            if (Due(_calAt, poll.CalendarSeconds))
            {
                _calAt = now;
                try
                {
                    _me ??= (await graph.MeAsync(ct)).Mail;
                    var dayStart = now.Date;
                    var calendar = new List<CalendarEvent>();
                    var tomorrowCal = new List<CalendarEvent>();
                    TomorrowInfo? tomorrow = null;
                    foreach (var e in (await graph.CalendarViewAsync(dayStart, dayStart.AddDays(2), ct)).Where(Relevant))
                    {
                        if (e.Start.Date == dayStart) calendar.Add(await MapEventAsync(e, dayStart, ct));
                        else if (e.Start.Date == dayStart.AddDays(1))
                        {
                            tomorrow ??= new TomorrowInfo(e.Start.ToString("H:mm"), e.Subject);
                            // Ngày mai chỉ cần giờ để tìm chuỗi họp liền — không tải tệp đính kèm
                            var t0 = dayStart.AddDays(1);
                            tomorrowCal.Add(new CalendarEvent
                            {
                                Id = e.Id, Subject = e.Subject, Start = Math.Max(0, (e.Start - t0).TotalSeconds), End = Math.Min(86400, (e.End - t0).TotalSeconds),
                            });
                        }
                    }
                    (_calendar, _tomorrow, _tomorrowCal, _calNote) = (calendar, tomorrow, tomorrowCal, null);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _calNote = "Lịch: " + Describe(ex);
                }
            }

            if (!auth.Has("Mail.Read"))
            {
                _mailOk = false;
                _mailNote = auth.GrantedScopes.Count > 0 ? "Chưa có quyền Mail.Read → tắt Email chờ" : null;
            }
            else if (Due(_mailAt, poll.MailSeconds))
            {
                _mailAt = now;
                try
                {
                    (_mails, _unread) = await LoadMailAsync(now, ct);
                    (_mailOk, _mailNote) = (true, null);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _mailNote = "Email: " + Describe(ex);
                }
            }
        }
        else
        {
            (_calendar, _tomorrow, _tomorrowCal, _mails, _mailOk) = ([], null, [], [], false);
            _calNote = "Chưa đăng nhập Microsoft → chưa có lịch và email";
            _mailNote = null;
        }

        if (boards is not { IsConfigured: true })
        {
            (_tasks, _completed, _sprint, _boardsOk) = ([], [], null, false);
            _boardsNote = "Chưa kết nối Azure Boards";
        }
        else if (Due(_boardsAt, poll.BoardsSeconds))
        {
            _boardsAt = now;
            try
            {
                var active = await boards.MyActiveAsync(ct);
                _tasks = active.Select(b => new WorkTask
                {
                    Id = b.Id.ToString(), Title = b.Title, Url = b.Url,
                    Days = b.StateChanged is { } sc ? BusinessDays.Between(sc, now) : 0,
                }).ToList();
                _completed = (await boards.MyCompletedTodayAsync(ct)).Select(b => new CompletedTask(b.Id.ToString(), b.Title)).ToList();
                _sprint = null;
                if (await boards.CurrentIterationAsync(ct) is { } it)
                {
                    var (done, total) = await boards.IterationProgressAsync(it, ct);
                    _sprint = new SprintInfo(it.Name, done, total, it.Finish is { } f ? BusinessDays.Between(now, f.ToLocalTime()) : 0);
                }
                (_boardsOk, _boardsNote) = (true, null);
            }
            catch (InvalidOperationException)
            {
                (_tasks, _completed, _sprint, _boardsOk) = ([], [], null, false);
                _boardsNote = "Chưa kết nối Azure Boards (chưa có PAT)";
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _boardsOk = false;
                _boardsNote = "Azure Boards: " + Describe(ex);
            }
        }

        var notes = new[] { _calNote, _mailNote, _boardsNote }.Where(n => n is not null).ToList();
        return new WorkSnapshot
        {
            Calendar = _calendar,
            Tomorrow = _tomorrow,
            TomorrowCalendar = _tomorrowCal,
            Wardrobe = WardrobeEnabled ? history.Wardrobe(today) : null,
            Emails = _mails,
            Unread = _unread,
            Tasks = _tasks,
            CompletedToday = _completed,
            Sprint = _sprint,
            AvgInProgress = history.AvgInProgress(today) ?? conn.AvgInProgressFallback ?? workDay.AvgInProgressBaseline,
            Week = history.Week(today),
            YesterdayScore = history.Yesterday(today),
            ThisWeek = history.WeekStats(today),
            LastWeek = history.WeekStats(today.AddDays(-7)) ?? (LastWeekFallback?.From == LocalStore.WeekStart(today.AddDays(-7)) ? LastWeekFallback : null),
            MailAvailable = _mailOk,
            CanWriteCalendar = graph is null || auth.GrantedScopes.Count == 0 || auth.Has("Calendars.ReadWrite"),
            BoardsAvailable = _boardsOk,
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
        MsalServiceException { Message: var m } when m.Contains("AADSTS65001") || m.Contains("AADSTS90094") || m.Contains("AADSTS90095")
            => "tenant yêu cầu admin duyệt quyền của Milo (Grant admin consent trong Entra → API permissions)",
        MsalServiceException { Message: var m } when m.Contains("AADSTS50020") => "tài khoản không thuộc tenant này — đăng xuất rồi chọn đúng tài khoản",
        MsalServiceException { Message: var m } when m.Contains("AADSTS50011") => "redirect URI chưa khai http://localhost (loại Mobile and desktop)",
        MsalServiceException { Message: var m } when m.Contains("AADSTS7000218") => "chưa bật Allow public client flows",
        HttpRequestException { Message: var m } => m,
        GraphException { Status: System.Net.HttpStatusCode.Forbidden } => "thiếu quyền (chờ admin consent?)",
        _ => ex.Message,
    };
}
