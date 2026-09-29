using Minditful.Core.Engine;
using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Scenario;

public enum TourKind
{
    /// <summary>Tua tới 1 mốc của ngày mẫu, kịch bản tự chạy.</summary>
    Milestone,
    /// <summary>Cho Milo làm ngay 1 tình huống (bỏ qua điều kiện).</summary>
    Case,
    /// <summary>Giả vờ Teams báo đang trình chiếu.</summary>
    Presenting,
    /// <summary>Mood realtime: gọi Milo đứng ngoài, người trình bày kéo mức căng thẳng.</summary>
    Mood,
    /// <summary>Tủ đồ: mở tủ đồ trên đầu Milo, phối lần lượt nhiều bộ, kết thúc bằng đồng phục Bosch (host đổi đồ).</summary>
    Wardrobe,
    /// <summary>Milo hài hước: diễn lần lượt cả 9 động tác (host xếp lịch).</summary>
    Meme,
    /// <summary>Dashboard: 4 quả → bảng chi tiết → trang tuần (host bấm lần lượt).</summary>
    Dashboard,
    /// <summary>Bắt đầu khối tập trung: Milo ngủ trên chóp đuôi, rồi tỉnh khi hết giờ.</summary>
    Focus,
}

/// <param name="Title">Tên bước trên bảng điều khiển.</param>
/// <param name="Show">Người xem thấy gì ở góc màn hình.</param>
/// <param name="Say">Gợi ý câu nói cho người trình bày.</param>
/// <param name="Expect">Case Milo sẽ giao trong bước này (để kiểm tra kịch bản phủ đủ mọi case).</param>
public sealed record TourStep(string Title, string Show, string Say, TourKind Kind, string? Time = null, CaseId? Case = null, CaseId[]? Expect = null)
{
    /// <summary>Bước này có Milo xuất hiện rồi rời đi (để tự chạy biết lúc nào sang bước sau).</summary>
    public bool ExpectMilo => Expect is { Length: > 0 };
}

/// <summary>
/// Kịch bản trình diễn: đi lần lượt qua cả 19 tình huống và các tính năng mới trong ~15 phút.
/// Phần đầu theo đúng ngày mẫu Thứ Năm 24/9 (tài liệu mục 13), phần sau cho Milo làm từng case còn lại.
/// Chi tiết lời thoại: docs/KICH-BAN-DEMO.md.
/// </summary>
public static class DemoTour
{
    /// <summary>Khoảng trống an toàn của ngày mẫu (giữa Lịch kín 12:55 và Email chờ 13:10) để bật 1 case.</summary>
    public static readonly double FreeMoment = T("13:02");

    public static readonly TourStep[] Steps =
    [
        new("Chào buổi sáng", "Bám mép → leo lên → \"Hello!\" → bản tin: 4 cuộc họp, 7 email, 6 task, giờ về hôm nay",
            "Mở máy lần đầu trong ngày, Milo leo lên chào và tóm tắt ngày hôm nay. Không cần mở Outlook hay Boards.",
            TourKind.Milestone, "08:58", Expect: [CaseId.MorningHello]),
        new("Sắp họp", "Milo chỉ tay, thẻ Teams \"còn 5 phút · Sprint Planning\" → Mở slide → Tham gia",
            "5 phút trước họp Teams, Milo nhắc và mở sẵn slide vì bạn là người trình bày.",
            TourKind.Milestone, "09:25", Expect: [CaseId.MeetingSoon]),
        new("Đang họp: Milo im lặng", "Milo ẩn hẳn, cả chóp đuôi. Lời nhắc dồn thành chấm \"2 lời nhắc đang chờ\"",
            "Nguyên tắc số 1: Milo không bao giờ chen vào cuộc họp. Nó tự biết từ trạng thái Teams.",
            TourKind.Milestone, "09:30"),
        new("Task kẹt → khoá giờ tập trung", "Hết họp, Milo nhảy vòng cung ra, thẻ Task kẹt #4821 → Khoá 90 phút",
            "Task dở 4 ngày. Milo chặn lịch 90 phút và bật Không làm phiền trên Teams, rồi im lặng tới hết khối.",
            TourKind.Milestone, "10:37", Expect: [CaseId.StuckTask]),
        new("Hết giờ tập trung · Nghỉ quá ít", "Bóng thoại \"90 phút sâu xong rồi!\" → thẻ Nghỉ quá ít → Để sau",
            "Xong khối tập trung Milo khen, rồi nhắc cả buổi sáng bạn mới nghỉ 7 phút.",
            TourKind.Milestone, "12:07", Expect: [CaseId.FocusDone, CaseId.LowRest]),
        new("Lịch kín", "Thẻ lịch mini 3 cuộc họp liền → Giữ chỗ → khối \"Nghỉ 10'\" trượt vào, dấu Đã giữ",
            "Chiều nay 3 cuộc họp nối nhau. Milo giữ sẵn 10 phút nghỉ trong Outlook.",
            TourKind.Milestone, "12:55", Expect: [CaseId.CalendarPacked]),
        new("Email chờ bạn", "Thẻ 3 email hỏi thẳng bạn mà chưa trả lời → Mở Outlook",
            "Chỉ email hỏi thẳng bạn, có dấu hỏi và chưa trả lời. Không phải toàn bộ hộp thư.",
            TourKind.Milestone, "13:10", Expect: [CaseId.EmailWaiting]),
        new("Ló đầu + dashboard 4 quả", "Rê chuột lên đuôi: Milo ló đầu thì thầm điểm → mở 4 quả → Tuần này",
            "Nho là mood, cam là cuộc họp, anh đào là email chờ, táo cắn dở là sprint. Cáo thì thèm nho: sức khoẻ của bạn là chùm nho Milo muốn giữ cho mọng.",
            TourKind.Milestone, "13:14", Expect: [CaseId.Dashboard]),
        new("Họp liên tục → thở 4-4-4", "Sau 3 cuộc họp liền, Milo nhảy ra → Đồng ý → vòng tròn thở 3 nhịp",
            "Hết họp Milo chờ 2 phút cho bạn ổn định rồi mới rủ thở. Mood đang tụt, Milo nhạt màu.",
            TourKind.Milestone, "16:12", Expect: [CaseId.MeetingOverload]),
        new("Task xong", "Milo nhảy tưng \"Xong #4821 rồi!\", màu hồi lại",
            "Kéo task sang Done trên Azure Boards là Milo ăn mừng, điểm mood cộng thêm.",
            TourKind.Milestone, "17:45", Expect: [CaseId.TaskDone]),
        new("Tan tầm", "Thẻ tổng kết ngày, \"Hôm nay thấy sao?\", gợi ý nghỉ giữa 3 cuộc họp ngày mai → Thêm 30 phút",
            "Hết giờ linh hoạt, Milo tổng kết và hỏi cảm nhận. Câu trả lời chỉ lưu trên máy.",
            TourKind.Milestone, "18:00", Expect: [CaseId.EodWrapup]),
        new("Nhắc lại tan tầm → chạy ra xe", "Hết 30 phút làm thêm → Về thôi → Milo chạy ra xe",
            "Milo chỉ cho làm thêm 1 lần. Gõ chat \"về thôi\" cũng được.",
            TourKind.Milestone, "18:31", Expect: [CaseId.EodNudge]),
        new("Chưa nghỉ trưa", "Thẻ màu xanh lá: đi ăn thôi / khoá 30' nghỉ trưa trong lịch",
            "Quá 12:30 mà chưa rời máy, Milo rủ đi ăn.", TourKind.Case, Case: CaseId.LunchMissed, Expect: [CaseId.LunchMissed]),
        new("Làm liền quá lâu", "Thẻ Làm liền → thở 1 phút",
            "Ngồi 2 tiếng không rời máy, Milo rủ thở 1 phút.", TourKind.Case, Case: CaseId.NoBreak, Expect: [CaseId.NoBreak]),
        new("Nhảy việc liên tục", "Thẻ Bị cắt vụn → tập trung 30 phút",
            "Chuyển qua lại giữa các app quá nhiều trong 1 giờ, Milo đề nghị gom việc.", TourKind.Case, Case: CaseId.HighFragmentation, Expect: [CaseId.HighFragmentation]),
        new("Quá giờ", "Thẻ Quá giờ → chốt việc, về thôi",
            "Vẫn làm sau giờ về 30 phút, Milo nhắc chốt việc.", TourKind.Case, Case: CaseId.Overtime, Expect: [CaseId.Overtime]),
        new("Chào hỏi", "Thẻ nhỏ khen \"tập trung được … rồi đó\" → Cảm ơn Milo",
            "Lúc bạn đang làm tốt, Milo thỉnh thoảng hỏi thăm, không đòi gì.", TourKind.Case, Case: CaseId.CheckIn, Expect: [CaseId.CheckIn]),
        new("Giữ giờ tập trung", "Thẻ \"từ 16:15 tới 17:45 bạn trống 1h30\" → Giữ",
            "Tính năng mới: Milo tìm khoảng trống dài nhất trong ngày và giữ chỗ để tập trung, tới giờ tự bật Không làm phiền.",
            TourKind.Case, Case: CaseId.FocusPlan, Expect: [CaseId.FocusPlan]),
        new("Báo cáo tuần", "Thẻ \"Tuần trước của bạn\": điểm TB, giờ họp, ngày mệt nhất + 1 mẹo",
            "Sáng thứ Hai Milo tóm tắt tuần trước. Dữ liệu cá nhân tự xoá theo tuần.",
            TourKind.Case, Case: CaseId.WeekReport, Expect: [CaseId.WeekReport]),
        new("Nghỉ ngắn", "Milo ló lên 5 giây \"Uống ngụm nước nha!\" / \"Đứng dậy vươn vai\", không nút bấm",
            "Nhắc nhẹ sau mỗi 50 phút ngồi máy, không cần trả lời.", TourKind.Case, Case: CaseId.MicroBreak, Expect: [CaseId.MicroBreak]),
        new("Trò chuyện với Milo", "Khung chat mở ra; tự gõ \"tui cũng khá mệt mà còn nhiều task quá\" → Milo trả lời và hiện nút tính năng (Tìm giờ tập trung, Thở 1 phút…)",
            "Ngoài lời nhắc, bạn có thể tự mở trò chuyện (bấm Milo, dashboard, menu khay). Milo hiểu bạn đang mệt và nhiều việc, rồi đề nghị đúng tính năng; bấm mới chạy. AI chỉ nhận con số trong ngày, không nhận nội dung công việc.",
            TourKind.Case, Case: CaseId.Talk, Expect: [CaseId.Talk]),
        new("Có mới (realtime)", "Milo ló lên: \"Chị Linh vừa gửi mail cho bạn\"; ngay sau đó thêm lời mời họp và task mới → gộp thành \"3 thứ mới vừa tới\"",
            "Email mới gửi thẳng cho bạn, lời mời họp mới, task mới được giao: Milo báo ngay khi app đọc thấy. Ở Sandbox, gửi 1 email thật từ tài khoản khác là khoảng 20 giây sau Milo báo. Đang họp hay tập trung thì thẻ chờ ở chấm chờ.",
            TourKind.Case, Case: CaseId.Incoming, Expect: [CaseId.Incoming]),
        new("Dashboard chi tiết", "Mở 4 quả quanh Milo → bảng chi tiết hôm nay (dòng thời gian, Office Vibe) → trang tuần (chùm nho 7 ngày) → đóng",
            "Bấm chóp đuôi là có dashboard. Chi tiết nằm gọn trên đầu Milo, cỡ 1 thẻ: hôm nay họp, tập trung, nghỉ lúc nào; cả tuần mood ra sao.",
            TourKind.Dashboard),
        new("Milo ngủ khi tập trung", "Bắt đầu khối tập trung 30 phút: Milo nằm ngủ trên chóp đuôi, chữ z bay; hết giờ Milo tỉnh dậy \"phút sâu xong rồi!\"",
            "Lúc bạn tập trung, Milo không biến mất mà ngủ trên chóp đuôi: vẫn ở đó, nhưng không làm phiền. Hết giờ thì tỉnh dậy báo xong.",
            TourKind.Focus),
        new("Trốn khi trình chiếu", "Milo và chóp đuôi biến mất hẳn",
            "Đang trình chiếu thì Milo trốn hẳn, không lộ lên màn hình mọi người đang xem.", TourKind.Presenting),
        new("Mood realtime", "Milo đứng ở góc; kéo thanh căng thẳng thì Milo nhạt màu, dáng mệt, có chữ z; bấm Nghỉ / Xong task thì hồi lại",
            "Điểm mood tính lại ngay khi có gì thay đổi. Không popup, Milo chỉ đổi dáng để bạn tự nhận ra.", TourKind.Mood),
        new("Milo hài hước", "Milo diễn lần lượt 9 động tác: slay → liếc xéo → toán bay → ơ kìa ngất → vibe TGIF → đang tải tuần mới → mạng nhện → mọi thứ vẫn ổn → NPC mode",
            "Milo phần lớn dễ thương, lâu lâu hài một chút (tính cách Pha trộn), lấy cảm hứng từ meme và vẽ lại theo Milo. Mỗi động tác gắn với 1 dịp: xong task, email chờ, task kẹt, chiều thứ Sáu, sáng thứ Hai, quá giờ, họp liền… Không bao giờ diễn lúc họp hay trình chiếu.",
            TourKind.Meme),
        new("Tủ đồ & đồng phục Bosch", "Tủ đồ mở trên đầu Milo, Milo phối lần lượt: kính + nơ + cà phê → kính râm + tai nghe + trà sữa → kẹp hoa + khăn + lồng đèn → mũ Noel + lì xì → mũ phù thuỷ → đồng phục Bosch",
            "14 món chia 5 ô, mở khoá bằng thói quen tốt: về đúng giờ, nghỉ cùng Milo, tập trung sâu, và đồ theo mùa. Về đúng giờ 15 ngày liền thì được đồng phục Bosch. Thay đồ ngay trên Milo: chuột phải → Thay đồ.",
            TourKind.Wardrobe),
    ];

    /// <summary>Bước "Milo hài hước": 9 động tác nối nhau (giây thật mỗi động tác).</summary>
    public static readonly (Clip Clip, double Seconds)[] MemeReel =
    [
        (Clip.Slay, 3.2), (Clip.SideEye, 4.5), (Clip.Confused, 4.5), (Clip.Faint, 3.6), (Clip.Vibe, 5),
        (Clip.Loading, 6), (Clip.Cobweb, 6), (Clip.ThisIsFine, 4.5), (Clip.Zombie, 4.5),
    ];

    /// <summary>Bước "Tủ đồ": các bộ Milo phối lần lượt (id món ngăn bởi dấu phẩy), bộ cuối là đồng phục Bosch.</summary>
    public static readonly string[] OutfitReel =
        ["glasses,bowtie,coffee", "sunglasses,headphones,bubbletea", "flower,scarf,lantern", "santa,lixi,glasses", "witch,lantern", "bosch"];

    /// <summary>Cho Milo làm ngay 1 case. Máy đang khoá / đang họp / đã hết ngày thì tua tới khoảng trống 13:02 trước.</summary>
    public static void RunCase(MiloEngine e, CaseId c)
    {
        if (e.HardGate() is not null || e.S.Ended) e.RunTo(FreeMoment);
        e.ForceCase(c);
    }

    /// <summary>Áp 1 bước lên engine. Phần tủ đồ (đổi phụ kiện) do host làm.</summary>
    public static void Apply(MiloEngine e, TourStep s)
    {
        e.CallMilo(false);
        e.SetPresenting(false);
        e.SetStressLevel(0);
        e.StopFocusNow();
        // Bước trước còn mở dashboard / tủ đồ / trò chuyện thì đóng lại trước
        if (e.S.Ep is { C: CaseId.Dashboard or CaseId.Talk, Phase: Phase.Show }) e.UserReply("close");
        switch (s.Kind)
        {
            case TourKind.Milestone:
                var m = DemoScenario.Milestones.First(x => x.Time == s.Time);
                e.RunTo(Math.Max(e.Cfg.DayOpen, T(m.Time) - m.LeadSeconds));
                break;
            case TourKind.Case:
                RunCase(e, s.Case!.Value);
                break;
            case TourKind.Presenting:
                if (e.HardGate() is not null || e.S.Ended) e.RunTo(FreeMoment);
                e.SetPresenting(true);
                break;
            case TourKind.Meme:
                if (e.HardGate() is not null || e.S.Ended) e.RunTo(FreeMoment);
                e.PlayMeme(MemeReel[0].Clip, MemeReel[0].Seconds);
                break;
            case TourKind.Dashboard:
                if (e.HardGate() is not null || e.S.Ended || e.S.Ep is not null) e.RunTo(FreeMoment);
                e.TailClick();
                break;
            case TourKind.Focus:
                if (e.HardGate() is not null || e.S.Ended || e.S.Ep is not null) e.RunTo(FreeMoment);
                e.StartFocusNow(30);
                break;
            case TourKind.Wardrobe:
                if (e.HardGate() is not null || e.S.Ended || e.S.Ep is not null) e.RunTo(FreeMoment);
                e.OpenWardrobe();
                break;
            case TourKind.Mood:
                if (e.HardGate() is not null || e.S.Ended) e.RunTo(FreeMoment);
                e.CallMilo(true);
                break;
        }
    }
}
