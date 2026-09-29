using Minditful.Core.Engine;
using Xunit;
using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Tests;

/// <summary>
/// Chứng minh công thức mood đúng với các kết luận nghiên cứu (docs/CO-SO-KHOA-HOC.md) <b>với mọi dữ liệu</b>:
/// sinh ngẫu nhiên hàng nghìn bộ số liệu / ngày làm việc (hạt giống cố định để chạy lại ra y hệt) và kiểm tra tính chất.
/// Nghiên cứu cho biết chiều tác động và ngưỡng, nên đây là thứ được chứng minh; con số tuyệt đối được hiệu chỉnh bằng WHO-5.
/// </summary>
public class MoodEvidenceTests
{
    private const int Samples = 20_000;

    private static MoodInputs RandomInputs(Random r) => new(
        MeetingMin: r.Next(0, 600), LongestChain: r.Next(0, 9), StreakMin: r.Next(0, 360), OvertimeMin: r.Next(0, 300),
        EarlyStart: r.Next(2) == 0, WorkedMin: r.Next(0, 720), RestMin: r.Next(0, 120), SwitchesHour: r.Next(0, 40),
        FragThreshold: r.Next(4, 31), WorkloadRatio: r.NextDouble() * 3, StuckTasks: r.Next(0, 8), WaitingEmails: r.Next(0, 15),
        Stress: r.Next(0, 61), Feeling: r.Next(0, 4), AcceptedBreaks: r.Next(0, 8), TasksDone: r.Next(0, 6), FocusBlocks: r.Next(0, 4),
        ExtraBonus: r.Next(0, 5), WeekOvertimeMin: r.Next(0, 1500));

    // ---------- Áp lực (demands) và nguồn hồi phục (resources) — mô hình JD-R, Bakker, Demerouti & Sanz-Vergel (2023) ----------
    private static readonly (string Name, Func<MoodInputs, double, MoodInputs> More)[] Demands =
    [
        ("giờ họp", (x, d) => x with { MeetingMin = x.MeetingMin + d * 120 }),
        ("chuỗi họp liền", (x, d) => x with { LongestChain = x.LongestChain + (int)Math.Ceiling(d * 4) }),
        ("làm liền không nghỉ", (x, d) => x with { StreakMin = x.StreakMin + d * 120 }),
        ("quá giờ hôm nay", (x, d) => x with { OvertimeMin = x.OvertimeMin + d * 120 }),
        ("quá giờ cả tuần", (x, d) => x with { WeekOvertimeMin = x.WeekOvertimeMin + d * 600 }),
        ("giờ đã làm (cần nghỉ nhiều hơn)", (x, d) => x with { WorkedMin = x.WorkedMin + d * 240 }),
        ("nhảy việc", (x, d) => x with { SwitchesHour = x.SwitchesHour + (int)Math.Ceiling(d * 15) }),
        ("khối lượng việc", (x, d) => x with { WorkloadRatio = x.WorkloadRatio + d * 1.5 }),
        ("task kẹt", (x, d) => x with { StuckTasks = x.StuckTasks + (int)Math.Ceiling(d * 4) }),
        ("email chờ", (x, d) => x with { WaitingEmails = x.WaitingEmails + (int)Math.Ceiling(d * 6) }),
    ];

    private static readonly (string Name, Func<MoodInputs, double, MoodInputs> More)[] Resources =
    [
        ("thời gian nghỉ", (x, d) => x with { RestMin = x.RestMin + d * 60 }),
        ("lần nghỉ cùng Milo", (x, d) => x with { AcceptedBreaks = x.AcceptedBreaks + (int)Math.Ceiling(d * 3) }),
        ("khối tập trung trọn vẹn", (x, d) => x with { FocusBlocks = x.FocusBlocks + (int)Math.Ceiling(d * 2) }),
        ("task xong", (x, d) => x with { TasksDone = x.TasksDone + (int)Math.Ceiling(d * 3) }),
    ];

    [Fact]
    public void Score_always_stays_between_0_and_100()
    {
        var r = new Random(1);
        for (var i = 0; i < Samples; i++) Assert.InRange(MoodModel.Score(RandomInputs(r)), 0, 100);
    }

    [Fact]
    public void More_demand_never_raises_the_score_JDR()
    {
        var r = new Random(2);
        for (var i = 0; i < Samples; i++)
        {
            var x = RandomInputs(r);
            foreach (var (name, more) in Demands)
            {
                var y = more(x, r.NextDouble());
                Assert.True(MoodModel.Score(y) <= MoodModel.Score(x), $"Thêm {name} mà điểm tăng: {x} → {y}");
            }
        }
    }

    [Fact]
    public void More_recovery_never_lowers_the_score_JDR()
    {
        var r = new Random(3);
        for (var i = 0; i < Samples; i++)
        {
            var x = RandomInputs(r);
            foreach (var (name, more) in Resources)
            {
                var y = more(x, r.NextDouble());
                Assert.True(MoodModel.Score(y) >= MoodModel.Score(x), $"Thêm {name} mà điểm giảm: {x} → {y}");
            }
        }
    }

    /// <summary>
    /// Albulescu et al. (2025): nghỉ ngắn càng có lợi khi khối lượng việc cao (tương tác workload × micro-break).
    /// Giới hạn đã biết (test này tìm ra): ngày nặng tới mức điểm chạm 0 thì chỉ số không phân biệt được nữa (hiệu ứng sàn),
    /// nên tính chất chỉ xét khi ngày nặng chưa chạm sàn. Ghi trong docs/CO-SO-KHOA-HOC.md mục Giới hạn.
    /// </summary>
    [Fact]
    public void A_break_helps_at_least_as_much_on_a_heavy_day_as_on_a_light_day()
    {
        var r = new Random(4);
        var checkedCases = 0;
        for (var i = 0; i < Samples; i++)
        {
            var x = RandomInputs(r);
            if (MoodModel.Score(x with { WorkedMin = 480, StreakMin = 150 }) == 0) continue; // chạm sàn: xem mô tả
            checkedCases++;
            MoodInputs Light(MoodInputs v) => v with { WorkedMin = 90, StreakMin = 60 };
            MoodInputs Heavy(MoodInputs v) => v with { WorkedMin = 480, StreakMin = 150 };
            MoodInputs Break(MoodInputs v) => v with { RestMin = v.RestMin + 10, AcceptedBreaks = v.AcceptedBreaks + 1, StreakMin = 0 };
            var gainLight = MoodModel.Score(Break(Light(x))) - MoodModel.Score(Light(x));
            var gainHeavy = MoodModel.Score(Break(Heavy(x))) - MoodModel.Score(Heavy(x));
            Assert.True(gainHeavy >= gainLight, $"Nghỉ ngày nặng +{gainHeavy} < ngày nhẹ +{gainLight}: {x}");
        }
        // Bộ sinh ngẫu nhiên cố tình dồn cả giá trị cực đoan (họp tới 10 tiếng, quá giờ 5 tiếng…) nên khoảng nửa số ca chạm sàn
        Assert.True(checkedCases > Samples / 3, $"Chỉ kiểm được {checkedCases} ca chưa chạm sàn");
    }

    /// <summary>Hiệu ứng sàn có thật nhưng hiếm: chỉ xảy ra khi dồn nhiều áp lực cực đoan cùng lúc.</summary>
    [Fact]
    public void Floor_effect_needs_extreme_days()
    {
        var typicalHeavy = new MoodInputs(MeetingMin: 300, LongestChain: 4, StreakMin: 150, OvertimeMin: 60, WorkedMin: 540, RestMin: 15,
            SwitchesHour: 12, WorkloadRatio: 1.8, StuckTasks: 2, WaitingEmails: 5);
        Assert.True(MoodModel.Score(typicalHeavy) > 0, "Một ngày nặng thường gặp không được chạm sàn 0");
    }

    /// <summary>Kim et al. (2024): 41–54 giờ/tuần chưa có liên hệ rõ, &gt; 48 giờ/tuần có. Milo chỉ phạt quá giờ theo tuần khi vượt 48 giờ.</summary>
    [Fact]
    public void Weekly_overtime_counts_only_above_48_hours()
    {
        var r = new Random(5);
        for (var i = 0; i < Samples; i++)
        {
            var x = RandomInputs(r);
            Assert.Equal(0, MoodModel.Penalties(x with { WeekOvertimeMin = r.Next(0, 481) })["week"]);
            Assert.True(MoodModel.Penalties(x with { WeekOvertimeMin = 481 + r.Next(0, 1000) })["week"] > 0);
        }
        var baseDay = new MoodInputs(WorkedMin: 480, RestMin: 45);
        Assert.True(MoodModel.Score(baseDay with { WeekOvertimeMin = 15 * 60 }) < MoodModel.Score(baseDay with { WeekOvertimeMin = 5 * 60 }),
            "55 giờ/tuần phải thấp điểm hơn 45 giờ/tuần");
    }

    /// <summary>Becker et al. (2023): đa nhiệm / bị ngắt quãng gây stress. Chuyển việc bình thường (dưới ngưỡng) không bị trừ.</summary>
    [Fact]
    public void Normal_switching_is_free_and_fragmentation_costs()
    {
        var r = new Random(6);
        for (var i = 0; i < Samples; i++)
        {
            var x = RandomInputs(r);
            Assert.Equal(0, MoodModel.Penalties(x with { SwitchesHour = r.Next(0, x.FragThreshold + 1) })["frag"]);
            Assert.True(MoodModel.Penalties(x with { SwitchesHour = x.FragThreshold + 1 + r.Next(0, 20) })["frag"] > 0);
        }
    }

    [Fact]
    public void Every_penalty_has_a_source()
    {
        foreach (var k in MoodModel.Penalties(new MoodInputs()).Keys.Where(k => k != "stress"))
            Assert.True(MoodModel.Evidence.ContainsKey(k), $"Khoản \"{k}\" chưa có nguồn trong MoodModel.Evidence");
    }

    // ---------- Chạy cả ngày qua engine thật với lịch ngẫu nhiên ----------
    private static WorkSnapshot Meetings(params (double Start, double End)[] ms) => new()
    {
        Calendar = ms.Select((m, i) => new CalendarEvent { Id = "m" + i, Subject = "Họp " + i, Start = m.Start, End = m.End }).ToList(),
    };

    private static MiloEngine Day(WorkSnapshot snap, IEnumerable<(double From, double To)> away, double until)
    {
        var e = new MiloEngine(new EngineConfig(), snap, null, new DateOnly(2026, 9, 29), T("08:55"));
        e.SetAuto(false);
        e.SetLocked(false);
        var breaks = away.OrderBy(b => b.From).ToList();
        while (e.S.T < until)
        {
            var now = e.S.T;
            e.SetAway(breaks.Any(b => now >= b.From && now < b.To));
            // Thẻ của Milo không được trả lời → không có điểm thưởng nào khác ngoài chính lịch và giờ nghỉ đang so sánh
            e.Advance(30, stopWhenBusy: false);
        }
        return e;
    }

    /// <summary>
    /// Microsoft Human Factors Lab (2021, ngoài 5 năm — chỉ tham khảo) và Albulescu et al. (2022): nghỉ giữa các cuộc họp giúp giảm mệt.
    /// Cùng số cuộc họp, xếp cách nhau 10 phút thì điểm không bao giờ thấp hơn xếp liền nhau.
    /// </summary>
    [Fact]
    public void Gaps_between_meetings_never_score_worse_than_back_to_back()
    {
        var r = new Random(7);
        for (var i = 0; i < 25; i++)
        {
            var n = r.Next(2, 6);
            var len = r.Next(2, 5) * 15 * 60.0;
            var start = T("09:30") + r.Next(0, 8) * 900;
            var chained = Enumerable.Range(0, n).Select(k => (start + k * len, start + (k + 1) * len)).ToArray();
            var spaced = Enumerable.Range(0, n).Select(k => (start + k * (len + 600), start + k * (len + 600) + len)).ToArray();
            var until = spaced[^1].Item2 + 1800;
            var a = Day(Meetings(chained), [], until).S.RuleScore;
            var b = Day(Meetings(spaced), [], until).S.RuleScore;
            Assert.True(b >= a, $"{n} cuộc × {len / 60}' có nghỉ 10' được {b} < liền nhau {a}");
        }
    }

    /// <summary>Albulescu et al. (2022; 2025): thêm 1 lần nghỉ ngắn (rời máy 10 phút) không bao giờ làm điểm cuối ngày thấp hơn.</summary>
    [Fact]
    public void An_extra_short_break_never_lowers_the_end_of_day_score()
    {
        var r = new Random(8);
        for (var i = 0; i < 25; i++)
        {
            var baseBreaks = Enumerable.Range(0, r.Next(0, 3)).Select(_ => T("10:00") + r.Next(0, 24) * 900.0).Select(s => (s, s + r.Next(5, 40) * 60.0)).ToList();
            var extraAt = T("09:30") + r.Next(0, 28) * 900.0;
            var withExtra = baseBreaks.Append((extraAt, extraAt + 600)).ToList();
            var until = T("17:30");
            var a = Day(new WorkSnapshot(), baseBreaks, until).S.RuleScore;
            var b = Day(new WorkSnapshot(), withExtra, until).S.RuleScore;
            Assert.True(b >= a, $"Thêm nghỉ 10' lúc {Hm(extraAt)} được {b} < không nghỉ {a}");
        }
    }

    /// <summary>Engine luôn tính đúng công thức với số liệu nó đang thấy, dù lịch và giờ nghỉ ra sao.</summary>
    [Fact]
    public void Engine_score_always_equals_the_model_on_its_own_inputs()
    {
        var r = new Random(9);
        for (var i = 0; i < 20; i++)
        {
            var meetings = Enumerable.Range(0, r.Next(0, 7)).Select(_ => T("09:00") + r.Next(0, 32) * 900.0).Distinct().OrderBy(s => s)
                .Select(s => (s, s + r.Next(1, 5) * 900.0)).ToArray();
            var breaks = Enumerable.Range(0, r.Next(0, 4)).Select(_ => T("09:30") + r.Next(0, 36) * 900.0).Select(s => (s, s + r.Next(3, 50) * 60.0)).ToList();
            var e = Day(Meetings(meetings), breaks, T("16:00") + r.Next(0, 16) * 900);
            e.ComputeMood();
            Assert.Equal(MoodModel.Score(e.MoodInputsNow()), e.S.RuleScore);
            Assert.InRange(e.S.Score, 0, 100);
        }
    }
}
