using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;
using Xunit;
using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Tests;

/// <summary>
/// Thẻ "Có mới": email mới / lời mời họp mới / task mới được giao hiện ngay khi app đọc thấy.
/// Lần đọc đầu chỉ ghi nhận; nhiều thứ gộp 1 thẻ; đang họp thì chờ; không báo cuộc họp chính mình tạo.
/// </summary>
public class IncomingTests
{
    private readonly List<MiloAction> _actions = [];
    private static readonly DateOnly Day = new(2026, 9, 29);

    private static void Until(MiloEngine e, Func<bool> done, double maxSec = 300)
    {
        var end = e.S.T + maxSec;
        while (!done())
        {
            Assert.True(e.S.T < end, $"Quá {maxSec}s lúc {Hm(e.S.T)}: " + string.Join(" | ", e.S.Log.TakeLast(5).Select(l => l.Text)));
            e.Advance(0.5, stopWhenBusy: false);
        }
    }

    private MiloEngine Start(WorkSnapshot? first = null, EngineConfig? cfg = null)
    {
        var e = new MiloEngine(cfg ?? new EngineConfig(), new WorkSnapshot(), null, Day, T("10:00"));
        e.SetAuto(false);
        e.ActionRequested += _actions.Add;
        e.SetLocked(false);
        Until(e, () => e.S.Ep is { C: CaseId.MorningHello, Phase: Phase.Show });
        e.UserReply("gotIt");
        Until(e, () => e.S.Ep is null && e.S.Visit is null);
        e.ApplySnapshot(first ?? new WorkSnapshot()); // lần đọc đầu: chỉ ghi nhận
        return e;
    }

    private static MailItem Mail(string id, string from, string subject) =>
        new() { Id = id, From = from[..2].ToUpperInvariant(), FromName = from, Subject = subject, WebLink = "https://outlook.office.com/mail/" + id };

    [Fact]
    public void Things_already_there_at_startup_are_not_announced()
    {
        var e = Start(new WorkSnapshot { RecentMail = [Mail("old", "Minh", "Báo cáo tuần")] });
        e.ApplySnapshot(new WorkSnapshot { RecentMail = [Mail("old", "Minh", "Báo cáo tuần")] });
        e.Advance(30, false);
        Assert.DoesNotContain(e.S.Queue, q => q.C == CaseId.Incoming);
        Assert.Null(e.S.Ep);
    }

    [Fact]
    public void A_source_that_was_not_readable_at_startup_does_not_flood_when_it_comes_online()
    {
        // Lần đọc đầu lúc chưa đăng nhập / mất mạng: chưa có mail, lịch, Boards
        var e = Start(new WorkSnapshot { MailAvailable = false, CalendarAvailable = false, BoardsAvailable = false });
        var existing = new WorkSnapshot
        {
            RecentMail = [Mail("old", "Minh", "Báo cáo tuần")],
            Calendar = [new CalendarEvent { Id = "ev1", Subject = "Daily", Start = T("14:00"), End = T("14:15"), Organizer = "Lan" }],
            Tasks = [new WorkTask { Id = "4821", Title = "Sửa lỗi", Days = 1 }],
            CompletedToday = [new CompletedTask("4700", "Xong từ sáng")],
        };
        e.ApplySnapshot(existing); // đăng nhập xong: đây là mốc, không phải "mới"
        e.Advance(30, false);
        Assert.DoesNotContain(e.S.Queue, q => q.C is CaseId.Incoming or CaseId.TaskDone);
        Assert.Null(e.S.Ep);

        e.ApplySnapshot(new WorkSnapshot { RecentMail = [.. existing.RecentMail, Mail("m2", "Chị Linh", "Nhờ review")], Calendar = existing.Calendar, Tasks = existing.Tasks, CompletedToday = existing.CompletedToday });
        Until(e, () => e.S.Ep is { C: CaseId.Incoming, Phase: Phase.Show }, 60);
        Assert.Single(e.S.Ep!.Data.Incoming!);
    }

    [Fact]
    public void A_new_email_pops_up_with_sender_subject_and_opens_on_click()
    {
        var e = Start();
        e.ApplySnapshot(new WorkSnapshot { RecentMail = [Mail("m1", "Chị Linh", "Nhờ review PR #512")] });
        Until(e, () => e.S.Ep is { C: CaseId.Incoming, Phase: Phase.Show }, 60);
        var card = Present.Card(e);
        Assert.Contains(card.Blocks, b => b is TitleBlock t && t.Text.Contains("Chị Linh"));
        Assert.Contains(card.Blocks, b => b is LineBlock l && l.Text == "Nhờ review PR #512");
        e.UserReply("open");
        Assert.Contains(_actions, a => a is MiloAction.OpenLink { Url: "https://outlook.office.com/mail/m1" });
    }

    [Fact]
    public void Several_new_things_are_grouped_in_one_card()
    {
        var e = Start();
        e.ApplySnapshot(new WorkSnapshot
        {
            RecentMail = [Mail("m1", "Chị Linh", "Review PR"), Mail("m2", "Anh Tuấn", "Họp lại lúc 3h?")],
            Tasks = [new WorkTask { Id = "4821", Title = "Sửa lỗi đăng nhập" }],
        });
        Until(e, () => e.S.Ep is { C: CaseId.Incoming, Phase: Phase.Show }, 60);
        Assert.Equal(3, e.S.Ep!.Data.Incoming!.Count);
        Assert.Contains(Present.Card(e).Blocks, b => b is TitleBlock t && t.Text.StartsWith("3 thứ mới"));
        // Tới thêm lúc thẻ đang hiện: gộp vào thẻ, không bật thẻ thứ 2
        e.ApplySnapshot(new WorkSnapshot { RecentMail = [Mail("m3", "Bảo", "Tài liệu")] });
        Assert.Equal(4, e.S.Ep!.Data.Incoming!.Count);
    }

    [Fact]
    public void Meeting_invites_i_created_myself_are_not_announced()
    {
        var e = Start();
        CalendarEvent Ev(string id, bool byMe) => new() { Id = id, Subject = "Sync " + id, Start = T("15:00"), End = T("15:30"), Organizer = "Anh Tuấn", ByMe = byMe };
        e.ApplySnapshot(new WorkSnapshot { Calendar = [Ev("mine", true)] });
        e.Advance(20, false);
        Assert.DoesNotContain(e.S.Queue, q => q.C == CaseId.Incoming);
        e.ApplySnapshot(new WorkSnapshot { Calendar = [Ev("mine", true), Ev("invite", false)] });
        Until(e, () => e.S.Ep is { C: CaseId.Incoming, Phase: Phase.Show }, 60);
        Assert.Contains(Present.Card(e).Blocks, b => b is TitleBlock t && t.Text.Contains("lời mời họp mới lúc 15:00"));
    }

    [Fact]
    public void During_a_meeting_the_card_waits_and_comes_after()
    {
        var meeting = new CalendarEvent { Id = "call", Subject = "Daily", Start = T("10:05"), End = T("10:20") };
        var e = Start(new WorkSnapshot { Calendar = [meeting] });
        Until(e, () => e.HardGate() == Gate.Meeting, 600);
        e.ApplySnapshot(new WorkSnapshot { Calendar = [meeting], RecentMail = [Mail("m1", "Chị Linh", "Review PR")] });
        e.Advance(60, false);
        Assert.Null(e.S.Ep);
        Assert.Contains(e.S.Queue, q => q.C == CaseId.Incoming);
        Until(e, () => e.S.Ep is { C: CaseId.Incoming }, 1800);
        Assert.True(e.S.T >= meeting.End);
    }

    [Fact]
    public void Alerts_can_be_turned_off()
    {
        var e = Start(cfg: new EngineConfig { IncomingAlerts = false });
        e.ApplySnapshot(new WorkSnapshot { RecentMail = [Mail("m1", "Chị Linh", "Review PR")] });
        e.Advance(30, false);
        Assert.DoesNotContain(e.S.Queue, q => q.C == CaseId.Incoming);
    }

    [Fact]
    public void Ignored_card_folds_away_without_nagging()
    {
        var e = Start();
        e.ApplySnapshot(new WorkSnapshot { RecentMail = [Mail("m1", "Chị Linh", "Review PR")] });
        Until(e, () => e.S.Ep is { C: CaseId.Incoming, Phase: Phase.Show }, 60);
        Until(e, () => e.S.Ep is null, 120);
        Assert.DoesNotContain(e.S.ParkedList, p => p.C == CaseId.Incoming);
    }

    [Fact]
    public void Demo_simulates_mail_then_meeting_then_task()
    {
        var e = DemoScenario.CreateEngine();
        e.RunTo(DemoTour.FreeMoment);
        e.ForceCase(CaseId.Incoming);
        e.ForceCase(CaseId.Incoming);
        e.ForceCase(CaseId.Incoming);
        var q = e.S.Queue.Single(x => x.C == CaseId.Incoming);
        Assert.Equal(["mail", "meeting", "task"], q.Data.Incoming!.Select(i => i.Kind));
    }
}
