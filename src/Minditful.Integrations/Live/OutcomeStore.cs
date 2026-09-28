using System.Globalization;
using Minditful.Core.Engine;

namespace Minditful.Integrations.Live;

/// <summary>
/// Log phản hồi từng lời nhắc (§14 · Log và cá nhân hoá), lưu local dạng 1 dòng/sự kiện:
/// ngày · giây trong ngày · case · loại · điểm mood. Giữ 30 ngày gần nhất.
/// </summary>
public sealed class OutcomeStore(string file)
{
    private readonly object _gate = new();

    public void Append(OutcomeEvent e)
    {
        lock (_gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.AppendAllText(file, string.Join('\t', e.Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ((int)e.T).ToString(CultureInfo.InvariantCulture), e.Case, e.Kind, e.Score) + "\n");
        }
    }

    public IReadOnlyList<OutcomeEvent> Load()
    {
        lock (_gate)
        {
            if (!File.Exists(file)) return [];
            var list = new List<OutcomeEvent>();
            foreach (var line in File.ReadLines(file))
            {
                var p = line.Split('\t');
                if (p.Length < 5
                    || !DateOnly.TryParseExact(p[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
                    || !double.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var t)
                    || !Enum.TryParse<CaseId>(p[2], out var c)
                    || !Enum.TryParse<Outcome>(p[3], out var k)
                    || !int.TryParse(p[4], out var score)) continue;
                list.Add(new OutcomeEvent(day, t, c, k, score));
            }
            return list;
        }
    }

    /// <summary>Gọn file: bỏ sự kiện cũ hơn 30 ngày.</summary>
    public void Trim(DateOnly today)
    {
        lock (_gate)
        {
            var keep = Load().Where(e => e.Day >= today.AddDays(-30)).ToList();
            if (!File.Exists(file)) return;
            File.WriteAllLines(file, keep.Select(e => string.Join('\t', e.Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ((int)e.T).ToString(CultureInfo.InvariantCulture), e.Case, e.Kind, e.Score)));
        }
    }
}
