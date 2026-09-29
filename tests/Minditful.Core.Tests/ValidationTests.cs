using Minditful.Core.Engine;
using Minditful.Integrations.Storage;
using Xunit;

namespace Minditful.Core.Tests;

/// <summary>Kiểm chứng thực tế: WHO-5, tương quan Pearson, lưu cặp số theo tuần.</summary>
public sealed class ValidationTests : IDisposable
{
    private static readonly DateOnly Monday = new(2026, 9, 28);
    private readonly string _dir = Directory.CreateTempSubdirectory("minditful-val-").FullName;

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    [Fact]
    public void Who5_percent_is_raw_sum_times_four()
    {
        Assert.Equal(100, Who5.Percent([5, 5, 5, 5, 5]));
        Assert.Equal(0, Who5.Percent([0, 0, 0, 0, 0]));
        Assert.Equal(68, Who5.Percent([4, 3, 3, 3, 4]));
        Assert.Throws<ArgumentException>(() => Who5.Percent([1, 2, 3]));
        Assert.Throws<ArgumentException>(() => Who5.Percent([6, 0, 0, 0, 0]));
    }

    [Fact]
    public void Pearson_matches_known_values()
    {
        Assert.Equal(1, Validation.Pearson([1, 2, 3, 4], [10, 20, 30, 40])!.Value, 6);
        Assert.Equal(-1, Validation.Pearson([1, 2, 3, 4], [8, 6, 4, 2])!.Value, 6);
        // Ví dụ tính tay: x = 1..5, y = 2,4,5,4,5 → r ≈ 0.7746
        Assert.Equal(0.7746, Validation.Pearson([1, 2, 3, 4, 5], [2, 4, 5, 4, 5])!.Value, 4);
        Assert.Null(Validation.Pearson([1, 2], [3, 4]));
        Assert.Null(Validation.Pearson([5, 5, 5], [1, 2, 3]));
        Assert.Contains("vừa", Validation.Interpret(0.4, 5));
    }

    [Fact]
    public void Weekly_pairs_survive_the_weekly_cleanup_for_12_weeks_then_go()
    {
        var s = new LocalStore(Path.Combine(_dir, "v.db"));
        for (var w = 0; w < 14; w++)
        {
            var day = Monday.AddDays(-7 * w);
            s.SaveDay(new DayRecord(day, 60 + w, 120, 1, 30, 1, 0, 3, 3, 2, 3));
            s.SaveWho5(day, 50 + w);
        }
        s.Cleanup(Monday);
        var rows = s.ValidationRows();
        Assert.Equal(13, rows.Count); // tuần này + 12 tuần trước
        Assert.Equal(Monday.AddDays(-84), rows[0].Week);
        // Dữ liệu ngày đã tự xoá theo tuần, nhưng điểm Milo TB đã lưu kèm cặp số vẫn còn
        Assert.All(rows, r => Assert.NotNull(r.MiloAvg));
        s.WipeAll();
        Assert.Empty(s.ValidationRows());
    }

    [Fact]
    public void Monday_answers_count_for_last_week()
    {
        Assert.Equal(Monday.AddDays(-7), LocalStore.Who5Week(Monday));
        Assert.Equal(Monday, LocalStore.Who5Week(Monday.AddDays(4)));
    }
}
