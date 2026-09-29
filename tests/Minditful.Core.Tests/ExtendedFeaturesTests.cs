using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;
using Minditful.Integrations.Storage;
using Xunit;
using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Tests;

/// <summary>
/// Tính năng mở rộng: giữ giờ tập trung, báo cáo tuần sáng thứ Hai, uống nước / 20-20-20, trốn khi trình chiếu,
/// "Hôm nay thấy sao?", nghỉ giữa chuỗi họp ngày mai, tủ đồ, bảng chi tiết dashboard.
/// </summary>
public sealed class ExtendedFeaturesTests : IDisposable
{
    private static readonly DateOnly Monday = new(2026, 9, 28);
    private readonly List<MiloAction> _actions = [];
    private readonly string _dir = Directory.CreateTempSubdirectory("minditful-ext-").FullName;

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    private MiloEngine Start(EngineConfig cfg, string at = "09:00", WorkSnapshot? snap = null, DateOnly? day = null)
    {
        var e = new MiloEngine(cfg, snap ?? DemoScenario.Snapshot(), null, day ?? DemoScenario.Day, T(at));
        e.SetAuto(false);
        e.ActionRequested += _actions.Add;
        e.SetLocked(false);
        Until(e, () => e.S.Ep is { C: CaseId.MorningHello, Phase: Phase.Show });
        e.UserReply("gotIt");
        Until(e, () => e.S.Ep?.C != CaseId.MorningHello); // thẻ kế tiếp có thể lên ngay sau khi Milo leo xuống
        return e;
    }

    private static void Until(MiloEngine e, Func<bool> done, double maxSec = 180)
    {
        var end = e.S.T + maxSec;
        while (!done())
        {
            Assert.True(e.S.T < end, $"Quá {maxSec}s (lúc {Hm(e.S.T)}, ep={e.S.Ep?.C}/{e.S.Ep?.Phase})\n" + string.Join("\n", e.S.Log.TakeLast(10).Select(l => Hm(l.T) + " " + l.Text)));
            e.Advance(0.5, stopWhenBusy: false);
        }
    }

    /// <summary>Tua nhanh tới giờ <paramref name="at"/>, tự bấm nút phụ của mọi thẻ khác chen vào.</summary>
    private static void RunTo(MiloEngine e, string at)
    {
        while (e.S.T < T(at))
        {
            if (e.S.Ep is { Phase: Phase.Show } ep && ep.C != CaseId.Dashboard)
                e.UserReply(Present.Card(e).Blocks.OfType<ButtonsBlock>().SelectMany(b => b.Buttons).LastOrDefault()?.Act ?? "dismiss");
            e.Advance(1, stopWhenBusy: false);
        }
    }

    private static IEnumerable<string> Buttons(MiloEngine e) =>
        Present.Card(e).Blocks.OfType<ButtonsBlock>().SelectMany(b => b.Buttons).Where(b => b.Enabled).Select(b => b.Act);

    // ================= giữ giờ tập trung =================
    [Fact]
    public void FocusPlan_holds_the_longest_gap_then_turns_on_dnd_when_it_starts()
    {
        var e = Start(new EngineConfig { FocusPlan = true });
        Until(e, () => e.S.Ep is { C: CaseId.FocusPlan, Phase: Phase.Show }, 1800); // 20 phút sau lần mở máy đầu
        // Ngày mẫu: họp 9:30–10:30 và 13:30–16:10, trưa 12–13 → khoảng dài nhất là 16:15–18:00, đề nghị 90 phút
        Assert.Equal(T("16:15"), e.S.Ep!.Data.At);
        Assert.Equal(90, e.S.Ep.Data.Min);
        Assert.Contains(Present.Card(e).Blocks, b => b is ParagraphBlock p && p.Text.Contains("16:15") && p.Text.Contains("17:45"));
        e.UserReply("accept");
        Assert.Contains(_actions, a => a is MiloAction.HoldFocus { Start: var s, End: var en } && s == T("16:15") && en == T("17:45"));
        Assert.Contains(e.S.Holds, h => h.Kind == "focusPlan");

        RunTo(e, "16:16");
        Assert.True(e.FocusActive());
        Assert.Equal(T("17:45"), e.S.FocusUntil);
        Assert.Contains(_actions, a => a is MiloAction.StartFocus { CalendarHeld: true });
        Assert.Equal(Gate.Focus, e.HardGate());
    }

    [Fact]
    public void FocusPlan_dismiss_is_for_the_whole_day_and_off_by_default()
    {
        var off = Start(new EngineConfig());
        RunTo(off, "09:30");
        Assert.DoesNotContain(off.S.Log, l => l.Text.StartsWith("Giữ giờ tập trung đủ điều kiện"));

        var e = Start(new EngineConfig { FocusPlan = true });
        Until(e, () => e.S.Ep is { C: CaseId.FocusPlan, Phase: Phase.Show }, 1800); // 20 phút sau lần mở máy đầu
        e.UserReply("dismiss");
        Until(e, () => e.S.Ep is null);
        RunTo(e, "11:00");
        Assert.Single(e.S.Log, l => l.Text.StartsWith("Giữ giờ tập trung đủ điều kiện"));
    }

    // ================= báo cáo tuần =================
    [Fact]
    public void WeekReport_on_monday_after_morning_hello_then_opens_the_week_page()
    {
        var e = Start(new EngineConfig { WeekReport = true }, "09:00", new WorkSnapshot { LastWeek = DemoScenario.Snapshot().LastWeek }, Monday);
        Until(e, () => e.S.Ep is { C: CaseId.WeekReport, Phase: Phase.Show }, 120);
        var card = Present.Card(e);
        Assert.Contains(card.Blocks, b => b is TitleBlock { Text: "Tuần trước của bạn" });
        Assert.Contains(card.Blocks, b => b is TilesBlock t && t.Tiles[0].Big == "68");
        Assert.Equal(["gotIt", "week"], Buttons(e));
        e.UserReply("week");
        Until(e, () => e.S.Ep is { C: CaseId.Dashboard, Phase: Phase.Show });
        Assert.Equal(DashPage.Week, e.S.Ep!.Page);
    }

    [Fact]
    public void WeekReport_only_on_monday_and_only_with_last_week_data()
    {
        var thursday = Start(new EngineConfig { WeekReport = true }, "09:00", new WorkSnapshot { LastWeek = DemoScenario.Snapshot().LastWeek });
        RunTo(thursday, "09:20");
        Assert.DoesNotContain(thursday.S.Log, l => l.Text.StartsWith("Báo cáo tuần đủ điều kiện"));

        var empty = Start(new EngineConfig { WeekReport = true }, "09:00", new WorkSnapshot(), Monday);
        RunTo(empty, "09:20");
        Assert.DoesNotContain(empty.S.Log, l => l.Text.StartsWith("Báo cáo tuần đủ điều kiện"));
    }

    [Fact]
    public void Week_tip_picks_the_weakest_point()
    {
        var w = DemoScenario.Snapshot().LastWeek!;
        Assert.Contains("Thứ Ba là ngày nặng nhất", Present.WeekTip(w)); // ngày tệ nhất 42 điểm
        Assert.Contains("về đúng giờ", Present.WeekTip(w with { Worst = new DayScore("T3", 60), OvertimeMin = 90 }));
        Assert.Contains("Giữ phong độ", Present.WeekTip(w with { Worst = new DayScore("T3", 60), OvertimeMin = 0, AcceptedBreaks = 9, FocusMin = 900 }));
    }

    // ================= uống nước / 20-20-20 =================
    [Fact]
    public void MicroBreak_bubble_after_each_block_of_screen_time()
    {
        var e = Start(new EngineConfig { MicroBreakEveryMin = 50 }, "09:00", new WorkSnapshot());
        Until(e, () => e.S.Ep is { C: CaseId.MicroBreak, Phase: Phase.Bubble }, 3600);
        Assert.InRange(e.S.T, T("09:48"), T("09:53")); // 50 phút ngồi máy tính từ lúc mở máy 09:00
        Assert.Equal(Present.MicroText(0), Present.Card(e).SayText);
        Until(e, () => e.S.Ep is null);
        Until(e, () => e.S.Ep is { C: CaseId.MicroBreak, Phase: Phase.Bubble }, 3600);
        Assert.Contains("20-20-20", Present.Card(e).SayText);
    }

    [Fact]
    public void MicroBreak_resets_when_you_step_away()
    {
        var e = Start(new EngineConfig { MicroBreakEveryMin = 50 }, "09:00", new WorkSnapshot());
        RunTo(e, "09:40");
        e.SetAway(true);
        RunTo(e, "09:50");
        e.SetAway(false);
        RunTo(e, "10:20");
        Assert.DoesNotContain(e.S.Log, l => l.Text.StartsWith("Uống nước · nhìn xa đủ điều kiện"));
    }

    // ================= trình chiếu =================
    [Fact]
    public void Presenting_hides_everything_including_the_tail_and_interrupts_cards()
    {
        var e = Start(new EngineConfig(), "11:00", new WorkSnapshot());
        e.ForceCase(CaseId.NoBreak);
        Until(e, () => e.S.Ep is { C: CaseId.NoBreak, Phase: Phase.Show });
        e.SetPresenting(true);
        Until(e, () => e.S.Ep is null, 10);
        Assert.Equal(Gate.Presenting, e.HardGate());
        Assert.Equal(PresenceState.Off, e.Presence());
        Assert.False(Present.TailVisible(e));
        Assert.Null(Present.DotPill(e)); // lời nhắc bị ngắt quay lại hàng đợi nhưng không hiện chấm chờ
        e.SetPresenting(false);
        Assert.True(Present.TailVisible(e) || e.S.Ep is not null);
    }

    // ================= tan tầm: hôm nay thấy sao, nghỉ ngày mai =================
    [Fact]
    public void Evening_feeling_is_saved_and_counts_in_the_mood()
    {
        var e = Start(new EngineConfig(), "17:55", new WorkSnapshot());
        Until(e, () => e.S.Ep is { C: CaseId.EodWrapup, Phase: Phase.Show }, 600);
        var before = e.S.Score;
        e.UserReply("feel", "bad");
        Assert.Equal(Feeling.Bad, e.S.Feeling);
        Assert.Equal(before - 6, e.S.Score);
        Assert.Equal(CaseId.EodWrapup, e.S.Ep!.C); // thẻ vẫn mở, chưa về
        Assert.Contains(Present.Card(e).Blocks.OfType<ButtonsBlock>().SelectMany(b => b.Buttons), b => b is { Act: "feel", Val: "bad", Style: ButtonStyle.Dark });
        e.UserReply("feel", "good");
        Assert.Equal(before + 3, e.S.Score);
        e.UserReply("goHome");
        Until(e, () => e.S.Ep is null);
        Assert.Equal(Feeling.Good, Assert.Single(_actions.OfType<MiloAction.DayClosed>()).Record.Feeling);
    }

    [Fact]
    public void Evening_card_offers_a_break_inside_tomorrows_meeting_chain()
    {
        var e = Start(new EngineConfig(), "17:55", new WorkSnapshot { TomorrowCalendar = DemoScenario.Snapshot().TomorrowCalendar });
        Until(e, () => e.S.Ep is { C: CaseId.EodWrapup, Phase: Phase.Show }, 600);
        Assert.Contains(Present.Card(e).Blocks, b => b is ParagraphBlock p && p.Text.Contains("3 cuộc họp liền"));
        e.UserReply("holdTomorrow");
        // Chuỗi mai 13:30–16:15: nghỉ sau cuộc thứ 2 (15:30), giống cách Lịch kín giữ chỗ hôm nay
        Assert.Contains(_actions, a => a is MiloAction.HoldBreak { DayOffset: 1, Start: var s } && s == T("15:30"));
        Assert.Contains(Present.Card(e).Blocks, b => b is StatusDotBlock sd && sd.Text.Contains("15:30"));
        Assert.DoesNotContain("holdTomorrow", Buttons(e));
    }

    // ================= tủ đồ =================
    [Fact]
    public void Wardrobe_streak_unlocks_items_and_resets_after_overtime()
    {
        var s = new LocalStore(Path.Combine(_dir, "w.db"));
        DayRecord Day(int i, double ot) => new(Monday.AddDays(i), 70, 60, 1, 30, 1, ot, 3, 3, 2, 3);
        Assert.Null(s.RecordDay(Monday, true).Unlocked);
        s.RecordDay(Monday.AddDays(1), true);
        var third = s.RecordDay(Monday.AddDays(2), Wardrobe.OnTime(Day(2, 10)));
        Assert.Equal(3, third.Streak);
        Assert.Equal("scarf", third.Unlocked!.Id);
        // Cùng ngày tính lại (vẫn làm tiếp tới quá giờ) → chuỗi về 0 nhưng món đã mở vẫn giữ
        var redo = s.RecordDay(Monday.AddDays(2), Wardrobe.OnTime(Day(2, 40)));
        Assert.Equal((0, 3), (redo.Streak, redo.Best));
        // Báo món mới vào ngày làm việc kế tiếp, không báo lại ngày sau nữa
        Assert.Equal("khăn quàng", s.Wardrobe(Monday.AddDays(3)).NewItem);
        s.RecordDay(Monday.AddDays(3), true);
        Assert.Null(s.Wardrobe(Monday.AddDays(4)).NewItem);
        Assert.Equal(["scarf"], Wardrobe.Unlocked(s.Wardrobe(Monday.AddDays(4)).Best).Select(i => i.Id));
        s.WipeAll();
        Assert.Equal(0, s.Wardrobe(Monday.AddDays(4)).Best);
    }

    [Fact]
    public void Feeling_is_stored_per_day_and_counted_in_week_stats()
    {
        var s = new LocalStore(Path.Combine(_dir, "f.db"));
        s.SaveDay(new DayRecord(Monday, 70, 60, 1, 30, 1, 0, 3, 3, 2, 3, Feeling.Bad));
        s.SaveDay(new DayRecord(Monday, 72, 60, 1, 30, 1, 0, 3, 3, 2, 3)); // lưu định kỳ sau đó không xoá câu trả lời
        s.SaveDay(new DayRecord(Monday.AddDays(1), 80, 60, 1, 30, 1, 0, 3, 3, 2, 3, Feeling.Good));
        Assert.Equal(Feeling.Bad, s.Days(Monday, Monday.AddDays(1))[0].Feeling);
        var w = s.WeekStats(Monday)!;
        Assert.Equal((1, 0, 1), (w.FeelGood, w.FeelOk, w.FeelBad));
        Assert.Contains("mệt 1 ngày", Present.WeekSummary(w, null));
    }

    [Fact]
    public void Morning_card_announces_a_new_accessory()
    {
        var snap = DemoScenario.Snapshot();
        var e = new MiloEngine(new EngineConfig(), snap, null, DemoScenario.Day, T("09:00"));
        e.SetAuto(false);
        e.SetLocked(false);
        Until(e, () => e.S.Ep is { C: CaseId.MorningHello, Phase: Phase.Show });
        Assert.Contains(Present.Card(e).Blocks, b => b is ParagraphBlock p && p.Text.Contains("Milo được tặng đồng phục Bosch"));
    }

    // ================= bảng chi tiết =================
    [Fact]
    public void Detail_dashboard_today_and_week()
    {
        var e = Start(new EngineConfig(), "11:00", new WorkSnapshot { Calendar = DemoScenario.Snapshot().Calendar, Week = DemoScenario.Snapshot().Week,
            ThisWeek = DemoScenario.Snapshot().ThisWeek, LastWeek = DemoScenario.Snapshot().LastWeek });
        Until(e, () => e.S.Ep is null);
        e.TailClick();
        Until(e, () => e.S.Ep is { C: CaseId.Dashboard, Phase: Phase.Show });
        Assert.Null(Present.Detail(e));
        e.UserReply("detail");
        var d = Present.Detail(e)!.Today!;
        Assert.Equal(4, d.Segments.Count(x => x.Kind is "meet" or "heavy"));
        Assert.InRange(d.Now!.Value, 0.2, 0.3); // 11:00 trong khung 09:00–18:00
        Assert.Equal(4, d.Chips.Count);
        Assert.All(d.Upcoming, r => Assert.True(TimeOnly.Parse(r.Time) >= new TimeOnly(10, 59)));
        e.UserReply("week");
        var w = Present.Detail(e)!.Week!;
        Assert.Equal(7, w.Days.Count);
        Assert.NotNull(w.Diff);
        Assert.Contains(w.Replies, r => r.Label == "Đồng ý");
        e.UserReply("detail");
        Assert.Null(Present.Detail(e));
        Assert.NotNull(Present.Fruits(e));
    }
}
