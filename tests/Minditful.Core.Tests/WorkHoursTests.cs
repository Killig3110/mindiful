using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;
using Minditful.Integrations.Storage;
using Xunit;
using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Tests;

/// <summary>Giờ làm linh hoạt kiểu Bosch: bắt đầu từ lần mở máy đầu ngày (08:00–10:00), làm 9 tiếng.</summary>
public class WorkHoursTests
{
    private static MiloEngine Flex(string openAt) =>
        new(new EngineConfig { FlexEarliestStart = T("08:00"), FlexLatestStart = T("10:00"), FlexHours = 9, MorningHelloUntil = T("12:00") },
            new WorkSnapshot(), null, DemoScenario.Day, T(openAt));

    [Theory]
    [InlineData("08:00", "08:00", "17:00")]
    [InlineData("09:00", "09:00", "18:00")]
    [InlineData("10:00", "10:00", "19:00")]
    [InlineData("08:47", "08:45", "17:45")]   // làm tròn xuống 5 phút
    [InlineData("07:20", "08:00", "17:00")]   // mở máy sớm: vẫn tính từ 8h
    [InlineData("10:40", "10:00", "19:00")]   // mở máy muộn: tối đa 10h
    public void Day_window_follows_first_unlock(string open, string start, string end)
    {
        var e = Flex(open);
        e.SetLocked(false);
        Assert.Equal((start, end), (Hm(e.Cfg.Start), Hm(e.Cfg.End)));
    }

    [Fact]
    public void End_of_day_follows_the_flexible_window()
    {
        var e = Flex("08:00");
        e.SetAuto(false);
        e.SetLocked(false);
        e.Advance(T("17:01") - e.S.T, false);
        Assert.Contains(e.S.Log, l => l.Text.StartsWith("Tan tầm đủ điều kiện") && Hm(l.T) == "17:00");
    }

    [Fact]
    public void Reopening_the_app_keeps_the_real_start_and_does_not_greet_again()
    {
        var e = Flex("14:00");
        e.RestoreDayStart(T("08:32"));
        e.SetLocked(false);
        Assert.Equal(("08:30", "17:30"), (Hm(e.Cfg.Start), Hm(e.Cfg.End)));
        Assert.Equal(T("08:32"), e.S.FirstAct);
        Assert.DoesNotContain(e.S.Queue, q => q.C == CaseId.MorningHello);
    }

    [Fact]
    public void Morning_card_tells_when_to_go_home()
    {
        var e = Flex("08:50");
        e.SetAuto(false);
        e.SetLocked(false);
        for (var i = 0; i < 200 && e.S.Ep is not { Phase: Phase.Show }; i++) e.Advance(0.25, false);
        var texts = Present.Card(e).Blocks.OfType<ParagraphBlock>().Select(p => p.Text);
        Assert.Contains(texts, t => t.Contains("Hôm nay về lúc 17:50"));
    }

    [Fact]
    public void Fixed_mode_and_demo_keep_9_to_18()
    {
        var e = DemoScenario.CreateEngine();
        e.RunTo(T("09:00"));
        Assert.Equal(("09:00", "18:00"), (Hm(e.Cfg.Start), Hm(e.Cfg.End)));
    }

    [Fact]
    public void Day_start_is_stored_once_per_day()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var s = new LocalStore(Path.Combine(dir, "t.db"));
            s.SaveDayStart(DemoScenario.Day, T("08:32"));
            s.SaveDayStart(DemoScenario.Day, T("14:00")); // mở lại app: không ghi đè
            Assert.Equal(T("08:32"), s.DayStart(DemoScenario.Day));
            Assert.Null(s.DayStart(DemoScenario.Day.AddDays(1)));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { Directory.Delete(dir, true); } catch (IOException) { }
        }
    }
}
