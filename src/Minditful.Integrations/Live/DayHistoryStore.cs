using System.Text.Json;
using Minditful.Core.Engine;

namespace Minditful.Integrations.Live;

/// <summary>Lịch sử quả nho mỗi ngày, lưu local (mục 11 · Lưu cuối ngày). Không gửi đi đâu.</summary>
public sealed class DayHistoryStore(string file)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private readonly object _gate = new();

    public IReadOnlyList<DayRecord> Load()
    {
        lock (_gate)
        {
            if (!File.Exists(file)) return [];
            try { return JsonSerializer.Deserialize<List<DayRecord>>(File.ReadAllText(file)) ?? []; }
            catch (JsonException) { return []; }
        }
    }

    public void Save(DayRecord r)
    {
        lock (_gate)
        {
            var all = Load().Where(x => x.Date != r.Date).Append(r).OrderBy(x => x.Date).TakeLast(120).ToList();
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, JsonSerializer.Serialize(all, Json));
        }
    }

    /// <summary>6 ngày làm việc gần nhất trước hôm nay, dạng nhãn "T2".."CN" cho chùm nho.</summary>
    public IReadOnlyList<DayScore> Week(DateOnly today) =>
        Load().Where(r => r.Date < today).OrderBy(r => r.Date).TakeLast(6).Select(r => new DayScore(Label(r.Date), r.Score)).ToList();

    public int? Yesterday(DateOnly today) => Load().Where(r => r.Date < today).OrderBy(r => r.Date).LastOrDefault()?.Score;

    /// <summary>Trung bình số task đang làm (baseline workload), cần ≥ 5 ngày dữ liệu.</summary>
    public double? AvgInProgress(DateOnly today)
    {
        var xs = Load().Where(r => r.Date < today && r.InProgress > 0).TakeLast(15).Select(r => (double)r.InProgress).ToList();
        return xs.Count >= 5 ? xs.Average() : null;
    }

    public static string Label(DateOnly d) => d.DayOfWeek switch
    {
        DayOfWeek.Monday => "T2", DayOfWeek.Tuesday => "T3", DayOfWeek.Wednesday => "T4", DayOfWeek.Thursday => "T5",
        DayOfWeek.Friday => "T6", DayOfWeek.Saturday => "T7", _ => "CN",
    };
}

public static class BusinessDays
{
    /// <summary>Số ngày làm việc (T2–T6) đã trôi qua từ <paramref name="from"/> tới <paramref name="to"/>.</summary>
    public static int Between(DateTime from, DateTime to)
    {
        if (to <= from) return 0;
        var n = 0;
        for (var d = from.Date.AddDays(1); d <= to.Date; d = d.AddDays(1))
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) n++;
        return n;
    }
}
