namespace Minditful.Core.Engine;

/// <summary>
/// Thẻ "Có mới": email mới gửi thẳng cho bạn, lời mời họp mới, task mới được giao. So với lần đọc trước (Graph / Azure Boards),
/// lần đọc đầu tiên sau khi mở app chỉ ghi nhận. Nhiều thứ tới liền nhau gộp vào 1 thẻ. Đang họp / tập trung thì thành chấm chờ.
/// </summary>
public sealed partial class MiloEngine
{
    private void DetectIncoming(WorkSnapshot snap)
    {
        var items = new List<IncomingItem>();
        items.AddRange(snap.RecentMail.Select(m => new IncomingItem("mail", "m:" + m.Id, m.Subject, m.FromName ?? m.From, m.WebLink)));
        items.AddRange(snap.Calendar.Where(e => !e.ByMe || Cfg.AlertOwnItems)
            .Select(e => new IncomingItem("meeting", "e:" + e.Id, e.Subject, e.Organizer ?? "", e.WebLink ?? e.JoinUrl, Tm.Hm(e.Start))));
        items.AddRange(snap.TomorrowCalendar.Where(e => !e.ByMe || Cfg.AlertOwnItems)
            .Select(e => new IncomingItem("meeting", "e:" + e.Id, e.Subject, e.Organizer ?? "", e.WebLink ?? e.JoinUrl, "mai " + Tm.Hm(e.Start))));
        items.AddRange(snap.Tasks.Select(t => new IncomingItem("task", "t:" + t.Id, t.Title, "#" + t.Id, t.Url)));
        var fresh = new List<IncomingItem>();
        foreach (var (kind, ok) in new[] { ("mail", snap.MailAvailable), ("meeting", snap.CalendarAvailable), ("task", snap.BoardsAvailable) })
        {
            if (!ok) continue;
            var ofKind = items.Where(i => i.Kind == kind);
            if (S.IncomingSeeded.Add(kind)) foreach (var i in ofKind) S.SeenIncoming.Add(i.Id); // lần đọc được đầu tiên: chỉ ghi nhận
            else fresh.AddRange(ofKind.Where(i => S.SeenIncoming.Add(i.Id)));
        }
        if (fresh.Count > 0 && Cfg.IncomingAlerts) AddIncoming(fresh);
    }

    /// <summary>Đưa các thứ vừa tới vào thẻ "Có mới" (gộp vào thẻ đang chờ hoặc đang hiện).</summary>
    public void AddIncoming(IReadOnlyList<IncomingItem> fresh)
    {
        if (fresh.Count == 0) return;
        Log("Có mới: " + string.Join(", ", fresh.Select(i => $"{KindName(i.Kind)} \"{i.Title}\"")), LogKind.Sig);
        if (S.Ep is { C: CaseId.Incoming } ep && ep.Phase is Phase.Enter or Phase.Show)
        {
            ep.Data.Incoming = [.. ep.Data.Incoming ?? [], .. fresh];
            ep.PhaseEnd = S.T + Dt(Catalog.Def(CaseId.Incoming).Timeout);
            ep.CardVer++;
            return;
        }
        var queued = S.Queue.FirstOrDefault(q => q.C == CaseId.Incoming)?.Data.Incoming ?? [];
        var parked = S.ParkedList.FirstOrDefault(p => p.C == CaseId.Incoming);
        if (parked is not null) S.ParkedList.Remove(parked); // thẻ cũ bị bỏ qua: gộp vào thẻ mới
        var list = queued.Concat(parked?.Item.Data.Incoming ?? []).Concat(fresh).DistinctBy(i => i.Id).TakeLast(10).ToList();
        Enqueue(CaseId.Incoming, "in-" + S.T, new CaseData { Incoming = list }, ttl: S.T + 3 * 3600);
    }

    public static string KindName(string kind) => kind switch { "mail" => "email", "meeting" => "cuộc họp", _ => "task" };

    /// <summary>Demo / Sandbox "Thử tình huống": giả lập lần lượt email mới, lời mời họp mới, task mới.</summary>
    public void SimulateIncoming(string? kind = null)
    {
        var n = S.DemoIncoming++;
        kind ??= (n % 3) switch { 0 => "mail", 1 => "meeting", _ => "task" };
        var at = Tm.Hm(Math.Ceiling((S.T + 3600) / 900) * 900);
        IncomingItem item = kind switch
        {
            "mail" => new("mail", "demo-m" + n, "Nhờ bạn review PR #512 trước 15:00 nhé", "Chị Linh (Team Lead)", null),
            "meeting" => new("meeting", "demo-e" + n, "Sync nhanh về bản build mới", "Anh Tuấn", null, at),
            _ => new("task", "demo-t" + n, "Sửa lỗi đăng nhập trên màn hình Settings", "#" + (4821 + n), null),
        };
        AddIncoming([item]);
    }
}
