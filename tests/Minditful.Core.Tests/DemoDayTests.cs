using Minditful.Core.Engine;
using Minditful.Core.Scenario;
using Xunit;
using Xunit.Abstractions;
using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Tests;

/// <summary>Chạy trọn ngày mẫu với người dùng tự trả lời và so với mục 13 của tài liệu.</summary>
public class DemoDayTests(ITestOutputHelper output)
{
    private static MiloEngine RunDay()
    {
        var e = DemoScenario.CreateEngine();
        // Tua như prototype: nhanh khi Milo ẩn, 1× khi Milo đang hiện.
        while (!e.S.Ended) e.Advance(e.Busy ? 0.1 : 1);
        return e;
    }

    private static IEnumerable<LogEntry> Delivered(MiloEngine e) => e.S.Log.Where(l => l.Kind == LogKind.Deliver);

    private static double FirstDelivery(MiloEngine e, string name) =>
        Delivered(e).First(l => l.Text.StartsWith($"**Giao {name}**")).T;

    [Fact]
    public void Sample_day_matches_documented_timeline()
    {
        var e = RunDay();
        foreach (var l in e.S.Log) output.WriteLine($"{Hm(l.T)} [{l.Kind}] {l.Text}");

        Assert.Equal("08:58", Hm(FirstDelivery(e, "Chào sáng")));
        Assert.Equal("09:25", Hm(FirstDelivery(e, "Sắp họp")));
        Assert.Equal("10:37", Hm(FirstDelivery(e, "Task kẹt")));
        Assert.Equal("12:07", Hm(FirstDelivery(e, "Hết giờ tập trung")));
        Assert.Equal("12:55", Hm(FirstDelivery(e, "Lịch kín")));
        Assert.Equal("13:10", Hm(FirstDelivery(e, "Email chờ")));
        Assert.Equal("13:15", Hm(FirstDelivery(e, "Dashboard")));
        Assert.Equal("16:12", Hm(FirstDelivery(e, "Họp liên tục")));
        Assert.Equal("17:45", Hm(FirstDelivery(e, "Task xong")));
        Assert.Equal("18:00", Hm(FirstDelivery(e, "Tan tầm")));
        Assert.Equal("18:31", Hm(FirstDelivery(e, "Nhắc lại tan tầm")));
        Assert.True(e.S.OffDuty);
        Assert.Equal(54, e.S.Score);
    }

    [Fact]
    public void Milo_stays_silent_through_all_meetings()
    {
        var e = RunDay();
        foreach (var d in Delivered(e))
        {
            var inMeeting = e.Meetings.Any(m => d.T > m.Start + 1 && d.T < m.End);
            Assert.False(inMeeting, $"Giao lúc {Hm(d.T)} giữa cuộc họp: {d.Text}");
        }
    }

    [Fact]
    public void Jumping_to_a_milestone_is_deterministic()
    {
        var a = DemoScenario.CreateEngine();
        a.RunTo(T("16:12") - 12);
        var b = DemoScenario.CreateEngine();
        b.RunTo(T("16:12") - 12);
        Assert.Equal(a.S.Score, b.S.Score);
        Assert.Equal(a.S.Queue.Select(q => q.C), b.S.Queue.Select(q => q.C));
    }
}
