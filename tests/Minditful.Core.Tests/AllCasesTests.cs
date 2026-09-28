using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;
using Xunit;
using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Tests;

/// <summary>
/// Phủ toàn bộ 16 episode của prototype: (1) mỗi case tự bật đúng điều kiện của luật,
/// (2) mỗi nút trên mỗi thẻ dẫn tới đúng kết quả như prototype / tài liệu mục 6–10.
/// </summary>
public class AllCasesTests
{
    private readonly List<MiloAction> _actions = [];

    // ================= tiện ích =================
    private MiloEngine Fresh(string at = "09:00", WorkSnapshot? snap = null)
    {
        var e = new MiloEngine(new EngineConfig(), snap ?? DemoScenario.Snapshot(), null, DemoScenario.Day, T(at));
        e.SetAuto(false);
        e.ActionRequested += _actions.Add;
        e.SetLocked(false);
        Until(e, () => e.S.Ep is { C: CaseId.MorningHello, Phase: Phase.Show });
        e.UserReply("gotIt");
        e.S.Queue.Clear(); // bắt đầu từ hàng đợi rỗng, mỗi test tự chọn case cần thử
        Until(e, () => e.S.Ep is null);
        return e;
    }

    private static void Until(MiloEngine e, Func<bool> done, double maxSec = 180)
    {
        var end = e.S.T + maxSec;
        while (!done())
        {
            Assert.True(e.S.T < end, $"Quá {maxSec}s mà chưa tới trạng thái mong đợi (lúc {Hm(e.S.T)}, ep={e.S.Ep?.C}/{e.S.Ep?.Phase})\n" + string.Join("\n", e.S.Log.TakeLast(12).Select(l => Hm(l.T) + " " + l.Text)));
            e.Advance(0.25, stopWhenBusy: false);
        }
    }

    private static Episode Show(MiloEngine e, CaseId c)
    {
        e.S.Queue.Clear();
        e.ForceCase(c);
        Until(e, () => e.S.Ep is { Phase: Phase.Show } ep && ep.C == c);
        return e.S.Ep!;
    }

    private static void Finish(MiloEngine e) => Until(e, () => e.S.Ep is null);

    private static IEnumerable<string> Buttons(MiloEngine e) =>
        Present.Card(e).Blocks.OfType<ButtonsBlock>().SelectMany(b => b.Buttons).Where(b => b.Enabled).Select(b => b.Act);

    private static bool Enqueued(MiloEngine e, CaseId c) => e.S.Log.Any(l => l.Text.StartsWith(Catalog.Def(c).Name + " đủ điều kiện"));

    // ================= 1. mỗi case tự bật theo luật =================
    [Fact]
    public void Sample_day_delivers_the_scripted_episodes()
    {
        var e = DemoScenario.CreateEngine();
        while (!e.S.Ended) e.Advance(e.Busy ? 0.25 : 1);
        var delivered = e.S.Log.Where(l => l.Kind == LogKind.Deliver).Select(l => l.Text).ToList();
        CaseId[] expected =
        [
            CaseId.MorningHello, CaseId.MeetingSoon, CaseId.StuckTask, CaseId.FocusDone, CaseId.LowRest, CaseId.CalendarPacked,
            CaseId.EmailWaiting, CaseId.Dashboard, CaseId.MeetingOverload, CaseId.TaskDone, CaseId.EodWrapup, CaseId.EodNudge,
        ];
        foreach (var c in expected) Assert.Contains(delivered, t => t.StartsWith($"**Giao {Catalog.Def(c).Name}**"));
        Assert.True(Enqueued(e, CaseId.NoBreak)); // 14:55 vào hàng đợi, 16:12 được gộp vào tổng kết (§8b)
        Assert.Contains(e.S.FoldedList, f => f.C == CaseId.NoBreak);
    }

    [Fact]
    public void LunchMissed_when_you_stay_at_the_desk_over_lunch()
    {
        var script = DemoScenario.Script();
        var noLunch = new ScenarioScript
        {
            World = script.World.Where(w => w.T is not (43200 + 900) && w.T != T("12:55")).ToList(),
            Ui = script.Ui,
            AutoReplies = script.AutoReplies,
        };
        var e = new MiloEngine(DemoScenario.Config(), DemoScenario.Snapshot(), noLunch, DemoScenario.Day);
        while (e.S.T < T("12:40")) e.Advance(e.Busy ? 0.25 : 1);
        Assert.True(Enqueued(e, CaseId.LunchMissed));
    }

    [Fact]
    public void NoBreak_and_LowRest_after_hours_without_a_break()
    {
        var e = Fresh("09:00", new WorkSnapshot());
        e.Advance(T("11:05") - e.S.T, stopWhenBusy: false);
        Assert.True(Enqueued(e, CaseId.NoBreak));   // ≥ 120 phút không có lần nghỉ ≥ 5 phút
        e.Advance(T("12:05") - e.S.T, stopWhenBusy: false);
        Assert.True(Enqueued(e, CaseId.LowRest));   // đã làm ≥ 3 giờ, nghỉ < 50% kỳ vọng
    }

    [Fact]
    public void HighFragmentation_after_many_app_switches()
    {
        var e = Fresh("11:00", new WorkSnapshot());
        e.SimulateFragmentation();
        Assert.True(Enqueued(e, CaseId.HighFragmentation));
        e.Advance(3, stopWhenBusy: false);
        Assert.Equal(CaseId.HighFragmentation, e.S.Ep?.C);
    }

    [Fact]
    public void Overtime_escalates_after_end_of_day()
    {
        var e = Fresh("17:30", new WorkSnapshot());
        e.Advance(T("18:40") - e.S.T, stopWhenBusy: false);
        Assert.True(Enqueued(e, CaseId.EodWrapup));
        Assert.True(Enqueued(e, CaseId.Overtime));  // còn thao tác > 30 phút sau giờ về
    }

    [Fact]
    public void CheckIn_appears_randomly_when_mood_is_good()
    {
        var found = false;
        for (uint seed = 1; seed <= 20 && !found; seed++)
        {
            var e = new MiloEngine(new EngineConfig { Seed = seed }, new WorkSnapshot(), null, DemoScenario.Day, T("08:55"));
            e.SetAuto(false);
            e.SetLocked(false);
            while (e.S.T < T("16:30") && !found)
            {
                e.Advance(5, stopWhenBusy: false);
                if (e.S.Ep is { Phase: Phase.Show } ep && ep.C != CaseId.CheckIn) e.UserReply(ep.C == CaseId.MorningHello ? "gotIt" : "accept");
                // nghỉ đều đặn để không dính Làm liền/Nghỉ quá ít
                if ((int)e.S.T % 5400 < 5) e.SetAway(true);
                if ((int)e.S.T % 5400 is >= 600 and < 605) e.SetAway(false);
                found = Enqueued(e, CaseId.CheckIn);
            }
        }
        Assert.True(found);
    }

    // ================= 2. mọi nút của mọi thẻ =================
    [Fact]
    public void MorningHello_buttons()
    {
        var e = Fresh();
        e.ForceCase(CaseId.MorningHello);
        Until(e, () => e.S.Ep is { C: CaseId.MorningHello, Phase: Phase.Show });
        Assert.Equal(["gotIt", "remindAt"], Buttons(e));
        e.UserReply("remindAt");
        Assert.Single(e.S.Reminders);
        Assert.Equal(T("10:00"), e.S.Reminders[0].At);
        Finish(e);
    }

    [Fact]
    public void MeetingSoon_open_slide_then_join()
    {
        var e = Fresh("09:20");
        Until(e, () => e.S.Ep is { C: CaseId.MeetingSoon, Phase: Phase.Show }, 400);
        Assert.Equal("09:25", Hm(e.S.T));
        Assert.Equal(["join", "open"], Buttons(e));
        e.UserReply("open");
        Assert.True(e.S.Ep!.Opened);
        Assert.Equal(["join"], Buttons(e)); // Mở slide mờ đi, thẻ vẫn giữ Tham gia
        e.UserReply("join");
        Assert.Equal(Clip.ClimbOutFast, e.S.Ep!.Clip);
        Assert.Contains(_actions, a => a is MiloAction.OpenAttachment);
        Assert.Contains(_actions, a => a is MiloAction.JoinMeeting { Event.Id: "e1" });
    }

    [Fact]
    public void MeetingSoon_times_out_once_and_is_not_repeated()
    {
        var e = Fresh("09:20");
        Until(e, () => e.S.Ep is { C: CaseId.MeetingSoon, Phase: Phase.Show }, 400);
        Until(e, () => e.S.Ep is null, 90);
        Assert.Empty(e.S.ParkedList); // Sắp họp không thu thành chấm
        e.Advance(60, stopWhenBusy: false);
        Assert.DoesNotContain(e.S.Queue, q => q.C == CaseId.MeetingSoon);
    }

    [Fact]
    public void CalendarPacked_hold_and_dismiss()
    {
        var e = Fresh();
        Show(e, CaseId.CalendarPacked);
        Assert.Equal(["accept", "dismiss"], Buttons(e));
        e.UserReply("accept");
        Assert.Contains(e.S.Holds, h => h.Kind == "break" && Hm(h.Start) == "15:30");
        Assert.Contains(Present.Card(e).Blocks.OfType<ScheduleBlock>().Single().Rows, r => r.IsSlot);
        Assert.Contains(_actions, a => a is MiloAction.HoldBreak { Subject: "Nghỉ cùng Milo" });
        Until(e, () => e.S.Ep is { Phase: Phase.Thanks });
        Finish(e);

        var f = Fresh();
        Show(f, CaseId.CalendarPacked);
        f.UserReply("dismiss");
        Assert.True(f.S.Mem[CaseId.CalendarPacked].ForDay);
    }

    [Fact]
    public void EmailWaiting_open_and_remind()
    {
        var e = Fresh("10:40");
        Show(e, CaseId.EmailWaiting);
        Assert.Equal(3, e.S.Ep!.Data.List!.Count);
        Assert.Equal(5, e.S.Ep.Data.Count);
        Assert.Equal(["remindAt", "openMail"], Buttons(e));
        e.UserReply("openMail");
        Assert.Contains(_actions, a => a is MiloAction.OpenMail { Mail.Id: "m1" });
        Assert.Equal(4, e.WaitingEmails().Count);
        Finish(e);

        var f = Fresh("10:40");
        Show(f, CaseId.EmailWaiting);
        f.UserReply("remindAt");
        Assert.Equal(T("16:00"), f.S.Reminders.Single().At);
    }

    [Fact]
    public void StuckTask_lock_focus_then_focus_done()
    {
        var e = Fresh("10:40");
        Show(e, CaseId.StuckTask);
        Assert.Equal("4821", e.S.Ep!.Data.Task!.Id);
        Assert.Equal(["accept", "snooze"], Buttons(e));
        e.UserReply("accept");
        Assert.True(e.FocusActive());
        Assert.Contains(_actions, a => a is MiloAction.StartFocus { TaskId: "4821" });
        Finish(e);
        Assert.Equal(Gate.Focus, e.HardGate());
        e.Advance(e.S.FocusUntil!.Value - e.S.T + 1, stopWhenBusy: false);
        Assert.Contains(_actions, a => a is MiloAction.EndFocus);
        Until(e, () => e.S.Ep is { C: CaseId.FocusDone, Phase: Phase.Bubble });
        Assert.Contains("phút sâu xong rồi", Present.Card(e).SayText);

        var f = Fresh("10:40");
        Show(f, CaseId.StuckTask);
        f.UserReply("snooze");
        Assert.Equal(f.S.T + 3600, f.S.Mem[CaseId.StuckTask].SnoozedUntil, 1);
    }

    [Fact]
    public void TaskDone_bubble_without_buttons()
    {
        var e = Fresh();
        e.MarkTaskDone("4821");
        Until(e, () => e.S.Ep is { C: CaseId.TaskDone, Phase: Phase.Bubble });
        var card = Present.Card(e);
        Assert.Equal(CardVariant.Say, card.Variant);
        Assert.StartsWith("Xong #4821", card.SayText);
        Assert.Equal(1, e.S.TasksDone);
    }

    [Theory]
    [InlineData(CaseId.MeetingOverload, 3)]
    [InlineData(CaseId.NoBreak, 3)]
    [InlineData(CaseId.LowRest, 5)]
    public void Care_accept_leads_to_breathing(CaseId c, int cycles)
    {
        var e = Fresh("11:00");
        Show(e, c);
        e.UserReply("accept");
        Assert.Equal(Phase.Breathe, e.S.Ep!.Phase);
        Assert.Equal(cycles, e.S.Ep.Cycles);
        Assert.Equal(CardVariant.Breathe, Present.Card(e).Variant);
        Until(e, () => e.S.Ep is { Phase: Phase.Thanks }, cycles * 12 + 5);
        Finish(e);
        Assert.Equal(1, e.S.AcceptedBreaks);
        if (c == CaseId.LowRest) Assert.Equal("15'", Present.TailBadge(e)); // Milo giữ chỗ 15 phút, đuôi đếm ngược
    }

    [Fact]
    public void Breathing_can_be_stopped_and_still_counts()
    {
        var e = Fresh("11:00");
        Show(e, CaseId.NoBreak);
        e.UserReply("accept");
        e.Advance(5, stopWhenBusy: false);
        e.UserReply("stop");
        Assert.Equal(Phase.Thanks, e.S.Ep!.Phase);
        Assert.Equal(1, e.S.AcceptedBreaks);
    }

    [Fact]
    public void LunchMissed_go_eat_or_lock_calendar()
    {
        var e = Fresh("12:35");
        Show(e, CaseId.LunchMissed);
        Assert.Equal(["accept", "lunchLock", "snooze", "dismiss"], Buttons(e));
        e.UserReply("accept");
        Assert.Equal(Phase.Thanks, e.S.Ep!.Phase); // không có vòng thở

        var f = Fresh("12:35");
        Show(f, CaseId.LunchMissed);
        f.UserReply("lunchLock");
        Assert.Contains(f.S.Holds, h => h.Kind == "lunch");
        Assert.Contains(_actions, a => a is MiloAction.HoldBreak { Subject: "Nghỉ trưa · Milo giữ chỗ" });
    }

    [Fact]
    public void Overtime_accept_runs_to_the_car()
    {
        var e = Fresh("18:40", new WorkSnapshot());
        e.S.Queue.Clear();
        Show(e, CaseId.Overtime);
        Assert.Contains("Quá giờ làm · 35 phút", Present.Card(e).Blocks.OfType<TopBlock>().Single().Pill);
        e.UserReply("accept");
        Assert.Equal(Clip.RunToCar, e.S.Ep!.Clip);
        Finish(e);
        Assert.True(e.S.OffDuty);
        Assert.Contains(_actions, a => a is MiloAction.DayClosed);
    }

    [Fact]
    public void HighFragmentation_accept_locks_30_minutes()
    {
        var e = Fresh("11:00");
        Show(e, CaseId.HighFragmentation);
        e.UserReply("accept");
        Assert.True(e.FocusActive());
        Assert.Equal(e.S.T + 1800, e.S.FocusUntil!.Value, 1);
        Assert.Contains(_actions, a => a is MiloAction.StartFocus { Title: "Gom việc" });
    }

    [Fact]
    public void Care_snooze_twice_then_only_accept_and_dismiss()
    {
        var e = Fresh("11:00");
        for (var i = 0; i < 2; i++)
        {
            Show(e, CaseId.NoBreak);
            Assert.Contains("snooze", Buttons(e));
            e.UserReply("snooze");
            Assert.Equal(e.S.T + 15 * 60, e.S.Mem[CaseId.NoBreak].SnoozedUntil, 1);
            Finish(e);
        }
        Show(e, CaseId.NoBreak);
        Assert.Equal(["accept", "dismiss"], Buttons(e)); // lần thứ 3 chỉ còn Đồng ý và Không cần (§8.1)
    }

    [Fact]
    public void Care_dismiss_twice_widens_waiting_times_x3()
    {
        var e = Fresh("11:00");
        Show(e, CaseId.NoBreak);
        e.UserReply("dismiss");
        Assert.Equal(e.S.T + 60 * 60, e.S.Mem[CaseId.NoBreak].DismissedUntil, 1);
        Finish(e);
        Show(e, CaseId.NoBreak);
        e.UserReply("dismiss");
        Assert.Equal(3, e.S.Mem[CaseId.NoBreak].Widen);
        Assert.Equal(e.S.T + 180 * 60, e.S.Mem[CaseId.NoBreak].DismissedUntil, 1);
    }

    [Theory]
    [InlineData("đang bận họp", "snooze")]
    [InlineData("mệt quá", "breathe")]
    [InlineData("thôi khỏi", "dismiss")]
    [InlineData("cảm ơn nha", "thanks")]
    [InlineData("hôm nay trời đẹp", "unknown")]
    public void Chat_intents(string text, string expect)
    {
        var e = Fresh("11:00");
        Show(e, CaseId.NoBreak);
        e.UserReply("chat", text);
        Assert.Single(e.S.Ep!.Chat);
        e.Advance(2, stopWhenBusy: false);
        var m = e.S.Mem[CaseId.NoBreak];
        switch (expect)
        {
            case "snooze": Assert.True(m.SnoozedUntil > e.S.T); break;
            case "breathe": Assert.Equal(Phase.Breathe, e.S.Ep!.Phase); break;
            case "dismiss": Assert.True(m.DismissedUntil > e.S.T); break;
            case "thanks": Assert.Equal(1, e.S.ExtraBonus); break;
            case "unknown":
                Assert.Equal(Phase.Show, e.S.Ep!.Phase);
                Assert.Contains("chọn một nút", e.S.Ep.Chat[0].Milo);
                break;
        }
    }

    [Fact]
    public void CheckIn_thanks()
    {
        var e = Fresh("11:00");
        Show(e, CaseId.CheckIn);
        Assert.Equal(["thanks"], Buttons(e));
        e.UserReply("thanks");
        Assert.Equal(Phase.Exit, e.S.Ep!.Phase);
    }

    [Fact]
    public void EodWrapup_extend_then_nudge_go_home()
    {
        var e = Fresh("17:55", new WorkSnapshot());
        Until(e, () => e.S.Ep is { C: CaseId.EodWrapup, Phase: Phase.Show }, 600);
        Assert.Equal(["goHome", "extend"], Buttons(e));
        e.UserReply("extend");
        Assert.Equal(e.S.T + 1800, e.S.ExtendedUntil!.Value, 1);
        Finish(e);
        Until(e, () => e.S.Ep is { C: CaseId.EodNudge, Phase: Phase.Show }, 1900);
        Assert.Equal(["goHome"], Buttons(e));
        e.UserReply("goHome");
        Finish(e);
        Assert.True(e.S.OffDuty);
    }

    [Fact]
    public void Dashboard_today_week_close()
    {
        var e = Fresh("11:00");
        e.TailClick();
        Until(e, () => e.S.Ep is { C: CaseId.Dashboard, Phase: Phase.Show });
        Assert.Equal(DashPage.Today, Present.Dashboard(e)!.Page);
        e.UserReply("week");
        var week = Present.Dashboard(e)!;
        Assert.Equal(DashPage.Week, week.Page);
        Assert.Equal(7, week.Days.Count);
        e.UserReply("today");
        Assert.Equal(DashPage.Today, Present.Dashboard(e)!.Page);
        e.Escape();
        Assert.Equal(Phase.Exit, e.S.Ep!.Phase);
    }

    // ================= 3. điều phối =================
    [Fact]
    public void Unanswered_card_parks_on_tail_and_reopens_on_click()
    {
        var e = Fresh("10:40");
        Show(e, CaseId.EmailWaiting);
        Until(e, () => e.S.Ep is null, 60);
        Assert.Equal("1", Present.TailBadge(e));
        e.TailClick();
        Assert.Equal(CaseId.EmailWaiting, e.S.Ep?.C);
        Assert.True(e.S.Ep!.FromParked);
    }

    [Fact]
    public void Typing_for_5_minutes_gives_compact_chip()
    {
        var e = Fresh("11:00");
        e.SetTyping(true);
        e.ForceCase(CaseId.NoBreak);
        e.Advance(60, stopWhenBusy: false);
        Assert.Null(e.S.Ep); // đang gõ → chờ
        Until(e, () => e.S.Ep is { Phase: Phase.Show }, 300);
        Assert.Equal(CardVariant.Chip, Present.Card(e).Variant);
        e.UserReply("expand");
        Assert.Equal(CardVariant.Card, Present.Card(e).Variant);
    }

    [Fact]
    public void Silent_gate_shows_dot_and_dot_opens_even_in_meeting()
    {
        var e = new MiloEngine(new EngineConfig(), DemoScenario.Snapshot(), null, DemoScenario.Day, T("09:35"));
        e.SetAuto(false);
        e.SetLocked(false); // mở máy giữa cuộc họp Sprint Planning
        e.Advance(3, stopWhenBusy: false);
        Assert.Equal(Gate.Meeting, e.HardGate());
        Assert.Null(e.S.Ep);
        Assert.Equal($"{e.S.Queue.Count} lời nhắc đang chờ", Present.DotPill(e));
        Assert.Contains(e.S.Queue, q => q.C == CaseId.MorningHello);
        e.DotPillClick();
        Assert.True(e.S.Ep!.FromDot);
        Until(e, () => e.S.Ep is { Phase: Phase.Show });
        Assert.True(Present.Card(e).Low);
    }

    [Fact]
    public void Meeting_soon_preempts_a_waiting_care_card()
    {
        var e = Fresh("09:20");
        e.Advance(T("09:24:40") - e.S.T, stopWhenBusy: false);
        Show(e, CaseId.NoBreak);
        Until(e, () => e.S.Ep?.C == CaseId.MeetingSoon, 60);
        Assert.Contains(e.S.Queue, q => q.C == CaseId.NoBreak); // thẻ cũ quay lại hàng đợi
    }

    [Fact]
    public void Gate_interrupts_a_card_and_requeues_it()
    {
        var e = Fresh("11:00");
        Show(e, CaseId.NoBreak);
        e.SetFullscreen(true);
        e.Advance(0.5, stopWhenBusy: false);
        Assert.Equal(Clip.ClimbOutFast, e.S.Ep!.Clip);
        Assert.Contains(e.S.Queue, q => q.C == CaseId.NoBreak);
    }

    [Fact]
    public void Every_case_can_be_forced_and_rendered()
    {
        foreach (var c in Enum.GetValues<CaseId>())
        {
            var e = Fresh("11:00");
            e.ForceCase(c);
            Until(e, () => e.S.Ep is { Card: true } ep && ep.C == c, 60);
            var card = Present.Card(e);
            if (c == CaseId.Dashboard) Assert.NotNull(Present.Dashboard(e));
            else Assert.NotEqual(CardVariant.None, card.Variant);
        }
    }
}
