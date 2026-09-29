namespace Minditful.Core.Engine;

/// <summary>Mọi số liệu Mood Engine cần cho 1 lần chấm điểm. Tách khỏi engine để kiểm chứng công thức với dữ liệu bất kỳ.</summary>
public sealed record MoodInputs(
    double MeetingMin = 0, int LongestChain = 0, double StreakMin = 0, double OvertimeMin = 0, bool EarlyStart = false,
    double WorkedMin = 0, double RestMin = 0, int SwitchesHour = 0, int FragThreshold = 8, double WorkloadRatio = 1,
    int StuckTasks = 0, int WaitingEmails = 0, double Stress = 0, int Feeling = 0,
    int AcceptedBreaks = 0, int TasksDone = 0, int FocusBlocks = 0, int ExtraBonus = 0, double WeekOvertimeMin = 0);

/// <summary>
/// Công thức điểm mood (Chỉ số cân bằng) theo mô hình Job Demands–Resources: <b>áp lực</b> (họp, làm liền, quá giờ, nhảy việc…)
/// trừ điểm, <b>nguồn hồi phục</b> (nghỉ, tập trung, xong việc) cộng điểm, bắt đầu từ <see cref="Base"/>.
/// Cơ sở khoa học của từng khoản và ngưỡng: docs/CO-SO-KHOA-HOC.md. Các tính chất được kiểm chứng bằng MoodEvidenceTests.
/// </summary>
public static class MoodModel
{
    /// <summary>
    /// Điểm đầu ngày khi chưa có áp lực nào. Nghiên cứu cho biết <i>chiều</i> tác động và <i>ngưỡng</i>, không cho con số tuyệt đối,
    /// nên điểm gốc và trọng số là lựa chọn thiết kế, được hiệu chỉnh bằng khảo sát WHO-5 khi chạy thử với người dùng thật.
    /// </summary>
    public const int Base = 92;

    /// <summary>Quá giờ cả tuần vượt 8 giờ (tức > 48 giờ/tuần) mới là rủi ro rõ rệt (Kim et al., 2024).</summary>
    public const double WeekOvertimeLimitMin = 8 * 60;

    public static Dictionary<string, double> Penalties(MoodInputs x)
    {
        var p = new Dictionary<string, double>
        {
            ["meet"] = Math.Min(20, Math.Max(0, x.MeetingMin - 180) * 0.1),
            ["chain"] = Math.Min(12, Math.Max(0, x.LongestChain - 2) * 4),
            ["streak"] = Math.Min(15, Math.Max(0, x.StreakMin - 90) * 0.2),
            ["ot"] = Math.Min(25, x.OvertimeMin * 0.33) + (x.EarlyStart ? 5 : 0),
            ["week"] = Math.Min(10, Math.Max(0, x.WeekOvertimeMin - WeekOvertimeLimitMin) * 0.05),
            ["rest"] = x.WorkedMin >= 120 ? Math.Min(15, Math.Max(0, 45 * x.WorkedMin / 480 - x.RestMin) * 0.5) : 0,
            ["frag"] = Math.Min(12, Math.Max(0, x.SwitchesHour - x.FragThreshold) * 2),
            ["work"] = x.WorkloadRatio > 2 ? 10 : x.WorkloadRatio > 1.5 ? 6 : 0,
            ["stuck"] = Math.Min(6, x.StuckTasks * 2),
            ["email"] = Math.Min(4, Math.Max(0, x.WaitingEmails - 2)),
            ["stress"] = x.Stress,
            // Người dùng tự nói cuối ngày: "Mệt" trừ 6 — lời tự đánh giá nặng hơn mọi phỏng đoán
            ["self"] = x.Feeling == Engine.Feeling.Bad ? 6 : 0,
        };
        return p;
    }

    public static int Bonus(MoodInputs x) =>
        Math.Min(12, x.AcceptedBreaks * 3) + Math.Min(6, x.TasksDone * 2) + Math.Min(8, x.FocusBlocks * 4) + Math.Min(3, x.ExtraBonus)
        + (x.Feeling == Engine.Feeling.Good ? 3 : 0);

    public static int Score(MoodInputs x) => Score(Penalties(x).Values.Sum(), Bonus(x));

    public static int Score(double penalties, int bonus) => (int)Tm.Clamp(Tm.JsRound(Base - penalties + bonus), 0, 100);

    /// <summary>Nguồn ngắn cho từng khoản (hiện trong Bộ não Milo). Chi tiết: docs/CO-SO-KHOA-HOC.md.</summary>
    public static readonly IReadOnlyDictionary<string, string> Evidence = new Dictionary<string, string>
    {
        ["meet"] = "Họp trực tuyến gây mệt mỏi thụ động, giảm linh hoạt nhận thức (Nurmi & Pakarinen, 2023); bớt họp giảm stress (Laker et al., 2022)",
        ["chain"] = "Không có khoảng nghỉ giữa các việc làm mệt tích luỹ; nghỉ ngắn tăng sức, giảm mệt (Albulescu et al., 2022)",
        ["streak"] = "Nghỉ ngắn trong giờ làm giảm mệt, nhất là khi khối lượng việc cao (Albulescu et al., 2022; 2025)",
        ["ot"] = "Làm ngoài giờ lấy mất thời gian hồi phục buổi tối (Sonnentag, Cheng & Parker, 2022)",
        ["week"] = "> 48 giờ/tuần liên quan rõ tới rủi ro sức khoẻ tâm thần; 41–54 giờ chưa rõ (Kim et al., 2024)",
        ["rest"] = "Hồi phục trong giờ làm là nguồn lực chống kiệt sức (Sonnentag et al., 2022; Bakker et al., 2023)",
        ["frag"] = "Đa nhiệm và bị ngắt quãng kích hoạt phản ứng stress sinh học (Becker et al., 2023)",
        ["work"] = "Khối lượng việc là áp lực chính trong mô hình JD-R (Bakker, Demerouti & Sanz-Vergel, 2023)",
        ["stuck"] = "Việc dở kéo dài là áp lực công việc (JD-R, Bakker et al., 2023)",
        ["email"] = "Việc chờ phản hồi là áp lực công việc (JD-R, Bakker et al., 2023)",
        ["self"] = "Tự đánh giá của người dùng; kiểm chứng hằng tuần bằng WHO-5 (Kliem et al., 2025)",
        ["bonus"] = "Nguồn hồi phục: nghỉ, tập trung trọn vẹn, xong việc (JD-R; Albulescu et al., 2022)",
    };
}
