using Minditful.Core.Engine;
using Minditful.Core.Presentation;
using Minditful.Core.Scenario;
using Minditful.Integrations.Storage;
using Xunit;

namespace Minditful.Core.Tests;

/// <summary>SQLite local: lưu, thống kê tuần, tự xoá theo tuần/tháng, không lưu tiêu đề.</summary>
public sealed class StorageTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("minditful-").FullName;
    private static readonly DateOnly Monday = new(2026, 9, 28);

    private LocalStore Store(string period = "Week", bool keepPrev = true) =>
        new(Path.Combine(_dir, $"{period}-{keepPrev}.db"), new StorageOptions { RetentionPeriod = period, KeepPreviousPeriod = keepPrev });

    private static DayRecord Day(DateOnly d, int score) => new(d, score, 120, 2, 60, 1, 0, 3, 3, 2, 4);

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    [Fact]
    public void Days_and_outcomes_round_trip()
    {
        var s = Store();
        s.SaveDay(Day(Monday, 70));
        s.SaveDay(Day(Monday, 64)); // upsert trong ngày
        s.Append(new OutcomeEvent(Monday, 36000.5, CaseId.NoBreak, Outcome.Snoozed, 64));
        Assert.Equal(64, Assert.Single(s.Days(Monday, Monday.AddDays(1))).Score);
        var o = Assert.Single(s.Outcomes(Monday));
        Assert.Equal((CaseId.NoBreak, Outcome.Snoozed, 36000.5), (o.Case, o.Kind, o.T));
        Assert.Equal(64, s.Yesterday(Monday.AddDays(1)));
    }

    [Fact]
    public void Week_stats_and_comparison_with_last_week()
    {
        var s = Store();
        for (var i = 0; i < 5; i++) s.SaveDay(Day(Monday.AddDays(i - 7), 60));        // tuần trước TB 60
        int[] scores = [72, 58, 80];
        for (var i = 0; i < 3; i++) s.SaveDay(Day(Monday.AddDays(i), scores[i]));     // tuần này
        s.Append(new OutcomeEvent(Monday, 1, CaseId.NoBreak, Outcome.Shown, 72));
        s.Append(new OutcomeEvent(Monday, 2, CaseId.NoBreak, Outcome.Accepted, 72));
        var w = s.WeekStats(Monday.AddDays(2))!;
        Assert.Equal(3, w.Days);
        Assert.Equal(70, w.AvgScore);
        Assert.Equal(("T4", 80), (w.Best!.Label, w.Best.Score));
        Assert.Equal(("T3", 58), (w.Worst!.Label, w.Worst.Score));
        Assert.Equal((1, 1), (w.Shown, w.Accepted));
        var text = Present.WeekSummary(w, s.WeekStats(Monday.AddDays(-1)))!;
        Assert.Contains("+10 so với tuần trước", text);
    }

    [Fact]
    public void Weekly_retention_keeps_this_and_last_week_then_deletes_older()
    {
        var s = Store("Week", keepPrev: true);
        foreach (var d in new[] { Monday.AddDays(-15), Monday.AddDays(-8), Monday.AddDays(-7), Monday })
        {
            s.SaveDay(Day(d, 70));
            s.Append(new OutcomeEvent(d, 1, CaseId.NoBreak, Outcome.Shown, 70));
            s.SampleMood(d, 36000, 70, 70, "Cân bằng", "Luật");
        }
        var r = s.Cleanup(Monday.AddDays(3));  // thứ Năm tuần này
        Assert.Equal(Monday.AddDays(-7), r.Cutoff);
        Assert.Equal(2, r.Days);                // 2 ngày của 2 tuần trước nữa bị xoá
        Assert.Equal([Monday.AddDays(-7), Monday], s.Days(DateOnly.MinValue, DateOnly.MaxValue).Select(x => x.Date));
        Assert.Equal(2, s.Outcomes(DateOnly.MinValue).Count);
        Assert.Empty(s.MoodSamples(Monday.AddDays(-8)));
    }

    [Fact]
    public void Strict_weekly_retention_keeps_only_the_current_week()
    {
        var s = Store("Week", keepPrev: false);
        s.SaveDay(Day(Monday.AddDays(-1), 60)); // Chủ nhật tuần trước
        s.SaveDay(Day(Monday, 70));
        s.Cleanup(Monday);                      // đúng thứ Hai → dữ liệu tuần trước biến mất
        Assert.Equal([Monday], s.Days(DateOnly.MinValue, DateOnly.MaxValue).Select(x => x.Date));
    }

    [Fact]
    public void Monthly_retention_uses_calendar_months()
    {
        var s = Store("Month", keepPrev: true);
        s.SaveDay(Day(new DateOnly(2026, 7, 31), 60));
        s.SaveDay(Day(new DateOnly(2026, 8, 1), 65));
        s.SaveDay(Day(new DateOnly(2026, 9, 29), 70));
        var r = s.Cleanup(new DateOnly(2026, 9, 29));
        Assert.Equal(new DateOnly(2026, 8, 1), r.Cutoff);
        Assert.Equal(2, s.Days(DateOnly.MinValue, DateOnly.MaxValue).Count);
    }

    [Fact]
    public void Meeting_titles_and_outlook_ids_are_never_stored()
    {
        var s = Store();
        var ev = DemoScenario.Snapshot().Calendar[0];
        s.SaveMeeting(Monday, new MeetingAssessment(ev.Id, 4, "Trình bày", 10, "cuộc dài", "Luật"), ev.Start, 60);
        Assert.Equal(1, s.MeetingCount(Monday));
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        var bytes = System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(s.Path));
        Assert.DoesNotContain(ev.Subject, bytes);
    }

    [Fact]
    public void Wipe_and_legacy_import()
    {
        File.WriteAllText(Path.Combine(_dir, "history.json"), System.Text.Json.JsonSerializer.Serialize(new[] { Day(Monday, 66) }));
        File.WriteAllText(Path.Combine(_dir, "outcomes.tsv"), "2026-09-28\t100\tNoBreak\tShown\t66\n");
        var s = Store();
        Assert.Equal(2, s.ImportLegacy(_dir));
        Assert.True(File.Exists(Path.Combine(_dir, "history.json.imported")));
        Assert.Single(s.Days(DateOnly.MinValue, DateOnly.MaxValue));
        s.WipeAll();
        Assert.Empty(s.Days(DateOnly.MinValue, DateOnly.MaxValue));
        Assert.Empty(s.Outcomes(DateOnly.MinValue));
    }
}
