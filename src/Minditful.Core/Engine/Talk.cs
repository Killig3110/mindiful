using System.Text.RegularExpressions;
using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Engine;

/// <summary>
/// Trò chuyện tự do với Milo (người dùng tự mở, không gắn với lời nhắc nào).
/// Có AI: AI trả lời dựa trên số liệu cả ngày. Không có AI hoặc AI lỗi: trả lời theo từ khoá bằng các câu dưới đây.
/// Câu có dấu hiệu khủng hoảng luôn nhận câu an toàn cố định, không giao cho AI.
/// </summary>
public static class Talk
{
    public const string CrisisReply =
        "Milo nghe bạn và lo cho bạn lắm. Bạn không một mình đâu: nói ngay với người bạn tin tưởng hoặc chuyên gia tâm lý nhé. Nếu đang nguy hiểm, gọi 115.";

    private static readonly Regex Crisis = new(
        "(tự tử|tự sát|muốn chết|không muốn sống|chán sống|kết liễu|tự làm hại|làm hại bản thân|kill myself|suicid)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool IsCrisis(string text) => Crisis.IsMatch(text);

    /// <summary>
    /// Tính năng Milo có thể đề nghị ngay trong chat (AI hoặc từ khoá chọn, người dùng bấm mới chạy).
    /// Mô tả gửi kèm cho AI để AI biết khi nào nên đề nghị.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (string Label, string When)> Actions = new Dictionary<string, (string, string)>
    {
        ["breathe"] = ("Thở 1 phút", "thở chậm 4-4-4 cùng Milo 1 phút; khi mệt, căng thẳng, cáu, hồi hộp"),
        ["break15"] = ("Nghỉ 15 phút", "Milo giữ chỗ 15 phút để đứng dậy, đi dạo, uống nước; khi mệt nhiều hoặc làm lâu không nghỉ"),
        ["focus30"] = ("Tập trung 30 phút", "khoá 30 phút tập trung và bật Không làm phiền; khi bị ngắt quãng, nhảy việc, cần làm cho xong 1 việc"),
        ["planFocus"] = ("Tìm giờ tập trung", "Milo tìm khoảng trống dài nhất trong lịch hôm nay và đề nghị giữ chỗ; khi nhiều việc, sợ không kịp deadline"),
        ["stuck"] = ("Xem task kẹt", "mở task đang kẹt lâu nhất để chặn giờ xử lý; khi nói tới việc tồn, task kẹt, backlog"),
        ["dashboard"] = ("Xem hôm nay", "mở dashboard điểm, họp, email, sprint hôm nay; khi hỏi hôm nay của họ thế nào"),
        ["wardrobe"] = ("Mở tủ đồ", "mở tủ đồ để phối đồ cho Milo; khi muốn đổi đồ, thay trang phục, hỏi Milo mặc gì"),
    };

    /// <summary>Tính năng dùng được lúc này (vd. không có task kẹt thì không đề nghị "Xem task kẹt").</summary>
    public static IReadOnlyList<string> Available(MiloEngine e)
    {
        var s = e.S;
        var list = new List<string> { "breathe", "break15" };
        var busy = e.Ongoing() is not null || s.FocusUntil is { } fu && fu > s.T;
        if (!busy) list.Add("focus30");
        if (!busy && e.FocusSlot() is not null && !s.Holds.Any(h => h.Kind is "focus" or "focusPlan")) list.Add("planFocus");
        if (e.StuckTasks().Count > 0) list.Add("stuck");
        if (s.Ep?.C != CaseId.Dashboard) list.Add("dashboard");
        if (s.Ep?.C != CaseId.Dashboard && e.Snap.Wardrobe is not null) list.Add("wardrobe");
        return list;
    }

    /// <summary>Giữ tối đa 2 mã hợp lệ và đang dùng được, theo thứ tự đề nghị.</summary>
    public static IReadOnlyList<string> Pick(MiloEngine e, IEnumerable<string>? proposed)
    {
        if (proposed is null) return [];
        var ok = Available(e);
        return proposed.Select(a => a.Trim()).Where(ok.Contains).Distinct().Take(2).ToList();
    }

    /// <summary>Không có AI: chọn tính năng theo từ khoá. Mỗi nhóm lấy 1 tính năng đầu tiên dùng được, rồi mới lấy thêm.</summary>
    public static IReadOnlyList<string> Suggest(MiloEngine e, string text)
    {
        var t = text.ToLowerInvariant();
        if (IsCrisis(t)) return [];
        var groups = new List<string[]>();
        if (Has(t, "nhiều việc|nhiều task|quá nhiều|deadline|dí|không kịp|ngập việc|quá tải|dồn việc|chạy deadline")) groups.Add(["planFocus", "focus30", "stuck"]);
        if (Has(t, "mệt|căng|stress|đuối|oải|kiệt sức|nản|chán|cáu|mệt mỏi|áp lực|buồn ngủ")) groups.Add(["breathe", "break15"]);
        if (Has(t, "nhảy việc|phân tâm|bị làm phiền|ngắt quãng|ping|tập trung|xao nhãng")) groups.Add(["focus30", "planFocus"]);
        if (Has(t, "task kẹt|việc tồn|backlog|kẹt|tồn đọng")) groups.Add(["stuck", "planFocus"]);
        if (Has(t, "nghỉ|giải lao|đi dạo|uống nước")) groups.Add(["break15", "breathe"]);
        if (Has(t, "hôm nay|điểm|thế nào|sao rồi|mood")) groups.Add(["dashboard"]);
        if (Has(t, "đổi đồ|thay đồ|mặc gì|tủ đồ|trang phục|outfit|phối đồ|quần áo|mũ|kính")) groups.Insert(0, ["wardrobe"]);
        var ok = Available(e);
        var firsts = groups.Select(g => g.FirstOrDefault(ok.Contains)).Where(a => a is not null).Cast<string>();
        var rest = groups.SelectMany(g => g).Where(ok.Contains);
        return firsts.Concat(rest).Distinct().Take(2).ToList();
    }

    /// <summary>Câu gợi ý bấm nhanh khi mới mở trò chuyện.</summary>
    public static readonly string[] Suggestions = ["Hôm nay mình sao rồi?", "Mình thấy mệt", "Lịch họp còn gì?"];

    /// <summary>Câu mở đầu theo mood hiện tại.</summary>
    public static string Opener(MiloEngine e)
    {
        var s = e.S.Score;
        return e.S.BandIdx switch
        {
            0 => $"Quả nho hôm nay đang mọng, {s} điểm. Có gì vui kể Milo nghe với?",
            1 => $"Hôm nay bạn đang cân bằng, {s} điểm. Muốn tâm sự hay hỏi gì Milo cũng được nè.",
            2 => $"Hôm nay hơi đuối rồi, {s} điểm. Kể Milo nghe bạn đang thấy sao nha.",
            _ => $"Hôm nay nặng quá, mới {s} điểm. Milo ở đây, bạn cứ nói, hoặc mình thở chậm 1 phút trước nha.",
        };
    }

    /// <summary>Số liệu cả ngày gửi cho AI khi trò chuyện (chỉ con số, không tiêu đề hay nội dung công việc).</summary>
    public static string Facts(MiloEngine e)
    {
        var next = e.NextMeeting() is { } n
            ? $"cuộc họp tới lúc {Hm(n.Start)}, dài {JsRound((n.End - n.Start) / 60)} phút"
            : "hôm nay không còn cuộc họp nào";
        return $"điểm cân bằng hiện tại {e.S.Score}/100 ({e.CurrentBand.Label}); {next}; {e.BuildMoodRequest().Facts}";
    }

    private static bool Has(string t, string pattern) =>
        Regex.IsMatch(t, $@"(?<!\w)({pattern})(?!\w)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Trả lời theo từ khoá khi không có AI. Thứ tự: cảm ơn → mệt → họp → nghỉ → về → hỏi về hôm nay → chào → vui.</summary>
    public static string Reply(MiloEngine e, string text)
    {
        var t = text.ToLowerInvariant();
        var s = e.S;
        if (IsCrisis(t)) return CrisisReply;
        if (Has(t, "cảm ơn|cám ơn|thank|thanks|tks")) return "Hihi, Milo luôn ở góc này nè. Cần gì cứ gọi Milo nha.";
        if (Has(t, "mệt|căng|stress|áp lực|đuối|chán|buồn|oải|kiệt sức|quá tải|nản|không vui|không ổn|không khoẻ|không khỏe|mệt mỏi"))
            return $"Nghe là biết hôm nay nặng rồi. {Why(e)} Mình thở chậm 1 phút cùng Milo nha, bấm Thở 1 phút bên dưới.";
        if (Has(t, "họp|lịch|meeting|cuộc họp"))
            return e.NextMeeting() is { } n
                ? $"Cuộc họp tới lúc {Hm(n.Start)}, còn {JsRound((n.Start - s.T) / 60)} phút nữa. Tranh thủ đứng dậy uống nước trước đó nha."
                : "Hôm nay không còn cuộc họp nào nữa. Tận dụng khoảng trống này để tập trung nhé.";
        if (Has(t, "nghỉ|giải lao|uống nước|đi dạo|vươn vai|đứng dậy"))
            return "Nghỉ ngắn 5 tới 10 phút giúp lấy lại sức rõ lắm. Đứng dậy, uống ngụm nước, nhìn ra xa một chút rồi quay lại nha.";
        if (Has(t, "về nhà|tan làm|hết giờ|giờ về|đi về"))
            return s.T >= e.Cfg.End
                ? $"Qua giờ về {JsRound((s.T - e.Cfg.End) / 60)} phút rồi đó. Ghi lại một dòng việc dở cho mai rồi về nghỉ nha."
                : $"Còn {Dur((e.Cfg.End - s.T) / 60)} nữa là tới giờ về {Hm(e.Cfg.End)}. Cố lên, Milo đếm cùng bạn.";
        if (Has(t, "hôm nay|điểm|thế nào|sao rồi|ổn không|mood|năng lượng|tâm trạng"))
            return $"Quả nho hôm nay {s.Score} điểm, {e.CurrentBand.Label.ToLowerInvariant()}. {Why(e)}";
        if (Has(t, "chào|xin chào|hello|hi|hey|alo")) return "Chào bạn! Milo đây. Hôm nay bạn thấy sao, kể Milo nghe với.";
        if (Has(t, "vui|tốt|ổn|khoẻ|khỏe|tuyệt|phấn khởi|yeah"))
            return "Nghe vui ghê! Giữ nhịp này nha, nhớ xen vài lần nghỉ ngắn để chiều vẫn còn sức.";
        return "Milo nghe nè. Không có AI thì Milo chỉ hiểu vài chuyện: điểm hôm nay, lịch họp, lúc bạn mệt hay muốn nghỉ.";
    }

    /// <summary>Lý do lớn nhất làm giảm điểm hôm nay, nói bằng lời.</summary>
    private static string Why(MiloEngine e)
    {
        var s = e.S;
        var top = s.Pen.Where(p => p.Value >= 3).OrderByDescending(p => p.Value).Select(p => p.Key).FirstOrDefault();
        return top switch
        {
            "meet" => $"Bạn đã họp {Dur(e.MeetingMin())} rồi.",
            "chain" => "Mấy cuộc họp nối liền nhau không có khoảng nghỉ.",
            "streak" => $"Bạn làm liền {Dur(e.Streak())} chưa nghỉ.",
            "ot" => $"Bạn đã quá giờ {JsRound(s.OtMin)} phút.",
            "week" => "Cả tuần này làm quá giờ khá nhiều.",
            "rest" => $"Cả ngày mới nghỉ {JsRound(s.Rest)} phút.",
            "frag" => $"Bạn chuyển việc {e.SwitchesHour()} lần trong giờ qua.",
            "work" => "Số việc đang ôm nhiều hơn thường lệ.",
            "stuck" => "Có vài task đang kẹt.",
            "email" => "Có mấy email đang chờ bạn trả lời.",
            "stress" or "self" => "Hôm nay có vẻ là một ngày căng.",
            _ => "Nhịp làm việc hôm nay khá nhẹ nhàng.",
        };
    }
}
