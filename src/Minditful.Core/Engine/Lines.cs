using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Engine;

/// <summary>Yêu cầu viết câu chính cho 1 case. Chỉ chứa tên case và số liệu — không bao giờ có tiêu đề email/họp/task (§14).</summary>
public sealed record LineRequest(CaseId Case, string CaseName, string Facts, string Action, string Template);

/// <summary>
/// Người dùng gõ câu mà bộ nhận diện từ khoá không hiểu → hỏi LLM (§10 · Chat tự do).
/// <see cref="History"/>: các lượt trước trong cùng thẻ để LLM nối mạch. Với <see cref="CaseId.Talk"/>, <see cref="Facts"/> là số liệu cả ngày.
/// </summary>
public sealed record ChatRequest(CaseId Case, string CaseName, string Facts, string UserText, IReadOnlyList<ChatLine>? History = null, string? Primary = null);

/// <summary>
/// Câu chính của thẻ (§14 · Lời thoại): LLM viết sẵn khi case vào hàng đợi; hết giờ/lỗi thì dùng template.
/// Mỗi case 3–5 biến thể, xoay vòng nên không lặp câu vừa dùng. Biến thể 0 là câu của prototype.
/// Nhãn, số liệu, nút bấm luôn lấy từ template.
/// </summary>
public static class Lines
{
    public const string ChatFallback = "Milo nghe rồi nè. Bạn chọn một nút bên trên giúp Milo nhé.";
    public const string ChatThinking = "…";

    /// <summary>Nhãn nút chính của thẻ chăm sóc (dùng chung cho thẻ và cho AI khi trả lời chat).</summary>
    public static string PrimaryLabel(CaseId c) => c switch
    {
        CaseId.Overtime => "Chốt việc, về thôi",
        CaseId.LunchMissed => "Đi ăn thôi",
        CaseId.NoBreak => "Thở 1 phút",
        CaseId.LowRest => "Nghỉ 15 phút",
        CaseId.HighFragmentation => "Tập trung 30 phút",
        CaseId.EodWrapup or CaseId.EodNudge => "Về thôi",
        _ => "Đồng ý, nghỉ chút",
    };

    private delegate string Line(MiloEngine e, CaseData d);

    private static string Next(MiloEngine e) => e.NextMeeting() is { } n ? $"Mình nghỉ chút trước {n.Subject} nhé?" : "Đứng dậy vươn vai 5 phút với Milo nha?";

    private static readonly Dictionary<CaseId, Line[]> Templates = new()
    {
        [CaseId.MeetingOverload] =
        [
            (e, d) => $"Bạn họp liền {Dur(d.Min)} rồi đó. {Next(e)}",
            (_, d) => $"{Dur(d.Min)} họp liên tục rồi, đầu chắc nóng lắm. Mình đứng dậy duỗi người 5 phút nha?",
            (_, d) => $"Chuỗi họp {Dur(d.Min)} vừa xong. Uống ngụm nước, hít thở vài nhịp với Milo nhé?",
            (_, d) => $"Họp liền {Dur(d.Min)} là nhiều rồi đó. Nghỉ mắt 5 phút rồi làm tiếp nha?",
        ],
        [CaseId.NoBreak] =
        [
            (_, d) => $"Bạn làm liền {Dur(d.Min)} không rời mắt khỏi màn hình. Hít thở 1 phút rồi uống nước nhé?",
            (_, d) => $"{Dur(d.Min)} liền không rời màn hình rồi. Đứng lên đi vài bước, uống nước nhé?",
            (_, d) => $"Mắt bạn làm việc {Dur(d.Min)} liền rồi đó. Nhìn ra xa 1 phút với Milo nha?",
            (_, d) => $"Chuỗi làm liền đã {Dur(d.Min)}. Thở chậm 1 phút rồi quay lại nhé?",
        ],
        [CaseId.LowRest] =
        [
            (_, d) => $"Hôm nay bạn mới nghỉ tổng cộng {d.Rest:0} phút. Nghỉ hẳn 15 phút nhé, Milo canh giờ cho.",
            (_, d) => $"Cả ngày mới nghỉ {d.Rest:0} phút thôi. Đi dạo 15 phút cho đầu nhẹ nhé?",
            (_, d) => $"Nghỉ hôm nay mới {d.Rest:0} phút. Milo giữ chỗ 15 phút, bạn ra ngoài chút nha?",
        ],
        [CaseId.LunchMissed] =
        [
            (e, _) => $"{Hm(e.S.T)} rồi mà bạn chưa rời máy. Đi ăn trưa thôi!",
            (e, _) => $"Đã {Hm(e.S.T)} mà bụng vẫn chưa có gì. Đi ăn trưa thôi nào!",
            (_, _) => "Giờ trưa sắp qua rồi đó. Rời máy đi ăn một bữa tử tế nhé?",
        ],
        [CaseId.Overtime] =
        [
            (_, d) => $"Đã quá giờ về {JsRound(d.Over)} phút rồi. Chốt việc đang dở rồi mình về nhé?",
            (_, d) => $"Quá giờ {JsRound(d.Over)} phút rồi. Ghi lại việc đang dở cho mai rồi về nhé?",
            (_, d) => $"Đã lố {JsRound(d.Over)} phút sau giờ về. Lưu bài, tắt máy, về nghỉ thôi!",
        ],
        [CaseId.HighFragmentation] =
        [
            (_, d) => $"Giờ vừa rồi bạn chuyển việc {d.Sw} lần. Gom việc lại và tắt thông báo 30 phút nhé?",
            (_, d) => $"{d.Sw} lần chuyển việc trong 1 giờ đó. Chọn 1 việc, tắt thông báo 30 phút nhé?",
            (_, d) => $"Giờ qua bị cắt vụn {d.Sw} lần. Gom lại làm từng việc một nha?",
        ],
        [CaseId.CheckIn] =
        [
            (e, _) => $"Bạn tập trung được {Dur(Focus(e))} rồi đó. Uống ngụm nước cho tỉnh nè.",
            (e, _) => $"Đã tập trung được {Dur(Focus(e))}. Giỏi lắm! Duỗi vai một chút nè.",
            (e, _) => $"{Dur(Focus(e))} tập trung rồi đó. Nhấp ngụm nước rồi làm tiếp nha.",
        ],
        [CaseId.CalendarPacked] =
        [
            (_, d) => $"{d.Chain!.Count} cuộc họp nối liền, không có phút nào nghỉ. Chèn 10 phút vào giữa nhé?",
            (_, d) => $"{d.Chain!.Count} cuộc họp nối đuôi nhau, không có khe nghỉ. Milo chèn 10 phút vào giữa nhé?",
            (_, d) => $"Buổi này kín {d.Chain!.Count} cuộc liền. Giữ sẵn 10 phút nghỉ giữa chừng nha?",
        ],
        [CaseId.EodNudge] =
        [
            (_, _) => "Hết 30 phút rồi nè. Mình về nhé?",
            (_, _) => "30 phút thêm đã hết. Lưu việc lại rồi về thôi nhé?",
            (_, _) => "Hết giờ làm thêm rồi nè. Mai làm tiếp, giờ về nghỉ nha?",
        ],
    };

    private static double Focus(MiloEngine e) => Math.Max(e.S.LongestNm, e.S.FocusMinDone);

    public static bool Supports(CaseId c) => Templates.ContainsKey(c);

    public static int VariantCount(CaseId c) => Templates.TryGetValue(c, out var t) ? t.Length : 1;

    public static string Template(MiloEngine e, CaseId c, CaseData d, int variant) =>
        Templates.TryGetValue(c, out var t) ? t[variant % t.Length](e, d) : "";

    /// <summary>Câu chính đang hiện: câu LLM viết sẵn nếu có, không thì template theo biến thể của lần hiện này.</summary>
    public static string Text(MiloEngine e, Episode ep) => ep.Data.Line ?? Template(e, ep.C, ep.Data, ep.Variant);

    /// <summary>Số liệu gửi cho LLM — không có tên cuộc họp, email hay task.</summary>
    public static string Facts(MiloEngine e, CaseId c, CaseData d) => c switch
    {
        CaseId.MeetingOverload => $"vừa họp liền {Dur(d.Min)}" + (e.NextMeeting() is { } n ? $", còn {JsRound((n.Start - e.S.T) / 60)} phút tới cuộc họp sau" : ", chiều nay không còn họp"),
        CaseId.NoBreak => $"đã làm liền {Dur(d.Min)} không có lần nghỉ nào ≥ 5 phút",
        CaseId.LowRest => $"đã làm {Dur(d.Worked)}, tổng nghỉ hôm nay {d.Rest:0} phút",
        CaseId.LunchMissed => $"bây giờ là {Hm(e.S.T)}, chưa rời máy nghỉ trưa",
        CaseId.Overtime => $"đã quá giờ về {JsRound(d.Over)} phút",
        CaseId.HighFragmentation => $"{d.Sw} lần chuyển qua lại giữa các app trong 1 giờ qua",
        CaseId.CheckIn => $"đã tập trung được {Dur(Focus(e))}, tâm trạng đang tốt",
        CaseId.CalendarPacked => $"{d.Chain?.Count ?? 3} cuộc họp nối liền trong buổi, không có phút nghỉ nào",
        CaseId.EodNudge => "đã hết 30 phút làm thêm sau giờ về",
        _ => Catalog.Def(c).Name,
    };

    public static string Action(CaseId c) => c switch
    {
        CaseId.MeetingOverload => "nghỉ chút, thở cùng Milo",
        CaseId.NoBreak => "hít thở 1 phút rồi uống nước",
        CaseId.LowRest => "nghỉ hẳn 15 phút",
        CaseId.LunchMissed => "đi ăn trưa",
        CaseId.Overtime => "chốt việc rồi về",
        CaseId.HighFragmentation => "gom việc, tắt thông báo 30 phút",
        CaseId.CheckIn => "uống ngụm nước",
        CaseId.CalendarPacked => "giữ 10 phút nghỉ trong lịch",
        CaseId.EodNudge => "về nhà nghỉ",
        _ => "",
    };

    /// <summary>
    /// Kiểm tra câu LLM trả về đúng ràng buộc (§14): 1 câu ngắn, không markdown, dưới 25 từ (cho phép tới 30).
    /// Sai thì trả null để dùng template.
    /// </summary>
    /// <param name="maxWords">Câu nhắc/chat trên thẻ tối đa 30 từ; trò chuyện tự do cho phép dài hơn.</param>
    /// <param name="joinLines">true: nối các dòng thành 1 đoạn (trò chuyện); false: chỉ lấy dòng đầu (câu nhắc).</param>
    public static string? Clean(string? raw, int maxWords = 30, bool joinLines = false)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim().Trim('"', '“', '”', '\'', '*', '`').Trim();
        if (s.Contains('\n'))
            s = joinLines
                ? string.Join(' ', s.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim().TrimStart('-', '•', '*', ' ')))
                : s.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        // Loại markdown (tiêu đề, in đậm, khối code) nhưng vẫn cho chữ như "C#" hay "#123"
        if (s.Length < 8 || s.StartsWith('#') || s.Contains("**") || s.Contains("```")) return null;
        var words = s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return words > 0 && words <= maxWords ? s : null;
    }
}
