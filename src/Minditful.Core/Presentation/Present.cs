using Minditful.Core.Engine;
using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Presentation;

/// <summary>Tính mọi thứ UI cần vẽ từ trạng thái engine (tương ứng phần render của prototype).</summary>
public static class Present
{
    // ================= Milo =================
    public static Clip VisualClip(MiloEngine e)
    {
        var s = e.S;
        Clip clip;
        if (s.Ep is { } ep) clip = ep.Clip ?? Clip.Idle;
        else if (s.Visit is { } v) clip = v.Clip == Clip.LookAround ? (s.BandIdx >= 2 ? Clip.IdleTired : Clip.LookAround) : v.Clip;
        else if (s.Peek) clip = Clip.HoverPeek;
        else clip = Clip.Gone;
        if (clip == Clip.Idle && s.BandIdx >= 2) clip = Clip.IdleTired;
        return clip;
    }

    public static Pose PoseFor(MiloEngine e, Clip clip)
    {
        var pose = Catalog.ClipPose.TryGetValue(clip, out var p) ? p : Pose.Idle;
        if (clip is Clip.LookAround or Clip.Idle or Clip.StandUp && e.S.BandIdx >= 2) pose = Pose.Tired;
        return pose;
    }

    /// <summary>Thời điểm (giây engine) clip hiện tại bắt đầu — để tính khung hoạt ảnh.</summary>
    public static double ClipStart(MiloEngine e) => e.S.Ep?.PhaseStart ?? 0;

    public static string? Whisper(MiloEngine e)
    {
        if (e.Presence() != PresenceState.Peek) return null;
        var s = e.S;
        return s.BandIdx <= 1 ? $"Hôm nay mọng **{s.Score}** · chạm để xem"
            : s.BandIdx == 2 ? $"Nho hôm nay đang héo dần · {s.Score}"
            : $"Nho hôm nay héo quá · {s.Score} · nghỉ chút nha";
    }

    public static bool TailVisible(MiloEngine e) => e.Presence() is PresenceState.Hidden or PresenceState.Peek;

    public static string? TailBadge(MiloEngine e)
    {
        var s = e.S;
        if (s.BreakUntil is { } bu && s.T < bu) return Math.Ceiling((bu - s.T) / 60) + "'";
        return s.ParkedList.Count > 0 ? s.ParkedList.Count.ToString() : null;
    }

    public static string? DotPill(MiloEngine e) =>
        e.Presence() == PresenceState.Silent && e.S.Queue.Count > 0 && e.S.Ep is null ? $"{e.S.Queue.Count} lời nhắc đang chờ" : null;

    public static bool MiloClickable(MiloEngine e) => e.S.Ep is { FromDot: false } ep && ep.Phase != Phase.Exit;

    public static string ClockState(MiloEngine e) =>
        !e.S.DayStarted ? "trước giờ làm" : e.S.OffDuty ? "đã tan tầm" : e.S.T >= e.Cfg.End ? "ngoài giờ làm" : "trong giờ làm";

    public static (string Phase, int Count, string Label)? Breathe(MiloEngine e)
    {
        if (e.S.Ep is not { Phase: Phase.Breathe } ep) return null;
        var el = (e.S.T - ep.PhaseStart) % 12;
        var ph = el < 4 ? "Hít vào" : el < 8 ? "Giữ" : "Thở ra";
        var n = (int)Math.Max(1, Math.Ceiling(4 - el % 4));
        return (ph, n, $"Thở 4-4-4 · nhịp {Math.Min(ep.Cycles, ep.Cycle + 1)}/{ep.Cycles}");
    }

    public static double BreatheElapsed(MiloEngine e) => e.S.Ep is { Phase: Phase.Breathe } ep ? (e.S.T - ep.PhaseStart) % 12 : 0;

    // ================= chú thích dưới sân khấu =================
    public static Caption Caption(MiloEngine e)
    {
        var s = e.S;
        var pres = e.Presence();
        var g = e.HardGate();
        if (!s.DayStarted) return new("Nghỉ làm", "Máy đang khoá. Lần mở khoá đầu tiên trong ngày sẽ gọi Milo chào buổi sáng.", "Mục 6.1");
        if (s.Ep is { } ep)
        {
            var def = Catalog.Def(ep.C);
            return new(def.Name, PhaseText(e, ep), def.Ref);
        }
        if (pres == PresenceState.Silent)
            return new("Im lặng", $"Cổng {Catalog.GateLabel[g!.Value]} đang đóng. Milo không xuất hiện" + (s.Queue.Count > 0 ? ", lời nhắc đọng thành chấm chờ ở góc." : "."), "Mục 4 · B11");
        if (pres == PresenceState.Off) return new("Nghỉ làm", s.OffDuty ? "Đã tan tầm. Milo nghỉ tới sáng mai." : "Máy đang khoá.", "Mục 3");
        if (s.Visit is not null) return new("Ghé ngang", $"Ghé ngang 8 giây, dáng theo mood {e.CurrentBand.Label.ToLowerInvariant()}, không bóng thoại.", "Mục 9.3");
        if (s.Peek) return new("Ló đầu", "Rê chuột 600ms → Milo ngóc đầu và thì thầm 1 dòng. Bấm để mở dashboard.", "Mục 9.2 · B03");
        if (s.BandIdx >= 2) return new("Ẩn", $"Milo ẩn. Chóp đuôi đang nhạt màu vì mood {e.CurrentBand.Label.ToLowerInvariant()}.", "Mục 9.4 · B10");
        return new("Ẩn", "Milo ẩn, chỉ còn chóp đuôi ở góc. Rê chuột lên đuôi để Milo ló đầu.", "Mục 3 · 9.1");
    }

    private static string PhaseText(MiloEngine e, Episode ep) => ep.Phase switch
    {
        Phase.Enter => $"Vào: {Catalog.ClipName(ep.Clip ?? Clip.ClimbIn)}.",
        Phase.Show => ep.Compact ? "Bạn gõ phím liên tục 5 phút nên Milo chỉ ló đầu với nhãn gọn."
            : ep.C == CaseId.Dashboard ? "Dashboard mở từ Milo. Bấm Milo, X hoặc Esc để đóng."
            : "Ở lại: thẻ đang chờ bạn trả lời." + (e.S.Auto && e.Script is not null ? " Người dùng mẫu sẽ tự bấm sau vài giây." : ""),
        Phase.Breathe => "Thở cùng Milo: vòng tròn phồng 4s, giữ 4s, xẹp 4s. Bấm Dừng lúc nào cũng được.",
        Phase.Thanks => "Cảm ơn rồi leo xuống.",
        Phase.Confirm => "Xác nhận hành động trên lịch/Teams.",
        Phase.Bubble => "Bóng thoại 3 giây, không thẻ, không nút.",
        Phase.Chat => "Milo đọc câu bạn gõ và nhận ra ý định.",
        Phase.Exit => "Ra: " + (ep.Clip switch
        {
            Clip.ClimbOut => "leo xuống, bám mép 1 nhịp, đuôi khuất sau cùng",
            Clip.ClimbOutShort => "leo xuống bản ngắn, ngó lại 1 nhịp",
            Clip.ClimbOutFast => "thụt xuống nhanh 0.7s",
            Clip.RunToCar => "chạy ra xe",
            Clip.DigExhausted => "đào bới kiệt sức (easter egg)",
            _ => "leo xuống",
        }) + ".",
        _ => Catalog.Def(ep.C).Name,
    };

    // ================= thẻ =================
    private static string NextFreeText(MiloEngine e) => e.NextMeeting() is { } n ? "trống tới " + Hm(n.Start) : "trống tới " + Hm(e.Cfg.End);

    public static CardModel Card(MiloEngine e)
    {
        var s = e.S;
        if (s.Ep is not { Card: true } ep || ep.C == CaseId.Dashboard) return CardModel.Empty;
        var c = ep.C;
        var d = ep.Data;
        var def = Catalog.Def(c);
        var low = ep.FromDot;
        if (ep.Compact) return new(CardVariant.Chip, [], SayText: "Milo có lời nhắn · bấm để xem", Low: low);
        if (ep.Phase == Phase.Breathe) return new(CardVariant.Breathe, [], 280, Low: low);
        if (ep.Phase == Phase.Thanks) return new(CardVariant.Say, [], SayText: ep.ThanksText ?? "Cảm ơn đã nghỉ cùng Milo!", Low: low);
        if (ep.Phase == Phase.Bubble)
            return new(CardVariant.Say, [], SayText: c == CaseId.TaskDone
                ? $"Xong #{d.Task!.Id} rồi! Quả nho hôm nay mọng thêm chút."
                : $"{JsRound(d.Min)} phút sâu xong rồi!", Low: low);

        var b = new List<CardBlock>();
        switch (c)
        {
            case CaseId.MorningHello:
            {
                var cal = e.Meetings;
                var inProg = e.InProgress();
                b.Add(new TitleBlock("Chào buổi sáng!"));
                b.Add(new ParagraphBlock(cal.Count > 0
                    ? $"Hôm nay có {cal.Count} cuộc họp, cuộc đầu lúc {Hm(cal.Min(x => x.Start))}. Milo xem qua giúp bạn rồi nè:"
                    : "Hôm nay lịch trống trơn. Milo xem qua giúp bạn rồi nè:", Small: true));
                if (e.Snap.MailAvailable)
                    b.Add(new LineBlock(LeadKind.Square, "", "#0F6CBD", $"**{e.Snap.Unread}** email chưa đọc", "Outlook"));
                if (e.Snap.BoardsAvailable)
                    b.Add(new LineBlock(LeadKind.Square, "", "#0078D4", $"**{inProg}** task đang dở", "Azure Boards"));
                var extra = new List<string>();
                if (e.Snap.BoardsAvailable && inProg / Math.Max(0.1, e.Snap.AvgInProgress) > 1.5)
                    extra.Add($"{inProg} task đang mở, nhiều hơn thường lệ. Chọn 1 việc quan trọng nhất nhé?");
                if (e.Snap.YesterdayScore is { } y) extra.Add($"Hôm qua: {y} điểm");
                if (extra.Count > 0) b.Add(new ParagraphBlock(string.Join(" · ", extra), Small: true));
                b.Add(new ButtonsBlock([new("gotIt", "Đã rõ", ButtonStyle.Dark), new("remindAt", $"Nhắc lúc {HourLabel(e.MorningRemindAt())}", ButtonStyle.Ghost)]));
                break;
            }
            case CaseId.MeetingSoon:
            {
                var ev = d.Ev!;
                var mins = Math.Max(1, (int)Math.Ceiling((ev.Start - s.T) / 60));
                var presenter = ev.Role == "Trình bày";
                b.Add(new TopBlock($"Teams · còn {mins} phút", "#E8E9FB", "#3D3F9E", PillIcon.Bell, $"{Hm(ev.Start)} – {Hm(ev.End)}"));
                b.Add(new TitleBlock(ev.Subject, 16));
                b.Add(new PeopleBlock(ev.People, ev.Role, presenter ? "#FBE7D8" : "#E4E9F7", presenter ? "#8A3F1F" : "#2E4A86"));
                if (ev.Attachment is not null)
                    b.Add(new ParagraphBlock(ep.Opened ? $"Đã mở **{ev.Attachment}**." : $"Milo mở sẵn **{ev.Attachment}** cho bạn nhé?", Small: true));
                var btns = new List<CardButton> { new("join", "Tham gia", ButtonStyle.Teams) };
                if (ev.Attachment is not null) btns.Add(new("open", "Mở slide", ButtonStyle.Ghost, !ep.Opened));
                b.Add(new ButtonsBlock(btns));
                break;
            }
            case CaseId.CalendarPacked:
            {
                var ch = d.Chain!;
                var half = ch[0].Start < T("12:00") ? "sáng" : "chiều";
                b.Add(new TopBlock($"Lịch Outlook · {half} nay kín", "#E1EEFA", "#0B4F8A", Stamp: ep.Held));
                b.Add(new ParagraphBlock(Lines.Text(e, ep)));
                var rows = new List<ScheduleRow>();
                for (var i = 0; i < ch.Count; i++)
                {
                    rows.Add(new(ch[i].Subject, false));
                    if (i == 1 && ep.Held) rows.Add(new("Nghỉ 10' · Milo giữ chỗ", true));
                }
                b.Add(new ScheduleBlock(Hm(ch[0].Start), Hm(ch[^1].End), rows));
                var hold = e.Snap.CanWriteCalendar ? "Giữ chỗ trong lịch" : "Nhắc tôi lúc đó";
                if (!ep.Held) b.Add(new ButtonsBlock([new("accept", hold, ButtonStyle.Dark), new("dismiss", "Thôi", ButtonStyle.Ghost)]));
                break;
            }
            case CaseId.EmailWaiting:
            {
                b.Add(new TopBlock("Outlook · đang chờ bạn", "#E1EEFA", "#0B4F8A"));
                b.Add(new ParagraphBlock($"{d.Count} email hỏi thẳng bạn, chưa trả lời:"));
                foreach (var m in d.List ?? [])
                    b.Add(new LineBlock(LeadKind.Avatar, m.From, m.Color, m.Subject, m.Days >= 2 ? $"{m.Days} ngày" : "hôm qua", m.Days >= 2));
                b.Add(new ButtonsBlock([new("remindAt", "Nhắc tôi lúc " + Hm(e.EmailRemindAt()), ButtonStyle.Dark), new("openMail", "Mở Outlook", ButtonStyle.Ghost)]));
                break;
            }
            case CaseId.StuckTask:
            {
                var tk = d.Task!;
                var sp = e.Snap.Sprint;
                b.Add(new TopBlock("Azure Boards" + (sp is null ? "" : " · " + sp.Name), "#DDEBF7", "#0B4F8A"));
                if (sp is not null)
                    b.Add(new ProgressBlock($"Sprint còn {sp.DaysLeft} ngày", $"**{Fmt(sp.Done)}/{Fmt(sp.Total)}** điểm xong", sp.Total > 0 ? sp.Done / sp.Total : 0));
                b.Add(new LineBlock(LeadKind.IdTag, "#" + tk.Id, "#0B4F8A", tk.Title, PillText: $"dở {tk.Days} ngày", PillBg: "#FBE3C0", PillFg: "#7A4A0C"));
                if (ep.Confirmed)
                    b.Add(new StatusDotBlock($"Teams: Không làm phiền đến {Hm(s.FocusUntil ?? s.T)} · lịch đã chặn"));
                else
                {
                    b.Add(new ParagraphBlock($"Chặn {d.FocusMin} phút tập trung cho #{tk.Id} và bật \"Không làm phiền\" trên Teams?"));
                    b.Add(new ButtonsBlock([new("accept", $"Khoá {d.FocusMin} phút", ButtonStyle.Dark), new("snooze", "Để sau", ButtonStyle.Ghost)]));
                }
                break;
            }
            case CaseId.CheckIn:
                b.Add(new ParagraphBlock(Lines.Text(e, ep)));
                b.Add(new ButtonsBlock([new("thanks", "Cảm ơn Milo", ButtonStyle.Amber)]));
                return new(CardVariant.Card, b, 280, Low: low);
            case CaseId.EodWrapup:
            {
                b.Add(new EyebrowBlock($"{Hm(e.Cfg.End)} · hết giờ làm"));
                b.Add(new TitleBlock("Hôm nay vậy là đủ rồi, về thôi!"));
                b.Add(new TilesBlock([(Dur(e.MeetingMin()), "họp"), ($"{s.AcceptedBreaks} lần", "nghỉ cùng Milo"), (Dur(s.FocusMinDone), "tập trung sâu")]));
                var y = e.Snap.YesterdayScore;
                b.Add(new ParagraphBlock($"Quả nho hôm nay: **{s.Score} điểm**" + (y is { } yy ? $", {(s.Score >= yy ? "mọng hơn" : "héo hơn")} hôm qua ({yy})." : "."), Small: true));
                if (s.FoldedList.Count > 0)
                    b.Add(new ParagraphBlock("Trong ngày còn: " + string.Join(", ", s.FoldedList.Select(f =>
                        Catalog.Def(f.C).Name.ToLowerInvariant() + (f.Data.Min > 0 ? " " + Dur(f.Data.Min) : ""))) + " chưa xử lý.", Small: true));
                if (e.Snap.Tomorrow is { } tm)
                    b.Add(new ParagraphBlock($"Mai {tm.Time} có {tm.Subject} — Milo nhắc lúc mở máy.", Small: true, Color: "#7A6455"));
                b.Add(new ButtonsBlock([new("goHome", "Về thôi", ButtonStyle.Amber), new("extend", "Thêm 30 phút", ButtonStyle.Ghost, s.ExtendedUntil is null)]));
                break;
            }
            case CaseId.EodNudge:
                b.Add(new ParagraphBlock(Lines.Text(e, ep)));
                b.Add(new ButtonsBlock([new("goHome", "Về thôi", ButtonStyle.Amber)]));
                return new(CardVariant.Card, b, 280, Low: low);
        }
        if (def.Kind == CaseKind.Care) b.AddRange(CareBlocks(e, ep));
        return new(CardVariant.Card, b, Low: low);
    }

    private static IEnumerable<CardBlock> CareBlocks(MiloEngine e, Episode ep)
    {
        var s = e.S;
        var c = ep.C;
        var d = ep.Data;
        var def = Catalog.Def(c);
        string label = "", acceptL = "Đồng ý, nghỉ chút";
        var text = Lines.Text(e, ep);
        CardButton? extra = null;
        switch (c)
        {
            case CaseId.MeetingOverload:
                label = "Họp liên tục · " + Dur(d.Min);
                break;
            case CaseId.Overtime:
                label = $"Quá giờ làm · {JsRound(d.Over)} phút";
                acceptL = "Chốt việc, về thôi";
                break;
            case CaseId.LunchMissed:
                label = "Chưa nghỉ trưa";
                acceptL = "Đi ăn thôi";
                if (e.Snap.CanWriteCalendar) extra = new("lunchLock", "Khoá 30' trong lịch", ButtonStyle.Ghost);
                break;
            case CaseId.NoBreak:
                label = "Làm liền · " + Dur(d.Min);
                acceptL = "Thở 1 phút";
                break;
            case CaseId.LowRest:
                label = $"Nghỉ quá ít · {Fmt(d.Rest)} phút";
                acceptL = "Nghỉ 15 phút";
                break;
            case CaseId.HighFragmentation:
                label = $"Bị cắt vụn · {d.Sw} lần/giờ";
                acceptL = "Tập trung 30 phút";
                break;
        }
        yield return new TopBlock(label, def.Bg!, def.Fg!, PillIcon.Clock, NextFreeText(e));
        yield return new ParagraphBlock(text);
        yield return new ButtonsBlock([new("accept", acceptL, ButtonStyle.Amber)]);
        if (extra is not null) yield return new ButtonsBlock([extra]);
        var sub = new List<CardButton>();
        if (e.SnoozeCount(c) < 2) sub.Add(new("snooze", $"Để sau ({e.SnoozeMinutes(c)}p)", ButtonStyle.Ghost));
        sub.Add(new("dismiss", "Không cần", ButtonStyle.Ghost));
        yield return new ButtonsBlock(sub);
        yield return new ChatBlock(ep.Chat);
    }

    private static string HourLabel(double t) => t % 3600 == 0 ? $"{(int)(t / 3600)}h" : Hm(t);
    private static string Fmt(double v) => v == Math.Floor(v) ? ((long)v).ToString() : v.ToString("0.#");

    // ================= dashboard =================
    public static DashboardModel? Dashboard(MiloEngine e)
    {
        var s = e.S;
        if (s.Ep is not { C: CaseId.Dashboard, Card: true } ep) return null;
        var n = e.NextMeeting();
        var today = new DayScore("Nay", s.Score);
        var days = e.Snap.Week.Concat([today]).ToList();
        var y = e.Snap.YesterdayScore;
        var worst = e.Snap.Week.Count > 0 ? e.Snap.Week.MinBy(w => w.Score) : null;
        var yLine = (y is { } yy ? $"Hôm qua {yy}" : "Chưa có dữ liệu hôm qua") + (worst is { Score: < 40 } ? $" · quả vàng là {WeekdayName(worst.Label)} mệt" : "");
        var mail = e.Snap.MailAvailable ? $"{e.WaitingEmails().Count} email" : "— email";
        var tasks = e.Snap.BoardsAvailable ? $"{e.InProgress()} task" : "— task";
        var tiles = new List<(string, string)>
        {
            ($"{e.Meetings.Count(x => x.Start <= s.T)}/{e.Meetings.Count} cuộc", Dur(e.MeetingMin()) + " họp"),
            (mail, "chờ bạn"),
            (tasks, "đang làm"),
        };
        var next = n is null ? null : new DashRow(Hm(n.Start), n.Subject, $"còn {JsRound((n.Start - s.T) / 60)} phút", "#EFE3D0", "#5B4A3C");
        var events = e.Meetings.Select(x => (x.Start, x.Subject, Tag: x.Role))
            .Concat(s.Holds.Select(h => (h.Start, h.Label, Tag: h.Kind == "focus" ? "Tập trung" : "Nghỉ")))
            .OrderBy(x => x.Start)
            .Select(x =>
            {
                var (bg, fg) = x.Tag switch
                {
                    "Trình bày" => ("#FBE7D8", "#8A3F1F"),
                    "Bắt buộc" => ("#E4E9F7", "#2E4A86"),
                    "Nghỉ" => ("#CFE6BC", "#33501A"),
                    "Tập trung" => ("#DDEBF7", "#0B4F8A"),
                    _ => ("#ECE6DB", "#5B4A3C"),
                };
                return new DashRow(Hm(x.Start), x.Item2, x.Tag, bg, fg);
            }).ToList();
        return new DashboardModel(ep.Page, s.Score, BandPhrase(s.BandIdx), yLine, days, tiles, next, s.Vibe, e.Snap.Sprint, events, e.Snap.StatusNote);
    }

    public static string BandPhrase(int idx) =>
        new[] { "Mọng và chín — đang rất ổn", "Mọng và đều — đang cân bằng", "Đang héo dần — nghỉ chút nhé", "Héo quá — hôm nay nặng rồi" }[idx];

    private static string WeekdayName(string label) => label switch
    {
        "T2" => "thứ Hai", "T3" => "thứ Ba", "T4" => "thứ Tư", "T5" => "thứ Năm", "T6" => "thứ Sáu", "T7" => "thứ Bảy", "CN" => "Chủ nhật", _ => label,
    };

    public static string GrapeKey(int score) => score >= 80 ? "ripe" : score >= 60 ? "mid" : score >= 40 ? "pale" : "dry";

    public static readonly IReadOnlyDictionary<string, string[]> GrapeColors = new Dictionary<string, string[]>
    {
        ["ripe"] = ["#C9B6F5", "#8A6BD6", "#4B2F95"],
        ["mid"] = ["#E0D5F8", "#A58FE0", "#6A52B8"],
        ["pale"] = ["#EEE8FB", "#C5B7EC", "#8F7CCB"],
        ["dry"] = ["#FAF1CF", "#DDC787", "#A48A4C"],
    };

    /// <summary>Nhãn penalty cho bảng Mood Engine.</summary>
    public static readonly IReadOnlyDictionary<string, string> PenaltyLabels = new Dictionary<string, string>
    {
        ["meet"] = "Họp", ["chain"] = "Chuỗi họp", ["streak"] = "Làm liền", ["ot"] = "Quá giờ", ["rest"] = "Thiếu nghỉ",
        ["frag"] = "Phân mảnh", ["work"] = "Workload", ["stuck"] = "Task kẹt", ["email"] = "Email chờ", ["stress"] = "Giả lập",
    };
}
