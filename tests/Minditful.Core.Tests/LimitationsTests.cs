using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;
using Minditful.Integrations;
using Minditful.Integrations.Live;
using Minditful.Integrations.Llm;
using Xunit;
using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Tests;

/// <summary>4 giới hạn cũ của README: Lớp 2 (LLM), clip "Cần vẽ", góc neo (phần thuần toán), cá nhân hoá 7 ngày.</summary>
public class LimitationsTests
{
    private static MiloEngine Fresh(string at = "11:00", WorkSnapshot? snap = null)
    {
        var e = new MiloEngine(new EngineConfig(), snap ?? DemoScenario.Snapshot(), null, DemoScenario.Day, T(at));
        e.SetAuto(false);
        e.SetLocked(false);
        while (e.S.Ep is not { C: CaseId.MorningHello, Phase: Phase.Show }) e.Advance(0.25, false);
        e.UserReply("gotIt");
        e.S.Queue.Clear();
        while (e.S.Ep is not null) e.Advance(0.25, false);
        return e;
    }

    private static Episode Show(MiloEngine e, CaseId c)
    {
        e.S.Queue.Clear();
        e.ForceCase(c);
        for (var i = 0; i < 400 && e.S.Ep is not { Phase: Phase.Show }; i++) e.Advance(0.25, false);
        Assert.Equal(c, e.S.Ep?.C);
        return e.S.Ep!;
    }

    private static void Finish(MiloEngine e)
    {
        for (var i = 0; i < 800 && e.S.Ep is not null; i++) e.Advance(0.25, false);
    }

    private static string MainText(MiloEngine e) =>
        Present.Card(e).Blocks.OfType<ParagraphBlock>().First(p => !p.Small).Text;

    // ================= 1. Lớp 2: lời thoại =================
    [Fact]
    public void Templates_rotate_so_the_same_sentence_is_not_repeated()
    {
        var e = Fresh();
        var seen = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            Show(e, CaseId.NoBreak);
            seen.Add(MainText(e));
            e.UserReply("snooze");
            Finish(e);
        }
        Assert.Equal(3, seen.Distinct().Count());
        Assert.StartsWith("Bạn làm liền", seen[0]); // biến thể 0 = câu của prototype
    }

    [Fact]
    public void Line_is_requested_when_case_enters_queue_and_used_on_the_card()
    {
        var e = Fresh();
        var requests = new List<(QueueItem Item, LineRequest Req)>();
        e.LineWanted += (item, req) => requests.Add((item, req));
        e.SimulateFragmentation(); // Phân mảnh vào hàng đợi bằng luật thật
        var (item, req) = Assert.Single(requests);
        Assert.Equal(CaseId.HighFragmentation, req.Case);
        Assert.Contains("12 lần", req.Facts);
        e.SetLine(item, "Mình gom việc lại 30 phút cho đầu đỡ rối nhé?");
        for (var i = 0; i < 40 && e.S.Ep is not { Phase: Phase.Show }; i++) e.Advance(0.25, false);
        Assert.Equal("Mình gom việc lại 30 phút cho đầu đỡ rối nhé?", MainText(e));
        // nhãn và nút vẫn từ template
        Assert.StartsWith("Bị cắt vụn", Present.Card(e).Blocks.OfType<TopBlock>().Single().Pill);
    }

    [Fact]
    public void Facts_sent_to_the_llm_never_contain_titles()
    {
        var snap = DemoScenario.Snapshot();
        var secrets = snap.Calendar.Select(c => c.Subject).Concat(snap.Emails.Select(m => m.Subject)).Concat(snap.Tasks.Select(t => t.Title)).ToList();
        var e = Fresh("13:00");
        var data = new CaseData { Min = 160, Sw = 11, Over = 35, Rest = 6, Worked = 200, Chain = e.Chains().First(c => c.Count >= 3), Task = snap.Tasks[0] };
        foreach (var c in Enum.GetValues<CaseId>())
        {
            var facts = Lines.Facts(e, c, data);
            foreach (var s in secrets) Assert.DoesNotContain(s, facts);
        }
    }

    [Fact]
    public void Unknown_chat_goes_to_llm_and_falls_back_when_it_fails()
    {
        var e = Fresh();
        var asked = new List<(int Ep, int Idx, ChatRequest Req)>();
        e.ChatWanted += (ep, idx, req) => asked.Add((ep, idx, req));
        var ep = Show(e, CaseId.NoBreak);
        e.UserReply("chat", "hôm nay trời đẹp ghê");
        var q = Assert.Single(asked);
        Assert.Equal(Lines.ChatThinking, ep.Chat[0].Milo);
        e.ResolveChat(q.Ep, q.Idx, "Trời đẹp thì ra ban công hít thở 1 phút với Milo nha?");
        Assert.StartsWith("Trời đẹp", ep.Chat[0].Milo);

        e.UserReply("chat", "abc xyz");
        e.ResolveChat(asked[1].Ep, asked[1].Idx, null);
        Assert.Equal(Lines.ChatFallback, ep.Chat[1].Milo);
    }

    [Theory]
    [InlineData("Đứng dậy duỗi vai 1 phút với Milo nhé?", true)]
    [InlineData("\"Uống ngụm nước nha.\"", true)]
    [InlineData("**Nghỉ** đi", false)]
    [InlineData("", false)]
    public void Llm_output_is_validated(string raw, bool ok) => Assert.Equal(ok, Lines.Clean(raw) is not null);

    [Fact]
    public async Task Writer_without_api_key_is_unavailable_and_returns_null()
    {
        var w = new ClaudeLineWriter(new LlmOptions(), () => null);
        Assert.False(w.Available);
        Assert.Null(await w.WriteLineAsync(new LineRequest(CaseId.NoBreak, "Làm liền", "2h", "thở", "x")));
    }

    // ================= 2. Clip "Cần vẽ" =================
    [Fact]
    public void Every_clip_has_a_rig_with_valid_part_moves()
    {
        foreach (var clip in Enum.GetValues<Clip>())
        {
            var rig = MiloRig.For(clip, tired: false);
            Assert.InRange(rig.Frames, 1, 11);
            for (var f = 0; f < rig.Frames; f++)
                foreach (var m in MiloRig.Frame(rig, f, blink: true))
                    Assert.True(MiloRig.Pivots.ContainsKey(m.Part), $"{clip}: bộ phận {m.Part} chưa có khớp");
        }
        Assert.Equal(11, MiloRig.For(Clip.ClimbIn, false).Frames); // "11 khung · 8fps" (§12)
        Assert.Equal(9, MiloRig.For(Clip.RunToCar, false).Frames); // "9 khung · 10fps"
        Assert.Equal(MiloRig.IdleTired, MiloRig.For(Clip.Idle, tired: true));
    }

    [Fact]
    public void Frame_index_loops_ping_pongs_and_holds()
    {
        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 5, 4, 3, 2, 1, 0], Enumerable.Range(0, 13).Select(i => MiloRig.FrameIndex(MiloRig.Idle, i / 6.0 + 0.01)));
        Assert.Equal(0, MiloRig.FrameIndex(MiloRig.Run, 9 / 10.0 + 0.01));
        Assert.Equal(5, MiloRig.FrameIndex(MiloRig.Stretch, 10)); // không lặp: giữ khung cuối
        Assert.True(MiloRig.Blink(4.5 * 3 + 0.05, tired: false));
        Assert.False(MiloRig.Blink(2, tired: false));
        Assert.True(MiloRig.Blink(0.25, tired: true)); // mệt: nhắm lâu hơn
    }

    [Fact]
    public void Waving_arm_actually_moves_between_frames()
    {
        var a = MiloRig.Frame(MiloRig.Greet, 1, false).Single(m => m.Part == "armR").Angle;
        var b = MiloRig.Frame(MiloRig.Greet, 4, false).Single(m => m.Part == "armR").Angle;
        Assert.NotEqual(a, b);
        Assert.True(a < -90 && b < -90); // tay phải (bản lật) giơ lên = góc âm lớn
    }

    // ================= 3. Cá nhân hoá 7 ngày =================
    private static IEnumerable<OutcomeEvent> Days(CaseId c, params Outcome[] replies)
    {
        var day = DemoScenario.Day.AddDays(-1);
        foreach (var r in replies)
        {
            yield return new OutcomeEvent(day, 36000, c, Outcome.Shown, 70);
            yield return new OutcomeEvent(day, 36010, c, r, 70);
            day = day.AddDays(-1);
        }
    }

    [Fact]
    public void Rule2_mostly_dismissed_case_waits_twice_as_long_and_needs_15pct_more()
    {
        var events = Days(CaseId.NoBreak, Outcome.Dismissed, Outcome.Ignored, Outcome.Dismissed, Outcome.Accepted, Outcome.Dismissed);
        var tuning = Personalizer.Compute(events, DemoScenario.Day);
        Assert.Equal(new CaseTuning(2, 1.15), tuning[CaseId.NoBreak]);

        var e = new MiloEngine(new EngineConfig(), new WorkSnapshot(), null, DemoScenario.Day, T("09:00")) { Tuning = tuning };
        e.SetAuto(false);
        e.SetLocked(false);
        e.Advance(T("11:05") - e.S.T, false);   // 125 phút: bình thường đã bật (120)
        Assert.DoesNotContain(e.S.Log, l => l.Text.StartsWith("Làm liền đủ điều kiện"));
        e.Advance(T("11:20") - e.S.T, false);   // 140 phút ≥ 138
        Assert.Contains(e.S.Log, l => l.Text.StartsWith("Làm liền đủ điều kiện"));
        Assert.Equal(30, e.SnoozeMinutes(CaseId.NoBreak)); // 15 × 2
    }

    [Fact]
    public void Rule2_needs_at_least_5_showings_and_never_disables_a_case()
    {
        Assert.Empty(Personalizer.Compute(Days(CaseId.LowRest, Outcome.Dismissed, Outcome.Dismissed, Outcome.Dismissed, Outcome.Dismissed), DemoScenario.Day));
        var t = Personalizer.Compute(Days(CaseId.LowRest, Enumerable.Repeat(Outcome.Ignored, 7).ToArray()), DemoScenario.Day)[CaseId.LowRest];
        Assert.Equal(2, t.WaitFactor); // chỉ thưa đi, không tắt
    }

    [Fact]
    public void Rule3_often_snoozed_case_waits_for_the_longest_gap()
    {
        var tuning = Personalizer.Compute(Days(CaseId.StuckTask, Enumerable.Repeat(Outcome.Snoozed, 5).ToArray()), DemoScenario.Day);
        Assert.True(tuning[CaseId.StuckTask].PreferLongGap);

        // 10:40: khoảng trống hiện tại 10:40–13:30 (170') dài hơn 16:10–18:00 → giao ngay
        var e = Fresh("10:40");
        e.Tuning = tuning;
        Assert.Null(e.LongestGapLater());
        // 9:00: khoảng trống hiện tại chỉ 30' (tới Sprint Planning), dài nhất là 10:30–13:30 → chờ
        var f = new MiloEngine(new EngineConfig(), DemoScenario.Snapshot(), null, DemoScenario.Day, T("09:00")) { Tuning = tuning };
        Assert.Equal("10:30", Hm(f.LongestGapLater()!.Value));
    }

    [Fact]
    public void Outcomes_are_recorded_for_every_reply_kind()
    {
        var e = Fresh();
        var log = new List<(CaseId, Outcome)>();
        e.OutcomeRecorded += (c, o) => log.Add((c, o));
        Show(e, CaseId.NoBreak);
        e.UserReply("snooze");
        Finish(e);
        Show(e, CaseId.NoBreak);
        e.UserReply("dismiss");
        Finish(e);
        Show(e, CaseId.LowRest);
        e.UserReply("accept");
        Finish(e);
        Show(e, CaseId.EmailWaiting);
        Finish(e); // hết giờ không trả lời
        Show(e, CaseId.HighFragmentation);
        e.SetFullscreen(true);
        e.Advance(1, false);
        Assert.Contains((CaseId.NoBreak, Outcome.Snoozed), log);
        Assert.Contains((CaseId.NoBreak, Outcome.Dismissed), log);
        Assert.Contains((CaseId.LowRest, Outcome.Accepted), log);
        Assert.Contains((CaseId.EmailWaiting, Outcome.Ignored), log);
        Assert.Contains((CaseId.HighFragmentation, Outcome.Gated), log);
        Assert.Equal(5, log.Count(x => x.Item2 == Outcome.Shown));
    }

    [Fact]
    public void Outcome_store_round_trips_and_trims()
    {
        var file = Path.Combine(Path.GetTempPath(), $"minditful-{Guid.NewGuid():N}.tsv");
        try
        {
            var store = new OutcomeStore(file);
            var today = DemoScenario.Day;
            store.Append(new OutcomeEvent(today.AddDays(-40), 100, CaseId.NoBreak, Outcome.Shown, 60));
            store.Append(new OutcomeEvent(today.AddDays(-1), 36000.7, CaseId.LowRest, Outcome.Snoozed, 72));
            Assert.Equal(2, store.Load().Count);
            store.Trim(today);
            var e = Assert.Single(store.Load());
            Assert.Equal((CaseId.LowRest, Outcome.Snoozed, 72, 36000.0), (e.Case, e.Kind, e.Score, e.T));
        }
        finally
        {
            File.Delete(file);
        }
    }
}

/// <summary>docs/KET-NOI-SANDBOX.md: ngưỡng rút gọn của Sandbox, tên môi trường, hạ cấp khi thiếu quyền ghi lịch.</summary>
public class SandboxGuideTests
{
    [Fact]
    public void Sandbox_overrides_shorten_thresholds()
    {
        var cfg = new EngineConfig { StuckMinDays = 0, NoBreakMin = 20, EmailNotBefore = T("00:00"), GapBudget = 180 };
        var snap = new WorkSnapshot { Tasks = [new WorkTask { Id = "7", Title = "Task mới giao", Days = 0 }] };
        var e = new MiloEngine(cfg, snap, null, DemoScenario.Day, T("09:00"));
        e.SetAuto(false);
        e.SetLocked(false);
        e.Advance(25 * 60, false);
        Assert.Contains(e.S.Log, l => l.Text.StartsWith("Task kẹt đủ điều kiện"));   // task Active 0 ngày vẫn tính kẹt
        Assert.Contains(e.S.Log, l => l.Text.StartsWith("Làm liền đủ điều kiện"));   // làm liền 20 phút
    }

    [Theory]
    [InlineData("Scenario", AppEnvironment.Demo)]
    [InlineData("demo", AppEnvironment.Demo)]
    [InlineData("Sandbox", AppEnvironment.Sandbox)]
    [InlineData("Prod", AppEnvironment.Production)]
    [InlineData("Production", AppEnvironment.Production)]
    public void Environment_names_from_the_guide_are_accepted(string name, AppEnvironment env) => Assert.Equal(env, AppEnvironments.Parse(name));

    [Fact]
    public void Without_calendar_write_permission_hold_becomes_a_reminder()
    {
        var snap = DemoScenario.Snapshot();
        var e = new MiloEngine(new EngineConfig(), new WorkSnapshot { Calendar = snap.Calendar, CanWriteCalendar = false }, null, DemoScenario.Day, T("09:00"));
        e.SetAuto(false);
        e.SetLocked(false);
        e.S.Queue.Clear();
        e.ForceCase(CaseId.CalendarPacked);
        for (var i = 0; i < 400 && e.S.Ep is not { C: CaseId.CalendarPacked, Phase: Phase.Show }; i++)
        {
            if (e.S.Ep is { C: CaseId.MorningHello, Phase: Phase.Show }) e.UserReply("gotIt");
            e.Advance(0.25, false);
        }
        var buttons = Present.Card(e).Blocks.OfType<ButtonsBlock>().SelectMany(b => b.Buttons).Select(b => b.Label).ToList();
        Assert.Contains("Nhắc tôi lúc đó", buttons);
        Assert.DoesNotContain("Giữ chỗ trong lịch", buttons);
    }

    [Fact]
    public void Sandbox_appsettings_match_the_guide()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "src", "Minditful.App", "appsettings.json"))) root = root.Parent;
        var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(root!.FullName, "src", "Minditful.App", "appsettings.json")));
        var sb = json.RootElement.GetProperty("Minditful").GetProperty("Sandbox");
        Assert.Equal("093be8d4-f285-4982-a198-db10d74e61e2", sb.GetProperty("Graph").GetProperty("TenantId").GetString());
        Assert.Equal("aa4ae2a6-4d2a-474c-a6f5-d7d2ebf5de06", sb.GetProperty("Graph").GetProperty("ClientId").GetString());
        Assert.Equal("Milo-Sandbox Team", sb.GetProperty("AzureDevOps").GetProperty("Team").GetString());
        Assert.Equal(20, sb.GetProperty("BehaviorOverrides").GetProperty("NoBreakStreakMin").GetInt32());
        Assert.DoesNotContain("Mail.Send", sb.GetProperty("Graph").GetProperty("Scopes").EnumerateArray().Select(x => x.GetString()));
    }
}
