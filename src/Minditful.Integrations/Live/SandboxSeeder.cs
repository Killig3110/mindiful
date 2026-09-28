using Minditful.Integrations.AzureDevOps;
using Minditful.Integrations.Graph;

namespace Minditful.Integrations.Live;

/// <summary>
/// Dựng lại "ngày mẫu" của prototype trên tài khoản cá nhân, lệch theo giờ hiện tại để thử được ngay:
/// 1 cuộc họp sau 7 phút (Sắp họp), chuỗi 3 cuộc liền nhau (Lịch kín → Họp liên tục), 6 work item (Task kẹt),
/// 3 email hỏi thẳng bạn (Email chờ).
/// </summary>
public sealed class SandboxSeeder(ConnectionOptions conn, MicrosoftAuth auth, GraphClient? graph, AzureBoardsClient? boards)
{
    private static readonly (string Title, int DaysAgo)[] Tasks =
    [
        ("Refactor login flow", 4), ("Cập nhật API docs", 3), ("Fix bug đăng nhập SSO", 1),
        ("Unit test module auth", 2), ("Tối ưu query báo cáo", 1), ("Review PR #212", 0),
    ];

    private static readonly string[] Mails = ["Chốt scope sprint 43?", "Bug #4830 có cần fix gấp?", "Review giúp PR #212?"];

    public async Task<IReadOnlyList<string>> SeedAsync(DateTime now, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var done = new List<string>();
        void Report(string s)
        {
            done.Add(s);
            progress?.Report(s);
        }

        if (graph is not null && auth.IsConfigured)
        {
            var online = conn.RequireOnlineMeeting;
            var t0 = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(7);
            await graph.CreateMeetingAsync("Sprint Planning · Milo sandbox", t0, t0.AddMinutes(30), online, ct);
            Report($"Lịch: Sprint Planning {t0:HH:mm} (Sắp họp sẽ nhắc lúc {t0.AddMinutes(-5):HH:mm})");

            var c0 = t0.AddMinutes(45);
            string[] chain = ["Design sync", "1:1 với lead", "Demo review"];
            for (var i = 0; i < chain.Length; i++)
                await graph.CreateMeetingAsync(chain[i] + " · Milo sandbox", c0.AddMinutes(30 * i), c0.AddMinutes(30 * (i + 1)), online, ct);
            Report($"Lịch: chuỗi 3 cuộc họp liền {c0:HH:mm}–{c0.AddMinutes(90):HH:mm} (Lịch kín, rồi Họp liên tục)");
            if ((now.Hour < 12) != (c0.Hour < 12))
                Report("Lưu ý: chuỗi họp rơi sang buổi khác nên Lịch kín sẽ không bật (luật \"cùng buổi\")");

            if (auth.Has("Mail.Send"))
            {
                var (me, _) = await graph.MeAsync(ct);
                foreach (var s in Mails) await graph.SendMailToSelfAsync(me, s, "Email mẫu của Minditful sandbox. Trả lời email này để Milo thôi nhắc?", ct);
                Report($"Email: đã gửi {Mails.Length} email có dấu \"?\" vào hộp thư của bạn");
            }
            else Report("Email: bỏ qua (chưa có quyền Mail.Send)");
        }
        else Report("Graph: chưa cấu hình ClientId → bỏ qua lịch/email");

        if (boards is { IsConfigured: true })
        {
            var who = await boards.MyUniqueNameAsync(ct);
            var it = await boards.CurrentIterationAsync(ct);
            foreach (var (title, days) in Tasks)
            {
                var id = await boards.CreateActiveTaskAsync(title, who, days, it?.Path, ct);
                Report($"Azure Boards: #{id} {title} (dở {days} ngày)");
            }
        }
        else Report("Azure Boards: chưa cấu hình → bỏ qua work item");
        return done;
    }
}

/// <summary>Kết quả đọc presence. <c>null</c> ở InCall = không dùng được presence → đoán cuộc họp từ lịch.</summary>
public sealed record PresenceReading(bool? InCall, bool Dnd, bool Presenting, string Activity);

/// <summary>Đọc /me/presence định kỳ: cổng Đang họp và Không làm phiền (mục 4; docs/KET-NOI-SANDBOX.md mục 5.2).</summary>
public sealed class PresenceWatcher(GraphClient graph, MicrosoftAuth auth)
{
    private static readonly string[] CallActivities = ["InACall", "InAConferenceCall", "InAMeeting", "Presenting"];
    private static readonly string[] DndActivities = ["DoNotDisturb", "UrgentInterruptionsOnly"];
    private static readonly string[] Unknown = ["Offline", "PresenceUnknown", ""];

    public bool CanRead => auth.Has("Presence.Read") || auth.Has("Presence.ReadWrite");

    public async Task<PresenceReading> PollAsync(CancellationToken ct = default)
    {
        if (!CanRead) return new(null, false, false, "");
        var p = await graph.PresenceAsync(ct);
        // Offline thường là do Teams chưa mở → không kết luận được, để engine dùng lịch
        if (Unknown.Contains(p.Activity, StringComparer.OrdinalIgnoreCase)) return new(null, false, false, p.Activity);
        var inCall = CallActivities.Contains(p.Activity, StringComparer.OrdinalIgnoreCase);
        var dnd = !inCall && (DndActivities.Contains(p.Activity, StringComparer.OrdinalIgnoreCase)
                              || string.Equals(p.Availability, "DoNotDisturb", StringComparison.OrdinalIgnoreCase));
        return new(inCall, dnd, string.Equals(p.Activity, "Presenting", StringComparison.OrdinalIgnoreCase), p.Activity);
    }
}
