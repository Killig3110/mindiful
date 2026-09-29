using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Minditful.Core.Engine;

namespace Minditful.Integrations.Storage;

public enum RetentionPeriod { Week, Month }

/// <summary>
/// Chính sách giữ dữ liệu cá nhân (mục "Storage" trong appsettings.json / .env).
/// Dữ liệu chia theo kỳ (tuần bắt đầu thứ Hai, hoặc tháng). Sang kỳ mới thì tự xoá mọi thứ cũ hơn.
/// </summary>
public sealed class StorageOptions
{
    /// <summary>Week (mặc định) hoặc Month.</summary>
    public string RetentionPeriod { get; set; } = "Week";
    /// <summary>
    /// Giữ thêm 1 kỳ trước (mặc định true) để chùm nho 7 ngày, cá nhân hoá 7 ngày và "so với tuần trước" vẫn có dữ liệu.
    /// false = chỉ giữ đúng kỳ hiện tại (chặt nhất; đầu tuần/đầu tháng các tính năng trên bắt đầu lại từ trống).
    /// </summary>
    public bool KeepPreviousPeriod { get; set; } = true;
    /// <summary>Mẫu mood mỗi bao nhiêu phút.</summary>
    public int MoodSampleMinutes { get; set; } = 15;
    /// <summary>
    /// Kiểm chứng điểm: giữ bao nhiêu tuần cặp số (điểm Milo trung bình tuần, điểm WHO-5). Chỉ 2 con số mỗi tuần,
    /// không có chi tiết ngày nào; cần vài tuần mới tính được tương quan nên giữ lâu hơn dữ liệu ngày.
    /// </summary>
    public int ValidationWeeks { get; set; } = 12;

    public RetentionPeriod Period => Enum.TryParse<RetentionPeriod>(RetentionPeriod, true, out var p) ? p : Storage.RetentionPeriod.Week;

    public string Describe() =>
        (Period == Storage.RetentionPeriod.Month ? "tự xoá theo tháng" : "tự xoá theo tuần") +
        (KeepPreviousPeriod ? $" (giữ {(Period == Storage.RetentionPeriod.Month ? "tháng" : "tuần")} này + {(Period == Storage.RetentionPeriod.Month ? "tháng" : "tuần")} trước)"
                            : $" (chỉ giữ {(Period == Storage.RetentionPeriod.Month ? "tháng" : "tuần")} hiện tại)");
}

public sealed record CleanupResult(DateOnly Cutoff, int Days, int Outcomes, int MoodSamples, int Meetings)
{
    public int Total => Days + Outcomes + MoodSamples + Meetings;
}

/// <summary>
/// Kho dữ liệu local của Milo: 1 file SQLite cho mỗi môi trường (%LOCALAPPDATA%\Minditful\&lt;môi trường&gt;\minditful.db).
/// Không lưu tiêu đề/nội dung email, cuộc họp hay task — chỉ số liệu. Dữ liệu tự xoá theo tuần/tháng (<see cref="StorageOptions"/>).
/// </summary>
public sealed class LocalStore
{
    private const int SchemaVersion = 4;
    private readonly string _cs;
    private readonly StorageOptions _opt;
    private readonly object _gate = new();

    public string Path { get; }

    public LocalStore(string dbPath, StorageOptions? opt = null)
    {
        Path = dbPath;
        _opt = opt ?? new StorageOptions();
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);
        _cs = new SqliteConnectionStringBuilder { DataSource = dbPath, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false }.ToString();
        Migrate();
    }

    private SqliteConnection Open()
    {
        var c = new SqliteConnection(_cs);
        c.Open();
        return c;
    }

    private static string D(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static DateOnly D(string s) => DateOnly.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private void Exec(string sql, params (string, object?)[] args)
    {
        lock (_gate)
        {
            using var c = Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (k, v) in args) cmd.Parameters.AddWithValue(k, v ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }

    private List<T> Query<T>(string sql, Func<SqliteDataReader, T> map, params (string, object?)[] args)
    {
        lock (_gate)
        {
            using var c = Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (k, v) in args) cmd.Parameters.AddWithValue(k, v ?? DBNull.Value);
            using var r = cmd.ExecuteReader();
            var list = new List<T>();
            while (r.Read()) list.Add(map(r));
            return list;
        }
    }

    private void Migrate()
    {
        lock (_gate)
        {
            using var c = Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "PRAGMA user_version;";
            var version = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
            if (version >= SchemaVersion) return;
            cmd.CommandText = """
                PRAGMA journal_mode = WAL;
                CREATE TABLE IF NOT EXISTS day_record (
                    day TEXT PRIMARY KEY, score INTEGER NOT NULL, meeting_min REAL, accepted_breaks INTEGER, focus_min REAL,
                    tasks_done INTEGER, overtime_min REAL, vibe_focus INTEGER, vibe_energy INTEGER, vibe_stress INTEGER,
                    in_progress INTEGER, mood_source TEXT, saved_at TEXT);
                CREATE TABLE IF NOT EXISTS outcome_event (
                    id INTEGER PRIMARY KEY AUTOINCREMENT, day TEXT NOT NULL, t REAL NOT NULL, case_id TEXT NOT NULL, kind TEXT NOT NULL, score INTEGER);
                CREATE INDEX IF NOT EXISTS ix_outcome_day ON outcome_event(day);
                CREATE TABLE IF NOT EXISTS mood_sample (
                    day TEXT NOT NULL, t REAL NOT NULL, score INTEGER, rule_score INTEGER, band TEXT, source TEXT, PRIMARY KEY (day, t));
                CREATE TABLE IF NOT EXISTS meeting_assessment (
                    day TEXT NOT NULL, event_hash TEXT NOT NULL, start TEXT, duration_min REAL, load INTEGER, kind TEXT,
                    recovery_min INTEGER, source TEXT, PRIMARY KEY (day, event_hash));
                CREATE TABLE IF NOT EXISTS day_start (day TEXT PRIMARY KEY, first_act REAL NOT NULL);
                CREATE TABLE IF NOT EXISTS streak (
                    id INTEGER PRIMARY KEY CHECK (id = 1), count INTEGER NOT NULL, best INTEGER NOT NULL, base INTEGER NOT NULL,
                    last_day TEXT, unlocked_item TEXT, unlocked_day TEXT);
                CREATE TABLE IF NOT EXISTS validation_week (
                    week TEXT PRIMARY KEY, milo_avg REAL, who5 INTEGER NOT NULL, saved_at TEXT);
                """;
            cmd.ExecuteNonQuery();
            // v3: người dùng tự đánh giá ngày (0 = chưa trả lời)
            cmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('day_record') WHERE name = 'feeling';";
            if (Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) == 0)
            {
                cmd.CommandText = "ALTER TABLE day_record ADD COLUMN feeling INTEGER NOT NULL DEFAULT 0;";
                cmd.ExecuteNonQuery();
            }
            cmd.CommandText = $"PRAGMA user_version = {SchemaVersion};";
            cmd.ExecuteNonQuery();
        }
    }

    // ================= bản ghi cuối ngày =================
    public void SaveDay(DayRecord r, string moodSource = "Luật") => Exec("""
        INSERT INTO day_record (day, score, meeting_min, accepted_breaks, focus_min, tasks_done, overtime_min, vibe_focus, vibe_energy,
            vibe_stress, in_progress, mood_source, saved_at, feeling)
        VALUES ($day,$score,$meet,$breaks,$focus,$done,$ot,$vf,$ve,$vs,$ip,$src,$at,$feel)
        ON CONFLICT(day) DO UPDATE SET score=$score, meeting_min=$meet, accepted_breaks=$breaks, focus_min=$focus, tasks_done=$done,
            overtime_min=$ot, vibe_focus=$vf, vibe_energy=$ve, vibe_stress=$vs, in_progress=$ip, mood_source=$src, saved_at=$at,
            feeling=CASE WHEN $feel > 0 THEN $feel ELSE feeling END;
        """, ("$day", D(r.Date)), ("$score", r.Score), ("$meet", r.MeetingMin), ("$breaks", r.AcceptedBreaks), ("$focus", r.FocusMin),
        ("$done", r.TasksDone), ("$ot", r.OvertimeMin), ("$vf", r.VibeFocus), ("$ve", r.VibeEnergy), ("$vs", r.VibeStress),
        ("$ip", r.InProgress), ("$src", moodSource), ("$at", DateTime.Now.ToString("s", CultureInfo.InvariantCulture)), ("$feel", r.Feeling));

    public IReadOnlyList<DayRecord> Days(DateOnly from, DateOnly toExclusive) => Query(
        """
        SELECT day, score, meeting_min, accepted_breaks, focus_min, tasks_done, overtime_min, vibe_focus, vibe_energy, vibe_stress, in_progress, feeling
        FROM day_record WHERE day >= $f AND day < $t ORDER BY day
        """, r => new DayRecord(
            D(r.GetString(0)), r.GetInt32(1), r.GetDouble(2), r.GetInt32(3), r.GetDouble(4), r.GetInt32(5), r.GetDouble(6),
            r.GetInt32(7), r.GetInt32(8), r.GetInt32(9), r.GetInt32(10), r.GetInt32(11)),
        ("$f", D(from)), ("$t", D(toExclusive)));

    /// <summary>6 ngày gần nhất trước hôm nay, nhãn "T2".."CN" — cho chùm nho 7 ngày.</summary>
    public IReadOnlyList<DayScore> Week(DateOnly today) =>
        Days(today.AddDays(-60), today).TakeLast(6).Select(r => new DayScore(Label(r.Date), r.Score)).ToList();

    public int? Yesterday(DateOnly today) => Days(today.AddDays(-60), today).LastOrDefault()?.Score;

    /// <summary>Baseline workload: trung bình số task đang làm, cần ≥ 5 ngày dữ liệu.</summary>
    public double? AvgInProgress(DateOnly today)
    {
        var xs = Days(today.AddDays(-30), today).Where(r => r.InProgress > 0).TakeLast(15).Select(r => (double)r.InProgress).ToList();
        return xs.Count >= 5 ? xs.Average() : null;
    }

    public static string Label(DateOnly d) => d.DayOfWeek switch
    {
        DayOfWeek.Monday => "T2", DayOfWeek.Tuesday => "T3", DayOfWeek.Wednesday => "T4", DayOfWeek.Thursday => "T5",
        DayOfWeek.Friday => "T6", DayOfWeek.Saturday => "T7", _ => "CN",
    };

    // ================= log phản hồi (cá nhân hoá 7 ngày) =================
    public void Append(OutcomeEvent e) => Exec("INSERT INTO outcome_event(day,t,case_id,kind,score) VALUES ($d,$t,$c,$k,$s)",
        ("$d", D(e.Day)), ("$t", e.T), ("$c", e.Case.ToString()), ("$k", e.Kind.ToString()), ("$s", e.Score));

    public IReadOnlyList<OutcomeEvent> Outcomes(DateOnly from) => Query(
        "SELECT day,t,case_id,kind,score FROM outcome_event WHERE day >= $f ORDER BY id",
        r => (D(r.GetString(0)), r.GetDouble(1), r.GetString(2), r.GetString(3), r.GetInt32(4)), ("$f", D(from)))
        .Where(x => Enum.TryParse<CaseId>(x.Item3, out _) && Enum.TryParse<Outcome>(x.Item4, out _))
        .Select(x => new OutcomeEvent(x.Item1, x.Item2, Enum.Parse<CaseId>(x.Item3), Enum.Parse<Outcome>(x.Item4), x.Item5)).ToList();

    // ================= giờ bắt đầu làm (giờ linh hoạt) =================
    /// <summary>Ghi giờ bắt đầu làm của hôm nay — chỉ lần đầu, mở lại app không ghi đè.</summary>
    public void SaveDayStart(DateOnly day, double firstAct) =>
        Exec("INSERT OR IGNORE INTO day_start VALUES ($d,$t)", ("$d", D(day)), ("$t", firstAct));

    public double? DayStart(DateOnly day) =>
        Query("SELECT first_act FROM day_start WHERE day = $d", r => r.GetDouble(0), ("$d", D(day))).Cast<double?>().FirstOrDefault();

    // ================= mẫu mood & đánh giá cuộc họp =================
    public void SampleMood(DateOnly day, double t, int score, int ruleScore, string band, string source) => Exec(
        "INSERT OR REPLACE INTO mood_sample VALUES ($d,$t,$s,$r,$b,$src)",
        ("$d", D(day)), ("$t", Math.Floor(t)), ("$s", score), ("$r", ruleScore), ("$b", band), ("$src", source));

    public IReadOnlyList<(double T, int Score, int RuleScore)> MoodSamples(DateOnly day) => Query(
        "SELECT t, score, rule_score FROM mood_sample WHERE day = $d ORDER BY t", r => (r.GetDouble(0), r.GetInt32(1), r.GetInt32(2)), ("$d", D(day)));

    /// <summary>Lưu đánh giá cuộc họp. Id sự kiện được băm — không lưu tiêu đề hay id gốc của Outlook.</summary>
    public void SaveMeeting(DateOnly day, MeetingAssessment a, double start, double durationMin) => Exec(
        "INSERT OR REPLACE INTO meeting_assessment VALUES ($d,$h,$st,$dur,$l,$k,$rec,$src)",
        ("$d", D(day)), ("$h", Hash(a.EventId)), ("$st", Tm.Hm(start)), ("$dur", durationMin), ("$l", a.Load), ("$k", a.Kind),
        ("$rec", a.RecoveryMin), ("$src", a.Source));

    public int MeetingCount(DateOnly day) => Query("SELECT COUNT(*) FROM meeting_assessment WHERE day = $d", r => r.GetInt32(0), ("$d", D(day)))[0];

    private static string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)))[..16];

    // ================= thống kê tuần =================
    public static DateOnly WeekStart(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));

    public WeekStats? WeekStats(DateOnly anyDayInWeek)
    {
        var from = WeekStart(anyDayInWeek);
        var days = Days(from, from.AddDays(7));
        if (days.Count == 0) return null;
        var outcomes = Outcomes(from).Where(o => o.Day < from.AddDays(7)).ToList();
        int Count(Outcome k) => outcomes.Count(o => o.Kind == k);
        var best = days.MaxBy(d => d.Score)!;
        var worst = days.MinBy(d => d.Score)!;
        return new WeekStats(from, days.Count, days.Average(d => d.Score), new DayScore(Label(best.Date), best.Score),
            new DayScore(Label(worst.Date), worst.Score), days.Sum(d => d.MeetingMin), days.Sum(d => d.AcceptedBreaks),
            days.Sum(d => d.FocusMin), days.Sum(d => d.TasksDone), days.Sum(d => d.OvertimeMin),
            Count(Outcome.Shown), Count(Outcome.Accepted), Count(Outcome.Snoozed), Count(Outcome.Dismissed), Count(Outcome.Ignored),
            days.Count(d => d.Feeling == Feeling.Good), days.Count(d => d.Feeling == Feeling.Ok), days.Count(d => d.Feeling == Feeling.Bad));
    }

    // ================= kiểm chứng điểm bằng WHO-5 =================
    /// <summary>Tuần mà câu trả lời WHO-5 nói tới: thứ Hai trả lời cho tuần trước, các ngày khác cho tuần này.</summary>
    public static DateOnly Who5Week(DateOnly today) => WeekStart(today.DayOfWeek == DayOfWeek.Monday ? today.AddDays(-7) : today);

    /// <summary>Lưu điểm WHO-5 (0–100) của 1 tuần kèm điểm Milo trung bình tuần đó (nếu máy còn dữ liệu).</summary>
    public void SaveWho5(DateOnly week, int who5) => Exec("""
        INSERT INTO validation_week (week, milo_avg, who5, saved_at) VALUES ($w, $m, $s, $at)
        ON CONFLICT(week) DO UPDATE SET who5 = $s, milo_avg = COALESCE($m, milo_avg), saved_at = $at;
        """, ("$w", D(WeekStart(week))), ("$m", WeekStats(week)?.AvgScore), ("$s", who5), ("$at", DateTime.Now.ToString("s", CultureInfo.InvariantCulture)));

    /// <summary>Các tuần đã trả lời WHO-5, cũ trước. Điểm Milo được cập nhật lại nếu máy còn dữ liệu ngày của tuần đó.</summary>
    public IReadOnlyList<(DateOnly Week, double? MiloAvg, int Who5)> ValidationRows()
    {
        var rows = Query("SELECT week, milo_avg, who5 FROM validation_week ORDER BY week",
            r => (Week: D(r.GetString(0)), Milo: r.IsDBNull(1) ? (double?)null : r.GetDouble(1), Who5: r.GetInt32(2)));
        return rows.Select(x => (x.Week, WeekStats(x.Week)?.AvgScore ?? x.Milo, x.Who5)).ToList();
    }

    // ================= tủ đồ: chuỗi ngày về đúng giờ =================
    /// <summary>
    /// Ghi kết quả 1 ngày làm việc vào chuỗi về đúng giờ. Gọi lại trong cùng ngày thì tính lại từ đầu ngày đó
    /// (vd. bấm "Về thôi" rồi vẫn làm tiếp tới quá giờ). Chỉ lưu 1 dòng số đếm, không lưu ngày nào về lúc mấy giờ.
    /// </summary>
    /// <returns>Chuỗi hiện tại, chuỗi dài nhất và món vừa mở khoá (nếu có).</returns>
    public (int Streak, int Best, Accessory? Unlocked) RecordDay(DateOnly day, bool onTime)
    {
        lock (_gate)
        {
            var row = Query("SELECT count, best, base, last_day FROM streak WHERE id = 1",
                r => (Count: r.GetInt32(0), Best: r.GetInt32(1), Base: r.GetInt32(2), Last: r.IsDBNull(3) ? null : r.GetString(3))).FirstOrDefault();
            var baseCount = row.Last == D(day) ? row.Base : row.Count;
            var count = onTime ? baseCount + 1 : 0;
            var best = Math.Max(row.Best, count);
            var item = Core.Engine.Wardrobe.NewlyUnlocked(row.Best, best);
            Exec("""
                INSERT INTO streak (id, count, best, base, last_day, unlocked_item, unlocked_day) VALUES (1, $c, $b, $base, $d, $i, $id)
                ON CONFLICT(id) DO UPDATE SET count=$c, best=$b, base=$base, last_day=$d,
                    unlocked_item=COALESCE($i, unlocked_item), unlocked_day=COALESCE($id, unlocked_day);
                """, ("$c", count), ("$b", best), ("$base", baseCount), ("$d", D(day)), ("$i", item?.Id), ("$id", item is null ? null : D(day)));
            return (count, best, item);
        }
    }

    /// <summary>Chuỗi hiện tại cho hôm nay. Món mới chỉ báo vào ngày làm việc ngay sau ngày mở khoá.</summary>
    public WardrobeInfo Wardrobe(DateOnly today)
    {
        var row = Query("SELECT count, best, last_day, unlocked_item, unlocked_day FROM streak WHERE id = 1",
            r => (Count: r.GetInt32(0), Best: r.GetInt32(1), Last: r.IsDBNull(2) ? null : r.GetString(2),
                Item: r.IsDBNull(3) ? null : r.GetString(3), ItemDay: r.IsDBNull(4) ? null : r.GetString(4))).FirstOrDefault();
        if (row.Last is null) return new WardrobeInfo(0, 0);
        var fresh = row.ItemDay is not null && row.ItemDay == row.Last && string.CompareOrdinal(row.Last, D(today)) < 0;
        return new WardrobeInfo(row.Count, row.Best, fresh ? Core.Engine.Wardrobe.Find(row.Item)?.Name : null);
    }

    // ================= dọn & xoá =================
    /// <summary>Ngày đầu kỳ chứa <paramref name="d"/>: thứ Hai của tuần, hoặc ngày 1 của tháng.</summary>
    public DateOnly PeriodStart(DateOnly d) => _opt.Period == RetentionPeriod.Month ? new DateOnly(d.Year, d.Month, 1) : WeekStart(d);

    /// <summary>Mọi dữ liệu trước ngày này sẽ bị xoá.</summary>
    public DateOnly Cutoff(DateOnly today)
    {
        var start = PeriodStart(today);
        if (!_opt.KeepPreviousPeriod) return start;
        return _opt.Period == RetentionPeriod.Month ? start.AddMonths(-1) : start.AddDays(-7);
    }

    /// <summary>
    /// Tự xoá dữ liệu cá nhân của các kỳ đã qua. Chạy lúc mở app và mỗi lần sang ngày mới,
    /// nên đúng thứ Hai (hoặc ngày 1 của tháng) dữ liệu cũ biến mất.
    /// </summary>
    public CleanupResult Cleanup(DateOnly today)
    {
        var cutoff = Cutoff(today);
        var c0 = D(cutoff);
        lock (_gate)
        {
            using var c = Open();
            int Del(string table)
            {
                using var cmd = c.CreateCommand();
                cmd.CommandText = $"DELETE FROM {table} WHERE day < $c";
                cmd.Parameters.AddWithValue("$c", c0);
                return cmd.ExecuteNonQuery();
            }
            Del("day_start");
            // Cặp số kiểm chứng giữ theo ValidationWeeks, không theo kỳ tuần/tháng
            using (var v = c.CreateCommand())
            {
                v.CommandText = "DELETE FROM validation_week WHERE week < $w";
                v.Parameters.AddWithValue("$w", D(WeekStart(today).AddDays(-7 * Math.Max(1, _opt.ValidationWeeks))));
                v.ExecuteNonQuery();
            }
            var result = new CleanupResult(cutoff, Del("day_record"), Del("outcome_event"), Del("mood_sample"), Del("meeting_assessment"));
            if (result.Total > 0)
            {
                using var vac = c.CreateCommand();
                vac.CommandText = "VACUUM;"; // xoá hẳn khỏi file, không để lại trang rác
                vac.ExecuteNonQuery();
            }
            return result;
        }
    }

    public string RetentionText => _opt.Describe();

    /// <summary>Xoá toàn bộ dữ liệu thống kê của môi trường này (nút trong Bảng điều khiển).</summary>
    public void WipeAll()
    {
        Exec("DELETE FROM day_record; DELETE FROM outcome_event; DELETE FROM mood_sample; DELETE FROM meeting_assessment; DELETE FROM day_start; DELETE FROM streak; DELETE FROM validation_week;");
        Exec("VACUUM;");
    }

    /// <summary>Chuyển dữ liệu từ bản cũ (history.json, outcomes.tsv) vào SQLite một lần rồi đổi tên file cũ.</summary>
    public int ImportLegacy(string dir)
    {
        var n = 0;
        var history = System.IO.Path.Combine(dir, "history.json");
        if (File.Exists(history))
        {
            try
            {
                foreach (var r in JsonSerializer.Deserialize<List<DayRecord>>(File.ReadAllText(history)) ?? [])
                {
                    SaveDay(r, "Luật");
                    n++;
                }
                File.Move(history, history + ".imported", overwrite: true);
            }
            catch (JsonException) { }
        }
        var outcomes = System.IO.Path.Combine(dir, "outcomes.tsv");
        if (File.Exists(outcomes))
        {
            foreach (var p in File.ReadLines(outcomes).Select(l => l.Split('\t')).Where(p => p.Length >= 5))
            {
                if (DateOnly.TryParseExact(p[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
                    && double.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var t)
                    && Enum.TryParse<CaseId>(p[2], out var c) && Enum.TryParse<Outcome>(p[3], out var k) && int.TryParse(p[4], out var s))
                {
                    Append(new OutcomeEvent(day, t, c, k, s));
                    n++;
                }
            }
            File.Move(outcomes, outcomes + ".imported", overwrite: true);
        }
        return n;
    }
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
