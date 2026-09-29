using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Minditful.Core.Engine;

/// <summary>Loại cuộc họp Milo đoán từ tiêu đề + agenda, ngay trên máy.</summary>
public enum MeetingIntent { Unknown, Present, Decision, Listen, Workshop, OneOnOne }

/// <summary>
/// Đọc tiêu đề và agenda (body preview của lời mời) <b>ngay trên máy</b> bằng từ khoá tiếng Việt (có dấu / không dấu) và tiếng Anh
/// để gắn nhãn cho cuộc họp. Chữ gốc không bao giờ rời máy: luật và AI chỉ nhận <b>nhãn</b> (vd. "Ra quyết định").
/// </summary>
public static class MeetingIntents
{
    // Thứ tự ưu tiên: 1:1 > trình bày > ra quyết định > làm việc nhóm > ngồi nghe
    private static readonly (MeetingIntent Intent, Regex Re)[] Rules =
    [
        (MeetingIntent.OneOnOne, Words(@"1\s*[:\-/]\s*1", "1 on 1", "1on1", "one on one", "one-on-one", "gap rieng", "noi chuyen rieng")),
        (MeetingIntent.Present, Words("demo", "present", "presentation", "thuyet trinh", "trinh bay", "showcase", "pitch", "sprint review",
            "bao cao", "report out", "demo day", "trinh dien")),
        (MeetingIntent.Decision, Words("quyet dinh", "decision", "decide", "approve", "approval", "phe duyet", "chot", "planning", "plan",
            "go/no-go", "go no go", "go-live", "architecture review", "design review", "sign off", "sign-off", "ke hoach", "len ke hoach")),
        (MeetingIntent.Workshop, Words("workshop", "brainstorm", "brainstorming", "retro", "retrospective", "hackathon", "design thinking",
            "grooming", "refinement", "thao luan", "dong nao", "xay dung", "whiteboard")),
        (MeetingIntent.Listen, Words("daily", "standup", "stand-up", "stand up", "sync", "update", "all-hands", "all hands", "town hall", "townhall",
            "training", "dao tao", "webinar", "briefing", "kick-off", "kickoff", "cap nhat", "thong bao", "hop toan cong ty", "onboarding", "info session")),
    ];

    private static Regex Words(params string[] words) =>
        new($@"(?<![\p{{L}}\p{{N}}])({string.Join("|", words.Select(w => w.Contains('\\') ? w : Regex.Escape(w)))})(?![\p{{L}}\p{{N}}])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>Đoán loại từ tiêu đề và agenda. Không khớp gì → Unknown (luật dùng các con số như trước).</summary>
    public static MeetingIntent Classify(string? title, string? agenda = null)
    {
        var text = Plain(title) + " \n " + Plain(agenda);
        foreach (var (intent, re) in Rules)
            if (re.IsMatch(Plain(title))) return intent;   // tiêu đề nói rõ nhất
        foreach (var (intent, re) in Rules)
            if (re.IsMatch(text)) return intent;
        return MeetingIntent.Unknown;
    }

    /// <summary>Loại của 1 cuộc họp: provider đã gắn sẵn (có agenda), không thì đoán từ tiêu đề.</summary>
    public static MeetingIntent Of(CalendarEvent ev) => ev.Intent != MeetingIntent.Unknown ? ev.Intent : Classify(ev.Subject);

    public static string Label(MeetingIntent i) => i switch
    {
        MeetingIntent.Present => "Trình bày",
        MeetingIntent.Decision => "Ra quyết định",
        MeetingIntent.Listen => "Ngồi nghe",
        MeetingIntent.Workshop => "Làm việc nhóm",
        MeetingIntent.OneOnOne => "1:1",
        _ => "",
    };

    /// <summary>1 câu gợi ý trên thẻ Sắp họp theo loại cuộc họp và vai trò của bạn.</summary>
    public static string? Tip(MeetingIntent i, bool youPresent) => i switch
    {
        MeetingIntent.Present when youPresent => "Bạn trình bày: uống ngụm nước, mở sẵn slide, hít sâu 3 nhịp rồi vào.",
        MeetingIntent.Present => "Nghe người khác trình bày: cứ thả lỏng, ghi chú 1–2 ý hay là đủ.",
        MeetingIntent.Decision => "Cuộc này cần chốt quyết định: ghi sẵn 1–2 ý chính trước khi vào.",
        MeetingIntent.Listen => "Chủ yếu ngồi nghe: nhẹ nhàng thôi, không cần chuẩn bị nhiều.",
        MeetingIntent.Workshop => "Làm việc nhóm khá tốn sức: xong Milo sẽ nhắc bạn nghỉ một chút.",
        MeetingIntent.OneOnOne => "1:1: nghĩ sẵn 1 điều bạn muốn hỏi hoặc muốn chia sẻ.",
        _ => youPresent ? "Bạn là người trình bày: mở sẵn tài liệu, hít sâu 3 nhịp rồi vào." : null,
    };

    /// <summary>Chữ thường, bỏ dấu tiếng Việt (đ → d) để khớp cả gõ có dấu lẫn không dấu.</summary>
    private static string Plain(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var d = s.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
