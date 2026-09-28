namespace Minditful.Core.Engine;

/// <summary>Thời gian trong engine là số giây tính từ 00:00 của ngày đang chạy.</summary>
public static class Tm
{
    public static double T(string s)
    {
        var p = s.Split(':').Select(double.Parse).ToArray();
        return p[0] * 3600 + p[1] * 60 + (p.Length > 2 ? p[2] : 0);
    }

    public static double T(TimeSpan ts) => ts.TotalSeconds;

    public static string Hm(double s)
    {
        var v = (long)Math.Floor(s);
        return $"{v / 3600:00}:{v % 3600 / 60:00}";
    }

    public static string Dur(double min)
    {
        var m = (long)Math.Round(min, MidpointRounding.AwayFromZero);
        return m >= 60 ? $"{m / 60}h{m % 60:00}" : $"{m} phút";
    }

    public static double Clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));

    /// <summary>Làm tròn kiểu JavaScript Math.round (0.5 → lên).</summary>
    public static int JsRound(double v) => (int)Math.Floor(v + 0.5);
}

/// <summary>mulberry32 — giống hệt prototype để ngày mẫu chạy ra đúng các mốc.</summary>
public sealed class Mulberry32(uint seed)
{
    private uint _s = seed;

    public double Next()
    {
        unchecked
        {
            _s += 0x6D2B79F5;
            uint t = (_s ^ (_s >> 15)) * (1 | _s);
            t = (t + ((t ^ (t >> 7)) * (61 | t))) ^ t;
            return (t ^ (t >> 14)) / 4294967296.0;
        }
    }
}
