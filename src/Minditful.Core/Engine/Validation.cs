namespace Minditful.Core.Engine;

/// <summary>
/// WHO-5 Well-Being Index: 5 câu, mỗi câu 0–5, điểm phần trăm = tổng × 4 (0–100). Miễn phí, được kiểm định rộng rãi
/// (vd. Kliem et al., 2025: chuẩn dân số Đức M = 67.6, SD = 23.0). Dùng để kiểm chứng điểm Milo với người dùng thật.
/// Đây là bản dịch làm việc (chưa phải bản tiếng Việt chuẩn hoá), hỏi theo tuần thay cho 2 tuần của bản gốc.
/// </summary>
public static class Who5
{
    public static readonly string[] Items =
    [
        "Tôi thấy vui vẻ, tinh thần tốt",
        "Tôi thấy bình tĩnh, thư thái",
        "Tôi thấy năng động, tràn đầy sức lực",
        "Tôi thức dậy thấy khoẻ khoắn, được nghỉ ngơi đủ",
        "Cuộc sống hằng ngày của tôi có nhiều điều khiến tôi hứng thú",
    ];

    /// <summary>Thang trả lời 0–5 của WHO-5.</summary>
    public static readonly string[] Scale =
        ["Không lúc nào", "Thỉnh thoảng", "Ít hơn nửa thời gian", "Hơn nửa thời gian", "Phần lớn thời gian", "Mọi lúc"];

    public static int Percent(IReadOnlyList<int> answers)
    {
        if (answers.Count != 5 || answers.Any(a => a is < 0 or > 5)) throw new ArgumentException("WHO-5 cần đúng 5 câu trả lời, mỗi câu 0–5");
        return answers.Sum() * 4;
    }
}

public static class Validation
{
    /// <summary>Hệ số tương quan Pearson; null khi chưa đủ 3 cặp hoặc 1 trong 2 dãy không đổi.</summary>
    public static double? Pearson(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        if (x.Count != y.Count || x.Count < 3) return null;
        double mx = x.Average(), my = y.Average(), sxy = 0, sxx = 0, syy = 0;
        for (var i = 0; i < x.Count; i++)
        {
            sxy += (x[i] - mx) * (y[i] - my);
            sxx += (x[i] - mx) * (x[i] - mx);
            syy += (y[i] - my) * (y[i] - my);
        }
        return sxx == 0 || syy == 0 ? null : sxy / Math.Sqrt(sxx * syy);
    }

    /// <summary>Diễn giải theo ngưỡng thường dùng: |r| ≥ .5 mạnh, ≥ .3 vừa, ≥ .1 yếu.</summary>
    public static string Interpret(double? r, int n) => r switch
    {
        null => $"Cần ít nhất 3 tuần có cả 2 số liệu (đang có {n}).",
        >= .5 => "Tương quan mạnh: điểm Milo đi cùng chiều với cảm nhận thật của bạn.",
        >= .3 => "Tương quan vừa: điểm Milo phản ánh khá đúng cảm nhận của bạn.",
        >= .1 => "Tương quan yếu: cần thêm tuần hoặc hiệu chỉnh trọng số.",
        > -.1 => "Chưa thấy liên hệ: cần thêm dữ liệu, hoặc công thức chưa hợp với bạn.",
        _ => "Ngược chiều: điểm Milo đang sai với bạn, cần xem lại trọng số.",
    };
}
