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
        if (s.Ep is { FromDot: true }) clip = Clip.Gone; // bấm chấm chờ lúc đang họp: chỉ thẻ bung ra, Milo vẫn ẩn
        else if (s.Ep is { } ep) clip = ep.Clip ?? Clip.Idle;
        else if (s.Visit is { } v) clip = v.Clip == Clip.LookAround ? (s.BandIdx >= 2 ? Clip.IdleTired : Clip.LookAround) : v.Clip;
        else if (s.Peek) clip = Clip.HoverPeek;
        else clip = Clip.Gone;
        if (clip == Clip.Idle && s.BandIdx >= 2) clip = Clip.IdleTired;
        // Clip hài chen vào (bấm liên tục, bảng điều khiển Demo) khi Milo đang đứng ngoài, không đè lúc leo lên/xuống
        if (s.Reaction is { } r && s.T >= r.Start && s.T < r.End && clip != Clip.Gone
            && (s.Ep is null ? s.Visit is { Phase: not VisitPhase.Out } : s.Ep.Phase is Phase.Show or Phase.Bubble or Phase.Chat))
            clip = r.Clip;
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

    /// <summary>Chóp đuôi hiện khi Milo ẩn / ló đầu, và hiện mờ khi đang im lặng (họp, tập trung…) để biết Milo vẫn chạy. Trình chiếu thì ẩn hẳn.</summary>
    public static bool TailVisible(MiloEngine e) => e.Presence() is PresenceState.Hidden or PresenceState.Peek or PresenceState.Silent;

    /// <summary>
    /// Bạn đang trong khối tập trung và Milo không có việc gì: Milo nằm ngủ trên chóp đuôi (thay vì chỉ còn chóp đuôi mờ).
    /// Họp, trình chiếu, toàn màn hình… vẫn chỉ chóp đuôi mờ như cũ.
    /// </summary>
    public static bool Sleeping(MiloEngine e) => e.Presence() == PresenceState.Silent && e.HardGate() == Gate.Focus;

    public static string? SleepText(MiloEngine e) =>
        Sleeping(e) && e.S.FocusUntil is { } until ? $"Bạn đang tập trung tới {Hm(until)} · Milo ngủ để không làm phiền" : null;

    /// <summary>Chóp đuôi mờ: đang im lặng.</summary>
    public static bool TailDimmed(MiloEngine e) => e.Presence() == PresenceState.Silent;

    public static string? TailBadge(MiloEngine e)
    {
        var s = e.S;
        if (s.BreakUntil is { } bu && s.T < bu) return Math.Ceiling((bu - s.T) / 60) + "'";
        return s.ParkedList.Count > 0 ? s.ParkedList.Count.ToString() : null;
    }

    public static string? DotPill(MiloEngine e) =>
        e.Presence() == PresenceState.Silent && e.S.Queue.Count > 0 && e.S.Ep is null ? $"{e.S.Queue.Count} lời nhắc đang chờ" : null;

    /// <summary>Bấm được vào Milo: khi đang có thẻ, hoặc khi Milo đang ghé đứng ở góc (bấm để trò chuyện).</summary>
    public static bool MiloClickable(MiloEngine e) => e.S.Ep is { FromDot: false } ep ? ep.Phase != Phase.Exit : e.S.Visit is not null;

    /// <summary>Dashboard đang mở tủ đồ.</summary>
    public static bool WardrobeOpen(MiloEngine e) => e.S.Ep is { C: CaseId.Dashboard, Card: true, Wardrobe: true };

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
        if (g == Gate.Presenting) return new("Trốn", "Bạn đang trình chiếu. Milo trốn hẳn, kể cả chóp đuôi, tới khi bạn thôi trình chiếu.", "Mở rộng");
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
            : ep.C == CaseId.Talk ? "Trò chuyện tự do: gõ gì cũng được, Milo trả lời bằng AI (hoặc theo từ khoá khi chưa bật AI)."
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
            return new(CardVariant.Say, [], SayText: c switch
            {
                CaseId.TaskDone => $"Xong #{d.Task!.Id} rồi! Quả nho hôm nay mọng thêm chút.",
                CaseId.MicroBreak => MicroText(d.Count),
                _ => $"{JsRound(d.Min)} phút sâu xong rồi!",
            }, Low: low);

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
                if (e.Cfg.IsFlexible) extra.Add($"Hôm nay về lúc {Hm(e.Cfg.End)}");
                if (e.Snap.Wardrobe is { } wd)
                {
                    if (wd.NewItem is { } item)
                        extra.Add(Wardrobe.Items.FirstOrDefault(i => i.Name == item) is { Kind: not UnlockKind.Streak } a
                            ? $"Nhờ bạn {a.Condition}, Milo được tặng {item}! Mở tủ đồ để phối nha."
                            : $"Bạn về đúng giờ {wd.Streak} ngày liền, Milo được tặng {item}!");
                    else if (wd.Streak >= 2) extra.Add($"Chuỗi về đúng giờ: {wd.Streak} ngày");
                }
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
            case CaseId.Incoming:
            {
                var items = d.Incoming ?? [];
                var kinds = items.Select(i => i.Kind).Distinct().ToList();
                var (src, bg, fg) = kinds.Count > 1 ? ("Outlook · Teams · Azure Boards", "#E8EEF8", "#0F3B6E")
                    : kinds.FirstOrDefault() switch
                    {
                        "mail" => ("Outlook · email mới", "#DCEBFA", "#0B4F8A"),
                        "meeting" => ("Teams · lời mời họp mới", "#E6E7FA", "#3F43A8"),
                        _ => ("Azure Boards · task mới giao cho bạn", "#DDEFF8", "#0B5A7A"),
                    };
                b.Add(new TopBlock(src, bg, fg, PillIcon.Bell, items.Count > 1 ? $"{items.Count} thứ mới" : "vừa tới"));
                b.Add(new TitleBlock(items.Count == 1 ? items[0].Kind switch
                {
                    "mail" => $"{items[0].From} vừa gửi mail cho bạn",
                    "meeting" => $"Có lời mời họp mới{(items[0].When is { } w ? " lúc " + w : "")}",
                    _ => "Bạn vừa được giao 1 task mới",
                } : $"{items.Count} thứ mới vừa tới", 15.5));
                foreach (var i in items.TakeLast(3).Reverse())
                    b.Add(i.Kind switch
                    {
                        "mail" => new LineBlock(LeadKind.Avatar, Initials(i.From), "#5471B0", i.Title, Source: i.From),
                        "meeting" => new LineBlock(LeadKind.Square, "", "#5B5FC7", i.Title, Source: (i.When ?? "") + (i.From.Length > 0 ? " · " + i.From : "")),
                        _ => new LineBlock(LeadKind.IdTag, i.From, "#0078D4", i.Title),
                    });
                if (items.Count > 3) b.Add(new ParagraphBlock($"… và {items.Count - 3} thứ khác.", Small: true, Color: "#7A6455"));
                var open = items.LastOrDefault(i => i.Link is not null);
                b.Add(new ButtonsBlock(open is null
                    ? [new("gotIt", "Đã xem", ButtonStyle.Dark)]
                    : [new("open", open.Kind switch { "mail" => "Mở email", "meeting" => "Xem cuộc họp", _ => "Mở task" }, ButtonStyle.Dark), new("gotIt", "Đã xem", ButtonStyle.Ghost)]));
                return new(CardVariant.Card, b, 300, Low: low);
            }
            case CaseId.Talk:
            {
                b.Add(new EyebrowBlock("Trò chuyện với Milo"));
                if (ep.Chat.Count == 0)
                {
                    b.Add(new ParagraphBlock(Talk.Opener(e)));
                    b.Add(new ButtonsBlock(Talk.Suggestions.Select(q => new CardButton("chat", q, ButtonStyle.Ghost, Val: q)).ToList()));
                }
                // Chỉ hiện 4 lượt gần nhất cho thẻ gọn; AI vẫn nhận 6 lượt để nối mạch
                b.Add(ChatOf(ep, 4, focus: true, hint: "Kể Milo nghe…"));
                b.Add(new ParagraphBlock("Chỉ con số trong ngày được gửi cho AI, không gửi tiêu đề hay nội dung công việc.", Small: true, Color: "#9C8672"));
                b.Add(new ButtonsBlock([new("breathe", "Thở 1 phút", ButtonStyle.Amber), new("close", "Xong", ButtonStyle.Ghost)]));
                return new(CardVariant.Card, b, 300, Low: low);
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
                if (e.Cfg.EveningCheck && e.TomorrowChain() is { } tc) b.AddRange(TomorrowBlocks(e, tc));
                else if (e.Snap.Tomorrow is { } tm)
                    b.Add(new ParagraphBlock($"Mai {tm.Time} có {tm.Subject} — Milo nhắc lúc mở máy.", Small: true, Color: "#7A6455"));
                if (e.Cfg.EveningCheck) b.AddRange(FeelBlocks(s));
                b.Add(new ButtonsBlock([new("goHome", "Về thôi", ButtonStyle.Amber), new("extend", "Thêm 30 phút", ButtonStyle.Ghost, s.ExtendedUntil is null)]));
                b.Add(ChatOf(ep)); // gõ "về thôi" / "đồng ý" để về, "chưa" / "bận" để làm thêm
                break;
            }
            case CaseId.EodNudge:
                b.Add(new ParagraphBlock(Lines.Text(e, ep)));
                if (e.Cfg.EveningCheck && s.Feeling == 0) b.AddRange(FeelBlocks(s));
                b.Add(new ButtonsBlock([new("goHome", "Về thôi", ButtonStyle.Amber)]));
                b.Add(ChatOf(ep));
                return new(CardVariant.Card, b, 280, Low: low);
            case CaseId.FocusPlan:
            {
                var end = d.At + d.Min * 60;
                b.Add(new TopBlock("Lịch Outlook · khoảng trống dài nhất", "#DDF0EA", "#1F5A4B", PillIcon.Clock, $"{Hm(d.At)} – {Hm(end)}", Stamp: ep.Held));
                b.Add(new ParagraphBlock($"Từ **{Hm(d.At)}** tới **{Hm(end)}** bạn trống {Dur(d.Min)}. Milo giữ chỗ \"Tập trung\" để không ai chen vào nhé?"));
                if ((e.StuckTasks().FirstOrDefault() ?? s.Tasks.FirstOrDefault(x => !x.Done)) is { } tk)
                    b.Add(new LineBlock(LeadKind.IdTag, "#" + tk.Id, "#0B4F8A", tk.Title, PillText: "hợp để làm", PillBg: "#DDF0EA", PillFg: "#1F5A4B"));
                b.Add(new ParagraphBlock("Tới giờ Milo tự bật Không làm phiền trên Teams.", Small: true, Color: "#7A6455"));
                if (!ep.Held)
                    b.Add(new ButtonsBlock([new("accept", e.Snap.CanWriteCalendar ? $"Giữ {Dur(d.Min)}" : "Nhắc tôi lúc đó", ButtonStyle.Dark), new("dismiss", "Thôi", ButtonStyle.Ghost)]));
                break;
            }
            case CaseId.WeekReport when e.Snap.LastWeek is { } w:
            {
                b.Add(new EyebrowBlock("Thứ Hai · tuần mới"));
                b.Add(new TitleBlock("Tuần trước của bạn"));
                b.Add(new TilesBlock([($"{w.AvgScore:0}", "điểm TB"), (Dur(w.MeetingMin), "họp"), ($"{w.AcceptedBreaks} lần", "nghỉ cùng Milo")]));
                if (w.Best is { } best && w.Worst is { } worst && w.Days > 1)
                    b.Add(new ParagraphBlock($"Tốt nhất {WeekdayName(best.Label)} **{best.Score}**, mệt nhất {WeekdayName(worst.Label)} **{worst.Score}**.", Small: true));
                b.Add(new ParagraphBlock(WeekTip(w)));
                b.Add(new ButtonsBlock([new("gotIt", "Đã rõ", ButtonStyle.Dark), new("week", "Xem chùm nho", ButtonStyle.Ghost)]));
                return new(CardVariant.Card, b, 300, Low: low);
            }
        }
        if (def.Kind == CaseKind.Care) b.AddRange(CareBlocks(e, ep));
        return new(CardVariant.Card, b, Low: low);
    }

    private static string Initials(string name)
    {
        var parts = name.Split([' ', '.', '_', '-', '@', '(', ')'], StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant(),
            _ => char.ToUpperInvariant(parts[0][0]).ToString() + char.ToUpperInvariant(parts[1][0]),
        };
    }

    /// <summary>Khung chat; câu trả lời mới nhất có tính năng đề nghị thì hiện thành nút ngay dưới (chỉ câu mới nhất, tránh nút cũ).</summary>
    private static ChatBlock ChatOf(Episode ep, int take = int.MaxValue, bool focus = false, string? hint = null)
    {
        var last = ep.Chat.LastOrDefault();
        var suggested = last?.Actions is { Count: > 0 } acts && last.Milo != Lines.ChatThinking
            ? acts.Where(Talk.Actions.ContainsKey).Select(a => new CardButton("do", Talk.Actions[a].Label, ButtonStyle.Dark, Val: a)).ToList()
            : null;
        return new ChatBlock(ep.Chat.TakeLast(take).ToList(), focus, hint, suggested);
    }

    private static IEnumerable<CardBlock> CareBlocks(MiloEngine e, Episode ep)
    {
        var s = e.S;
        var c = ep.C;
        var d = ep.Data;
        var def = Catalog.Def(c);
        string label = "", acceptL = Lines.PrimaryLabel(c);
        var text = Lines.Text(e, ep);
        CardButton? extra = null;
        switch (c)
        {
            case CaseId.MeetingOverload:
                label = "Họp liên tục · " + Dur(d.Min);
                break;
            case CaseId.Overtime:
                label = $"Quá giờ làm · {JsRound(d.Over)} phút";
                break;
            case CaseId.LunchMissed:
                label = "Chưa nghỉ trưa";
                if (e.Snap.CanWriteCalendar) extra = new("lunchLock", "Khoá 30' trong lịch", ButtonStyle.Ghost);
                break;
            case CaseId.NoBreak:
                label = "Làm liền · " + Dur(d.Min);
                break;
            case CaseId.LowRest:
                label = $"Nghỉ quá ít · {Fmt(d.Rest)} phút";
                break;
            case CaseId.HighFragmentation:
                label = $"Bị cắt vụn · {d.Sw} lần/giờ";
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
        yield return ChatOf(ep);
    }

    private static IEnumerable<CardBlock> FeelBlocks(DayState s)
    {
        yield return new ParagraphBlock("Hôm nay bạn thấy sao? (chỉ lưu trên máy)", Small: true, Color: "#7A6455");
        CardButton Btn(string v, string label) =>
            new("feel", label, Engine.Feeling.Parse(v) == s.Feeling ? ButtonStyle.Dark : ButtonStyle.Ghost, Val: v);
        yield return new ButtonsBlock([Btn("good", "Vui"), Btn("ok", "Bình thường"), Btn("bad", "Mệt")]);
    }

    private static IEnumerable<CardBlock> TomorrowBlocks(MiloEngine e, IReadOnlyList<CalendarEvent> tc)
    {
        var at = e.S.TomorrowHoldAt ?? tc[1].End;
        yield return new ParagraphBlock($"Mai {Hm(tc[0].Start)}–{Hm(tc[^1].End)} có **{tc.Count} cuộc họp liền**, không phút nghỉ.", Small: true);
        if (e.S.TomorrowHoldAt is null)
            yield return new ButtonsBlock([new("holdTomorrow", e.Snap.CanWriteCalendar ? $"Giữ 10' nghỉ lúc {Hm(at)}" : $"Nhắc nghỉ lúc {Hm(at)}", ButtonStyle.Ghost)]);
        else yield return new StatusDotBlock($"Đã giữ 10' nghỉ mai lúc {Hm(at)}");
    }

    /// <summary>Nghỉ ngắn (micro-break): uống nước / đứng dậy vươn vai. Cơ sở: Albulescu et al. (2022, 2025). Không dùng 20-20-20 vì nghiên cứu 2023 không thấy tác dụng rõ.</summary>
    public static string MicroText(int n) => (n % 4) switch
    {
        0 => "Uống ngụm nước nha! Ngồi máy cả tiếng rồi đó.",
        1 => "Đứng dậy vươn vai 1 phút rồi làm tiếp nhé.",
        2 => "Nhấp ngụm nước rồi làm tiếp nè.",
        _ => "Rời màn hình, đi vài bước cho người nhẹ lại nha.",
    };

    /// <summary>1 mẹo cho tuần mới, chọn theo điểm yếu nhất của tuần trước.</summary>
    public static string WeekTip(WeekStats w)
    {
        if (w.Worst is { Score: < 45 } z)
            return $"{char.ToUpper(WeekdayName(z.Label)[0])}{WeekdayName(z.Label)[1..]} là ngày nặng nhất. Tuần này thử **giữ chỗ nghỉ** giữa chuỗi họp nhé.";
        if (w.OvertimeMin >= 60) return $"Tuần trước quá giờ {Dur(w.OvertimeMin)}. Tuần này thử **về đúng giờ** để Milo có đồ mới nha.";
        if (w.AcceptedBreaks < w.Days) return $"Tuần trước mới nghỉ cùng Milo {w.AcceptedBreaks} lần. Tuần này thử **mỗi ngày 1 lần** nhé.";
        if (w.FocusMin < 60 * w.Days) return "Thử **giữ 1 khối tập trung** mỗi sáng, Milo canh giờ cho.";
        return "Tuần trước rất cân bằng. **Giữ phong độ** nha!";
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
        // Luôn đủ 7 vị trí để chùm nho giữ hình dạng; ngày chưa có dữ liệu là quả mờ (điểm -1)
        var past = e.Snap.Week.TakeLast(6).ToList();
        var days = Enumerable.Repeat(new DayScore("·", -1), 6 - past.Count).Concat(past).Append(today).ToList();
        var y = e.Snap.YesterdayScore;
        var worst = past.Count > 0 ? past.MinBy(w => w.Score) : null;
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
        var events = e.Meetings.Select(x => (x.Start, x.Subject, Tag: x.Role, A: e.Assessment(x.Id)))
            .Concat(s.Holds.Select(h => (h.Start, h.Label, Tag: h.Kind == "focus" ? "Tập trung" : "Nghỉ", A: (MeetingAssessment?)null)))
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
                return new DashRow(Hm(x.Start), x.Item2, x.Tag, bg, fg, x.A?.Load,
                    x.A is { } a ? $"{a.Kind} · nặng {a.Load}/5 · nên nghỉ {a.RecoveryMin}' sau đó · {a.Note} ({a.Source})" : null);
            }).ToList();
        var insight = e.Cfg.MoodMode != MoodMode.Rules && s.MoodInsight is { } mi ? $"Milo nhận xét: {mi.Insight}" : null;
        return new DashboardModel(ep.Page, s.Score, BandPhrase(s.BandIdx), yLine, days, tiles, next, s.Vibe, e.Snap.Sprint, events, e.Snap.StatusNote, insight,
            WeekSummary(e.Snap.ThisWeek, e.Snap.LastWeek));
    }

    /// <summary>Dashboard dạng "vườn trái cây" quanh Milo: 4 quả, mọi chi tiết cũ nằm trong thẻ khi rê chuột.</summary>
    public static FruitDashboard? Fruits(MiloEngine e)
    {
        if (Dashboard(e) is not { } d) return null;
        var s = e.S;
        var vibe = $"Office Vibe · tập trung {d.Vibe.F}/5 · năng lượng {d.Vibe.E}/5 · căng thẳng {d.Vibe.S}/5";

        // --- cuộc họp hôm nay ---
        var total = e.Meetings.Count;
        var done = e.Meetings.Count(m => m.End <= s.T);
        var meetLines = d.Events.Select(r => $"{r.Time}  {r.Name} · {r.Tag}" + (r.Load is { } l ? $" · nặng {l}/5" : "")).ToList();
        if (d.Next is { } nx) meetLines.Add($"Kế tiếp {nx.Time} · {nx.Tag}");
        if (meetLines.Count == 0) meetLines.Add("Hôm nay không có cuộc họp nào.");

        // --- email chờ ---
        var waiting = e.WaitingEmails();
        var mail = e.Snap.MailAvailable
            ? new FruitItem("mail", FruitKind.Cherries, waiting.Count.ToString(), waiting.Count > 0 ? "email chờ" : "đã trả lời", "O", "#0F6CBD",
                $"Anh đào email · {waiting.Count} email chờ bạn",
                waiting.Take(3).Select(m => $"{m.From} · {m.Subject} · {(m.Days >= 2 ? $"{m.Days} ngày" : "hôm qua")}")
                    .Append($"Mỗi quả là 1 email hỏi thẳng bạn, chưa trả lời. Chưa đọc: {e.Snap.Unread}.").ToList(),
                Count: waiting.Count)
            : new FruitItem("mail", FruitKind.Cherries, "—", "email", "O", "#0F6CBD", "Email", ["Chưa kết nối Outlook hoặc thiếu quyền Mail.Read."], Available: false);

        // --- sprint / task ---
        var sp = e.Snap.Sprint;
        var apple = sp is { Total: > 0 }
            ? new FruitItem("sprint", FruitKind.Apple, $"{sp.Done:0.#}/{sp.Total:0.#}", sp.Name, "B", "#0078D4",
                $"Táo sprint · {Math.Round(sp.Done / sp.Total * 100)}% xong",
                [$"Còn {sp.DaysLeft} ngày làm việc · {sp.Done:0.#}/{sp.Total:0.#} điểm", $"{e.InProgress()} task đang làm · {e.StuckTasks().Count} task kẹt",
                 "Sprint càng gần xong táo càng bị cắn nhiều; xong hẳn còn lõi."], Progress: sp.Done / sp.Total)
            : e.Snap.BoardsAvailable
                ? new FruitItem("sprint", FruitKind.Apple, $"{e.InProgress()}", "task đang làm", "B", "#0078D4", "Task đang làm",
                    [$"{s.TasksDone} task xong hôm nay · {e.StuckTasks().Count} task kẹt", "Team chưa chọn sprint hiện tại nên táo tính theo task xong/đang làm."],
                    Progress: s.TasksDone + e.InProgress() == 0 ? 0 : (double)s.TasksDone / (s.TasksDone + e.InProgress()))
                : new FruitItem("sprint", FruitKind.Apple, "—", "sprint", "B", "#0078D4", "Azure Boards", ["Chưa kết nối Azure Boards."], Available: false);

        if (d.Page == DashPage.Today)
        {
            var moodLines = new List<string> { d.Phrase, d.YesterdayLine, vibe };
            if (d.Insight is { } ins) moodLines.Add(ins);
            if (d.StatusNote is { } note) moodLines.Add(note);
            return new FruitDashboard(DashPage.Today, "Hôm nay của bạn",
            [
                new("mood", FruitKind.Grape, s.Score.ToString(), Catalog.Bands[s.BandIdx].Label, "★", "#E8A33D", $"Quả nho hôm nay · {s.Score} điểm", moodLines, Score: s.Score),
                new("meet", FruitKind.Orange, $"{done}/{total}", "cuộc họp", "T", "#5B5FC7", $"Cam họp · {total} cuộc · {Dur(e.MeetingMin())} họp", meetLines, Total: total, Done: done),
                mail,
                apple,
            ]);
        }

        // --- trang tuần: cùng 4 quả, nho thành chùm 7 ngày ---
        var real = d.Days.Where(x => x.Score >= 0).ToList();
        var avg = real.Count > 0 ? (int)Math.Round(real.Average(x => x.Score)) : s.Score;
        var weekLines = (WeekSummary(e.Snap.ThisWeek, e.Snap.LastWeek) ?? "Chưa có thống kê tuần (cần dữ liệu các ngày trước).").Replace("**", "").Split('\n').ToList();
        weekLines.Add(vibe);
        var workday = ((int)e.Day.DayOfWeek + 6) % 7; // T2 = 0
        var weekMeetMin = e.Snap.ThisWeek?.MeetingMin is { } wm && wm > 0 ? wm : e.MeetingMin();
        return new FruitDashboard(DashPage.Week, "Tuần này của bạn",
        [
            new("mood", FruitKind.Bunch, avg.ToString(), "TB tuần", "★", "#E8A33D", $"Chùm nho tuần · trung bình {avg} điểm",
                weekLines.Prepend("Mỗi quả là 1 ngày, quả dưới cùng viền cam là hôm nay. Rê lên từng quả để xem điểm.").ToList(), Score: avg, Days: d.Days),
            new("meet", FruitKind.Orange, Dur(weekMeetMin), "họp tuần", "T", "#5B5FC7", "Cam tuần · 5 múi = 5 ngày làm việc",
                [$"Đã qua {Math.Min(workday, 5)}/5 ngày làm việc", $"Tổng thời gian họp: {Dur(weekMeetMin)}", "Múi đã ăn = ngày đã qua."],
                Total: 5, Done: Math.Min(workday, 5)),
            mail,
            apple,
        ]);
    }

    /// <summary>Bảng chi tiết (bấm "Chi tiết" trên dashboard 4 quả). null khi đang xem 4 quả.</summary>
    public static DetailDashboard? Detail(MiloEngine e)
    {
        if (e.S.Ep is not { Detail: true, Wardrobe: false } ep || Dashboard(e) is not { } d) return null;
        var s = e.S;
        if (ep.Page == DashPage.Today)
        {
            double t0 = e.Cfg.Start, t1 = Math.Max(e.Cfg.End, t0 + 3600);
            double F(double x) => Math.Clamp((x - t0) / (t1 - t0), 0, 1);
            var segs = e.Meetings.Where(m => !s.Skipped.Contains(m.Id) && m.End > t0 && m.Start < t1)
                .Select(m => new TimelineSeg(F(m.Start), F(m.End), e.Assessment(m.Id) is { Load: >= 4 } ? "heavy" : "meet",
                    $"{Hm(m.Start)}–{Hm(m.End)} · {m.Subject}" + (e.Assessment(m.Id) is { } a ? $" · nặng {a.Load}/5" : "")))
                .Concat(s.Holds.Where(h => h.End > t0 && h.Start < t1).Select(h => new TimelineSeg(F(h.Start), F(h.End),
                    h.Kind is "focus" or "focusPlan" ? "focus" : "break", $"{Hm(h.Start)}–{Hm(h.End)} · {h.Label}")))
                .OrderBy(x => x.From).ToList();
            var upcoming = d.Events.Where(r => TimeOnly.TryParse(r.Time, out var tt) && tt.ToTimeSpan().TotalSeconds >= s.T - 60).Take(3).ToList();
            var chips = new List<(string, string)>();
            if (e.Snap.MailAvailable) chips.Add(($"{e.WaitingEmails().Count}", "email chờ"));
            if (e.Snap.BoardsAvailable) chips.Add(($"{e.InProgress()}", e.StuckTasks().Count > 0 ? $"task · {e.StuckTasks().Count} kẹt" : "task đang làm"));
            chips.Add((Dur(Math.Max(s.LongestNm, s.FocusMinDone)), "tập trung"));
            chips.Add(($"{s.AcceptedBreaks}", "lần nghỉ"));
            return new DetailDashboard(DashPage.Today, new DetailToday(
                s.Score, Catalog.Bands[s.BandIdx].Label, d.Phrase, e.Snap.YesterdayScore is { } y ? s.Score - y : null, d.Insight?.Replace("Milo nhận xét: ", ""),
                Hm(t0), Hm(t1), segs, s.T >= t0 && s.T <= t1 ? F(s.T) : null, s.Vibe, upcoming, chips, d.StatusNote), null);
        }
        var real = d.Days.Where(x => x.Score >= 0).ToList();
        var avg = real.Count > 0 ? (int)Math.Round(real.Average(x => x.Score)) : s.Score;
        var w = e.Snap.ThisWeek;
        var prev = e.Snap.LastWeek;
        var stats = new List<(string, string)>
        {
            (Dur(w?.MeetingMin is > 0 ? w.MeetingMin : e.MeetingMin()), "họp"),
            ($"{w?.AcceptedBreaks ?? s.AcceptedBreaks}", "lần nghỉ cùng Milo"),
            (Dur(w?.FocusMin is > 0 ? w.FocusMin : s.FocusMinDone), "tập trung sâu"),
            ($"{w?.TasksDone ?? s.TasksDone}", "task xong"),
        };
        if (w is { OvertimeMin: >= 1 }) stats.Add((Dur(w.OvertimeMin), "quá giờ"));
        var replies = w is null ? [] : new List<(string, int, string)>
        {
            ("Đồng ý", w.Accepted, "#CFE6BC"), ("Để sau", w.Snoozed, "#FBE3C0"), ("Không cần", w.Dismissed, "#F8D7DC"), ("Bỏ qua", w.Ignored, "#E7DED2"),
        }.Where(r => r.Item2 > 0).ToList();
        var feel = w is null || w.FeelGood + w.FeelOk + w.FeelBad == 0 ? null : $"Bạn tự thấy: vui {w.FeelGood} · bình thường {w.FeelOk} · mệt {w.FeelBad} ngày";
        return new DetailDashboard(DashPage.Week, null, new DetailWeek(avg, prev is null ? null : (int)Math.Round(avg - prev.AvgScore), d.Days, stats, replies, feel,
            w is { Days: >= 2 } ? WeekTip(w) : null));
    }

    /// <summary>Thống kê tuần từ dữ liệu local (SQLite), kèm so sánh tuần trước nếu còn giữ.</summary>
    public static string? WeekSummary(WeekStats? w, WeekStats? prev)
    {
        if (w is null) return null;
        var answered = w.Accepted + w.Snoozed + w.Dismissed + w.Ignored;
        var diff = prev is null ? "" : $" ({(w.AvgScore >= prev.AvgScore ? "+" : "")}{w.AvgScore - prev.AvgScore:0} so với tuần trước)";
        return $"{w.Days} ngày · điểm TB **{w.AvgScore:0}**{diff}" +
               (w.Best is { } b && w.Worst is { } z && w.Days > 1 ? $" · tốt nhất {b.Label} {b.Score}, mệt nhất {z.Label} {z.Score}" : "") +
               $"\nHọp {Dur(w.MeetingMin)} · nghỉ cùng Milo {w.AcceptedBreaks} lần · tập trung sâu {Dur(w.FocusMin)} · {w.TasksDone} task xong" +
               (w.OvertimeMin >= 1 ? $" · quá giờ {Dur(w.OvertimeMin)}" : "") +
               (w.FeelGood + w.FeelOk + w.FeelBad > 0 ? $"\nBạn tự thấy: vui {w.FeelGood} · bình thường {w.FeelOk} · mệt {w.FeelBad} ngày" : "") +
               (answered > 0 ? $"\nLời nhắc: {w.Shown} lần hiện · đồng ý {w.Accepted} · để sau {w.Snoozed} · không cần {w.Dismissed} · bỏ qua {w.Ignored}" : "");
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
        ["frag"] = "Phân mảnh", ["work"] = "Workload", ["stuck"] = "Task kẹt", ["email"] = "Email chờ", ["stress"] = "Giả lập", ["llm"] = "Claude",
        ["self"] = "Bạn tự thấy", ["week"] = "Tuần > 48h",
    };
}
