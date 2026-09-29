using System.Text.RegularExpressions;

namespace Minditful.Core.Engine;

public sealed partial class MiloEngine
{
    private static readonly CaseId[] NoRequeue = [CaseId.MeetingSoon, CaseId.TaskDone, CaseId.FocusDone, CaseId.Dashboard, CaseId.MicroBreak, CaseId.Talk];
    private static readonly CaseId[] Parkable =
        [CaseId.EmailWaiting, CaseId.CalendarPacked, CaseId.StuckTask, CaseId.MorningHello, CaseId.EodWrapup, CaseId.EodNudge, CaseId.FocusPlan, CaseId.WeekReport];
    private static bool IsBubble(CaseId c) => c is CaseId.TaskDone or CaseId.FocusDone or CaseId.MicroBreak;

    // ================= điều phối (mục 4) =================
    public void SortQueue()
    {
        // Case do Sandbox ép chạy luôn đứng đầu; mức ưu tiên thật vẫn giữ cho các luật hoãn (gõ phím, sau họp, rời máy)
        S.Queue = S.Queue.OrderBy(q => S.Forced.Contains(q.C) ? 0 : 1).ThenBy(q => q.Pri).ThenByDescending(q => q.Sev)
            .ThenBy(q => (int)q.C).ThenBy(q => q.Enq).ToList();
    }

    private void Reason(CaseId c, string text)
    {
        if (S.Reasons.TryGetValue(c, out var old) && old == text) return;
        S.Reasons[c] = text;
        Log($"{Catalog.Def(c).Name}: {text}", LogKind.Wait);
    }

    private bool BudgetOk()
    {
        S.HourList = S.HourList.Where(x => x > S.T - 3600).ToList();
        return S.T - S.LastProactive >= Cfg.GapBudget && S.HourList.Count < Cfg.PerHour && S.DayCount < Cfg.PerDay;
    }

    public double NextBudgetAt() =>
        Math.Max(S.LastProactive + Cfg.GapBudget, S.HourList.Count >= Cfg.PerHour ? S.HourList[0] + 3600 : 0);

    private void Arbitrate()
    {
        var t = S.T;
        S.Queue = S.Queue.Where(q =>
        {
            if (q.Ttl is { } ttl && t >= ttl)
            {
                Log($"{Catalog.Def(q.C).Name} hết hạn, bỏ khỏi hàng đợi", LogKind.Queue);
                return false;
            }
            return true;
        }).ToList();
        S.ParkedList = S.ParkedList.Where(p => t < p.Until).ToList();

        if (S.Ep is { } cur)
        {
            // P1 được chen ngang thẻ P3–P5 đang chờ phản hồi
            if (cur.Phase == Phase.Show && cur.Pri >= 3 && HardGate() is null)
            {
                SortQueue();
                var first = S.Queue.FirstOrDefault();
                if (first is { Pri: 1 })
                {
                    Log($"Sắp họp chen ngang {Catalog.Def(cur.C).Name} → thẻ cũ quay lại hàng đợi", LogKind.Deliver);
                    Requeue(cur);
                    S.Ep = null;
                    StartEp(first, swap: true);
                }
            }
            return;
        }
        if (S.Visit is not null || S.Peek) return;
        if (HardGate() is not null) return;
        if (S.Queue.Count == 0)
        {
            S.DeferStart = null;
            return;
        }
        SortQueue();
        var top = S.Queue[0];
        if (S.Away && top.Pri > 0)
        {
            Reason(top.C, "người dùng đang rời máy → hoãn, Milo không nói với màn hình trống");
            return;
        }
        if (t < S.SettleUntil && top.Pri > 1)
        {
            Reason(top.C, "vừa hết họp → chờ 2 phút ổn định (B12)");
            return;
        }
        if (top.Need > 0 && GapToNext() < top.Need)
        {
            Reason(top.C, $"sắp tới cuộc họp sau, cần khoảng trống ≥ {top.Need} phút → giữ lại");
            return;
        }
        if (top.Pri > 1 && !S.Forced.Contains(top.C) && Tune(top.C).PreferLongGap && LongestGapLater() is { } later)
        {
            Reason(top.C, $"bạn hay bấm Để sau case này → chờ khoảng trống dài nhất lúc {Tm.Hm(later)} (cá nhân hoá 7 ngày)");
            return;
        }
        var def = Catalog.Def(top.C);
        var exempt = top.Pri <= 1 || def.Exempt;
        if (!exempt && !BudgetOk())
        {
            Reason(top.C, "chưa đủ ngân sách, sớm nhất " + Tm.Hm(NextBudgetAt()));
            return;
        }
        var compact = false;
        if (S.Typing && top.Pri > 1)
        {
            S.DeferStart ??= t;
            if (t - S.DeferStart < Cfg.TypingDefer)
            {
                Reason(top.C, "bạn đang gõ phím → chờ khoảng dừng");
                return;
            }
            compact = true;
        }
        S.DeferStart = null;
        var jump = t < S.JumpUntil;
        if (jump) S.JumpUntil = 0;
        StartEp(top, compact: compact, jump: jump);
    }

    /// <summary>
    /// Khoảng trống giữa các cuộc họp từ giờ tới hết giờ làm. Trả về giờ bắt đầu của khoảng trống dài nhất
    /// nếu nó nằm ở phía sau và dài hơn khoảng trống hiện tại; null nếu bây giờ đã là khoảng trống tốt nhất.
    /// </summary>
    public double? LongestGapLater()
    {
        var t = S.T;
        var end = Math.Max(Cfg.End, t);
        var gaps = new List<(double Start, double Len)>();
        var cursor = t;
        foreach (var m in Meetings.Where(m => m.End > t).OrderBy(m => m.Start))
        {
            if (m.Start > cursor) gaps.Add((cursor, Math.Min(m.Start, end) - cursor));
            cursor = Math.Max(cursor, m.End);
            if (cursor >= end) break;
        }
        if (cursor < end) gaps.Add((cursor, end - cursor));
        if (gaps.Count == 0) return null;
        var current = gaps[0].Start <= t ? gaps[0].Len : 0;
        var best = gaps.MaxBy(g => g.Len);
        return best.Start > t && best.Len > current ? best.Start : null;
    }

    private void Requeue(Episode ep)
    {
        if (NoRequeue.Contains(ep.C) || ep.FromParked) return;
        var m = Mem(ep.C);
        m.Shown = Math.Max(0, m.Shown - 1);
        ep.Item.Enq = S.T;
        S.Queue.Add(ep.Item);
    }

    private void StartEp(QueueItem item, bool compact = false, bool jump = false, bool swap = false, bool fromDot = false, bool fromParked = false)
    {
        var c = item.C;
        var def = Catalog.Def(c);
        S.Queue.Remove(item);
        S.Forced.Remove(c);
        S.Reasons.Clear();
        var ep = new Episode
        {
            Id = ++S.EpSeq, C = c, Item = item, Data = item.Data, Pri = item.Pri, Compact = compact, FromDot = fromDot,
            FromParked = fromParked, PhaseEnd = S.T, T0 = S.T,
        };
        S.Ep = ep;
        var m = Mem(c);
        if (c is not (CaseId.Dashboard or CaseId.Talk))
        {
            m.Shown++;
            m.Half.Add(HalfOf(S.T));
        }
        if (c == CaseId.MicroBreak)
        {
            S.MicroCount++;
            S.LastMicro = S.T;
        }
        ep.Variant = S.VariantCounter.GetValueOrDefault(c);
        S.VariantCounter[c] = ep.Variant + 1;
        Record(c, Outcome.Shown);
        if (item.Pri >= 2 && !def.Exempt && !fromDot)
        {
            S.LastProactive = S.T;
            S.HourList.Add(S.T);
            S.DayCount++;
        }
        // Chỉ nhắc 1 case chăm sóc/lần, các case chăm sóc khác gộp vào tổng kết cuối ngày (§8b)
        if (def.Kind == CaseKind.Care)
        {
            foreach (var o in S.Queue.Where(q => Catalog.Def(q.C).Kind == CaseKind.Care).ToList())
            {
                S.FoldedList.Add(new Folded(o.C, S.T, o.Data));
                Mem(o.C).Keys.Add(o.Key);
                Record(o.C, Outcome.Folded);
                Log($"Gộp {Catalog.Def(o.C).Name} vào tổng kết cuối ngày (chỉ nhắc 1 case/lần)", LogKind.Queue);
            }
            S.Queue = S.Queue.Where(q => Catalog.Def(q.C).Kind != CaseKind.Care).ToList();
        }
        RefreshData(ep);

        var seq = new List<Clip>();
        if (fromDot) { }
        else if (c == CaseId.MorningHello) seq.AddRange([Clip.HangPull, Clip.Hello]);
        else if (swap || S.Peek) seq.Add(Clip.StandUp);
        else if (jump) seq.Add(Clip.JumpIn);
        else if (IsBubble(c)) seq.Add(Clip.PeekIn);
        else if (c == CaseId.Dashboard) seq.AddRange([Clip.ClimbIn, Clip.StandUp]);
        else seq.Add(Clip.ClimbIn);
        if (c == CaseId.EodWrapup) seq.Add(Clip.Stretch);
        ep.Seq = new Queue<Clip>(seq);
        S.Peek = false;
        S.Visit = null;

        var how = fromDot ? "thẻ bung ra từ chấm chờ" : string.Join(" → ", seq.Select(Catalog.ClipName));
        var why = fromDot ? " (bạn bấm, P0)" : item.Pri <= 1 ? $" (P{item.Pri}, miễn ngân sách)" : def.Exempt ? " (miễn ngân sách)" : $" (P{item.Pri})";
        Log($"**Giao {def.Name}**{why} · vào bằng {how}" + (ep.Compact ? " · bản gọn vì bạn gõ phím liên tục 5 phút" : ""), LogKind.Deliver);
        NextEnter();
    }

    private void SetPhase(Phase phase, double sec, Clip? clip = null)
    {
        var ep = S.Ep!;
        ep.Phase = phase;
        ep.PhaseStart = S.T;
        ep.PhaseEnd = S.T + Dt(sec);
        if (clip is not null) ep.Clip = clip;
        ep.CardVer++;
    }

    private void NextEnter()
    {
        var ep = S.Ep!;
        if (ep.Seq.Count > 0)
        {
            var k = ep.Seq.Dequeue();
            SetPhase(Phase.Enter, Catalog.ClipLength(k), k);
        }
        else ShowCard();
    }

    private void RefreshData(Episode ep)
    {
        var d = ep.Data;
        switch (ep.C)
        {
            case CaseId.StuckTask:
                var slot = Math.Min(GapToNext(), (Cfg.End - S.T) / 60);
                d.FocusMin = (int)Math.Max(30, Math.Min(90, Math.Floor(slot / 5) * 5));
                d.Task = StuckTasks().FirstOrDefault() ?? d.Task;
                break;
            case CaseId.EmailWaiting:
                var w = WaitingEmails();
                d.List = w.Take(3).ToList();
                d.Count = w.Count;
                break;
            case CaseId.NoBreak: d.Min = Math.Max(d.Min, Streak()); break;
            case CaseId.Overtime: d.Over = Math.Max(d.Over, S.OtMin); break;
            case CaseId.LowRest:
                d.Rest = S.Rest;
                d.Worked = Worked();
                break;
        }
    }

    /// <summary>
    /// Clip hài thay cho clip dễ thương khi tính cách cho phép (xem <see cref="Joke"/>).
    /// Chỉ ở những lúc Milo đã được phép hiện; không bao giờ lúc họp, trình chiếu, toàn màn hình.
    /// </summary>
    private Clip Meme(CaseId c, Clip normal)
    {
        var meme = c switch
        {
            CaseId.TaskDone or CaseId.FocusDone => Clip.Slay,
            CaseId.EmailWaiting => Clip.SideEye,
            CaseId.StuckTask => Clip.Confused,
            CaseId.MorningHello when Day.DayOfWeek == DayOfWeek.Monday => Clip.Loading,
            CaseId.Overtime => Clip.ThisIsFine,
            CaseId.MeetingOverload => Clip.Zombie,
            _ => normal,
        };
        return meme != normal && Joke() ? meme : normal;
    }

    /// <summary>Dịp này có diễn hài không: Hài hước luôn có, Pha trộn thỉnh thoảng, Dễ thương không bao giờ.</summary>
    private bool Joke() => Cfg.Personality switch
    {
        Personality.Funny => true,
        Personality.Mixed => S.Rnd.Next() < Catalog.MixedJokeChance,
        _ => false,
    };

    private static Clip StayClip(CaseId c)
    {
        var def = Catalog.Def(c);
        if (def.Stay is { } s) return s;
        if (def.Kind == CaseKind.Care || c is CaseId.CalendarPacked or CaseId.StuckTask) return Clip.Reminder;
        if (c == CaseId.EmailWaiting) return Clip.Point;
        return Clip.Greet;
    }

    private void ShowCard()
    {
        var ep = S.Ep!;
        var c = ep.C;
        var def = Catalog.Def(c);
        if (IsBubble(c))
        {
            ep.Card = true;
            SetPhase(Phase.Bubble, c == CaseId.MicroBreak ? 5 : 3.2, c == CaseId.MicroBreak ? Clip.Greet : Meme(c, Clip.Celebrate));
            return;
        }
        ep.Card = true;
        var to = def.Timeout > 0 ? def.Timeout : def.Kind == CaseKind.Care ? Catalog.CareTimeout : 0;
        SetPhase(Phase.Show, 0, c == CaseId.Dashboard ? Clip.Idle : Meme(c, StayClip(c)));
        ep.PhaseEnd = S.T + (to > 0 ? to : 1e9); // giờ chờ tính bằng giây thật, kể cả khi tua
        ep.AutoPlan = new Queue<(double, string)>(
            (S.Auto || S.Instant) && Script is not null && Script.AutoReplies.TryGetValue(c, out var plan) ? plan : []);
    }

    private void ExitEp(Clip clip, string? note = null)
    {
        var ep = S.Ep!;
        ep.Card = false;
        ep.ExitClip = clip;
        if (ep.FromDot)
        {
            FinishEp();
            return;
        }
        if (clip == Clip.ClimbOut && S.BandIdx == 3 && S.Rnd.Next() < 0.04) clip = Clip.DigExhausted;
        SetPhase(Phase.Exit, Catalog.ClipLength(clip), clip);
        if (note is not null) Log(note, LogKind.User);
    }

    private void FinishEp()
    {
        var ep = S.Ep!;
        S.Ep = null;
        S.LastEpEnd = S.T;
        S.NextVisit = S.T + RandMin(Cfg.VisitMinMinutes, Cfg.VisitMaxMinutes);
        if (ep.GoHome)
        {
            S.OffDuty = true;
            Log($"Sang Nghỉ làm. Lưu quả nho của ngày: {S.Score} điểm", LogKind.Gate);
            Raise(new MiloAction.DayClosed(BuildDayRecord()));
        }
    }

    private void Park()
    {
        var ep = S.Ep!;
        var def = Catalog.Def(ep.C);
        if (def.Kind == CaseKind.Care || Parkable.Contains(ep.C))
        {
            S.ParkedList.Add(new Parked(ep.C, ep.Item, S.T + Cfg.ParkTtl));
            Mem(ep.C).SnoozedUntil = S.T + Cfg.ParkTtl;
        }
        Mem(ep.C).Keys.Add(ep.Item.Key);
        Record(ep.C, Outcome.Ignored);
        var to = def.Timeout > 0 ? def.Timeout : Catalog.CareTimeout;
        ExitEp(Clip.ClimbOutShort, $"Không ai trả lời {to} giây → thẻ thu vào, chấm \"1\" trên đuôi giữ 30 phút");
    }

    private void AdvanceEp()
    {
        var ep = S.Ep!;
        switch (ep.Phase)
        {
            case Phase.Enter: NextEnter(); break;
            case Phase.Show:
                if (ep.C == CaseId.MeetingSoon)
                {
                    Mem(ep.C).Keys.Add(ep.Item.Key);
                    Record(ep.C, Outcome.Ignored);
                    ExitEp(Clip.ClimbOutShort, "Sắp họp: 60 giây không bấm → tự thu, không nhắc lại cuộc này");
                }
                else if (ep.C == CaseId.Talk) ExitEp(Clip.ClimbOut, "Trò chuyện: 2 phút không gõ gì → Milo chào rồi leo xuống");
                else if (ep.C == CaseId.CheckIn)
                {
                    Mem(ep.C).Keys.Add(ep.Item.Key);
                    Record(ep.C, Outcome.Ignored);
                    ExitEp(Clip.ClimbOut, "Chào hỏi tự đóng sau 15 giây");
                }
                else Park();
                break;
            case Phase.Bubble: ExitEp(Clip.ClimbOutShort); break;
            case Phase.Breathe:
                ep.Cycle++;
                if (ep.Cycle >= ep.Cycles)
                {
                    if (ep.LongBreak)
                    {
                        S.BreakUntil = S.T + 900;
                        ToThanks("Milo giữ chỗ 15 phút. Đi dạo, uống nước, lát gặp!");
                    }
                    else ToThanks("Cảm ơn đã nghỉ cùng Milo!");
                }
                else SetPhase(Phase.Breathe, 12, Clip.Breathe);
                break;
            case Phase.Confirm:
                if (ep.AfterThanks) ToThanks(ep.ThanksText);
                else ExitEp(ep.AfterClip ?? Clip.ClimbOut);
                break;
            case Phase.Thanks: ExitEp(ep.AfterThanksClip); break;
            case Phase.Chat: ApplyIntent(ep.Intent); break;
            case Phase.Exit: FinishEp(); break;
        }
    }

    private void ToThanks(string? text, Clip after = Clip.ClimbOut)
    {
        var ep = S.Ep!;
        ep.ThanksText = text;
        ep.AfterThanksClip = after;
        SetPhase(Phase.Thanks, Catalog.ClipLength(Clip.Thanks) + 0.6, Clip.Thanks);
    }

    private void StartBreathe(int cycles)
    {
        var ep = S.Ep!;
        ep.Cycles = cycles;
        ep.Cycle = 0;
        SetPhase(Phase.Breathe, 12, Clip.Breathe);
        S.AcceptedBreaks++;
        ComputeMood();
    }

    // ================= phản hồi (mục 10) =================
    /// <summary>Người dùng bấm một nút trên thẻ/dashboard. <paramref name="act"/> giống data-act của prototype.</summary>
    public void Reply(string act, string? val = null)
    {
        var ep = S.Ep;
        if (ep is null) return;
        var c = ep.C;
        var def = Catalog.Def(c);
        var m = Mem(c);
        if (act == "expand")
        {
            ep.Compact = false;
            ep.CardVer++;
            return;
        }
        if (act == "stop" && ep.Phase == Phase.Breathe)
        {
            Log("Bấm Dừng → vẫn tính là đã nghỉ", LogKind.User);
            ToThanks("Cảm ơn đã nghỉ cùng Milo!");
            return;
        }
        if (ep.Phase != Phase.Show) return;
        if (act == "chat")
        {
            Chat(val);
            return;
        }
        if (act == "do")
        {
            DoAction(val);
            return;
        }
        // Thẻ tan tầm: tự đánh giá ngày và giữ chỗ nghỉ ngày mai — không đóng thẻ, không tính là trả lời lời nhắc
        if (act == "feel")
        {
            S.Feeling = Feeling.Parse(val);
            ep.CardVer++;
            Log($"Hôm nay bạn thấy: {Feeling.Label(S.Feeling)} (chỉ lưu trên máy, dùng để chấm mood)", LogKind.User);
            ComputeMood();
            return;
        }
        if (act == "holdTomorrow")
        {
            if (S.TomorrowHoldAt is null && TomorrowChain() is { } tc)
            {
                var at = tc[1].End;
                S.TomorrowHoldAt = at;
                ep.CardVer++;
                Log($"Giữ 10' nghỉ ngày mai lúc {Tm.Hm(at)} giữa chuỗi {tc.Count} cuộc họp", LogKind.User);
                Raise(new MiloAction.HoldBreak(at, at + 600, "Nghỉ cùng Milo", DayOffset: 1));
            }
            return;
        }
        Record(c, act switch
        {
            "snooze" or "remindAt" or "extend" => Outcome.Snoozed,
            "dismiss" => Outcome.Dismissed,
            _ => Outcome.Accepted,
        });
        switch (c)
        {
            case CaseId.Dashboard:
                if (act == "week") { ep.Page = DashPage.Week; ep.CardVer++; Log("Dashboard → Xem cả tuần", LogKind.User); }
                else if (act == "today") { ep.Page = DashPage.Today; ep.CardVer++; }
                else if (act == "detail") { ep.Detail = !ep.Detail; ep.Wardrobe = false; ep.CardVer++; Log(ep.Detail ? "Dashboard → Xem chi tiết" : "Dashboard → về 4 quả", LogKind.User); }
                else if (act == "wardrobe") { ep.Wardrobe = !ep.Wardrobe; ep.Detail = false; ep.CardVer++; Log(ep.Wardrobe ? "Mở tủ đồ → phối đồ cho Milo" : "Đóng tủ đồ → về 4 quả", LogKind.User); }
                else if (act == "talk") OpenTalk();
                else if (act == "close") ExitEp(Clip.ClimbOut, "Đóng dashboard → leo xuống");
                return;
            case CaseId.Talk:
                if (act == "breathe")
                {
                    Log("Trò chuyện → thở 1 phút cùng Milo", LogKind.User);
                    StartBreathe(5);
                }
                else ExitEp(Clip.ClimbOut, "Kết thúc trò chuyện → Milo vẫy tay rồi leo xuống");
                return;
            case CaseId.MorningHello:
                m.Keys.Add(ep.Item.Key);
                if (act == "remindAt")
                {
                    var at = MorningRemindAt();
                    S.Reminders.Add(new Reminder(c, "mh", at, new CaseData()));
                    ExitEp(Clip.ClimbOut, $"Bấm \"Nhắc lúc {Tm.Hm(at)}\" → hẹn đúng 1 lần");
                    return;
                }
                ExitEp(Clip.ClimbOut, "Bấm \"Đã rõ\" → leo xuống");
                return;
            case CaseId.MeetingSoon:
                var ev = ep.Data.Ev!;
                if (act == "open")
                {
                    if (ev.Attachment is null) return;
                    ep.Opened = true;
                    ep.CardVer++;
                    Log($"Bấm \"Mở slide\" → mở {ev.Attachment}", LogKind.User);
                    Raise(new MiloAction.OpenAttachment(ev));
                    return;
                }
                m.Keys.Add(ep.Item.Key);
                ExitEp(Clip.ClimbOutFast, "Bấm \"Tham gia\" → mở joinUrl, Milo thụt xuống vì sắp vào họp");
                Raise(new MiloAction.JoinMeeting(ev));
                return;
            case CaseId.CalendarPacked:
                m.Keys.Add(ep.Item.Key);
                if (act == "dismiss")
                {
                    m.ForDay = true;
                    ExitEp(Clip.ClimbOutShort, "Bấm \"Thôi\" → không hỏi lại hôm nay");
                    return;
                }
                {
                    var ch = ep.Data.Chain!;
                    var at = ch[1].End;
                    S.Holds.Add(new Hold("break", at, at + 600, "Nghỉ 10' · Milo giữ chỗ"));
                    ep.Held = true;
                    ep.AfterThanks = true;
                    ep.ThanksText = $"Đã giữ 10 phút nghỉ lúc {Tm.Hm(at)}!";
                    Log($"Giữ chỗ → POST /me/events \"Nghỉ cùng Milo\" {Tm.Hm(at)}–{Tm.Hm(at + 600)}, tentative", LogKind.User);
                    Raise(new MiloAction.HoldBreak(at, at + 600, "Nghỉ cùng Milo"));
                    SetPhase(Phase.Confirm, 2.4, Clip.Celebrate);
                }
                return;
            case CaseId.EmailWaiting:
                m.Keys.Add(ep.Item.Key);
                if (act == "remindAt")
                {
                    var at = EmailRemindAt();
                    S.Reminders.Add(new Reminder(c, "mail", at, new CaseData()));
                    ExitEp(Clip.ClimbOut, $"Hẹn nhắc email lúc {Tm.Hm(at)} (1 lần)");
                    return;
                }
                if (ep.Data.List is [var first, ..])
                {
                    first.Handled = true;
                    Raise(new MiloAction.OpenMail(first));
                }
                ExitEp(Clip.ClimbOut, "Bấm \"Mở Outlook\" → mở email đầu tiên");
                return;
            case CaseId.StuckTask:
                if (act == "snooze")
                {
                    m.SnoozedUntil = S.T + 3600 * Tune(c).WaitFactor;
                    ExitEp(Clip.ClimbOut, $"Để sau → hỏi lại sau {60 * Tune(c).WaitFactor:0} phút");
                    return;
                }
                m.Keys.Add(ep.Item.Key);
                {
                    var tk = ep.Data.Task!;
                    var len = ep.Data.FocusMin * 60;
                    S.FocusUntil = S.T + len;
                    S.FocusStart = S.T;
                    S.FocusTask = tk;
                    S.Holds.Add(new Hold("focus", S.T, S.T + len, "Tập trung: #" + tk.Id));
                    ep.Confirmed = true;
                    ep.AfterClip = Clip.ClimbOutFast;
                    Log($"Khoá {ep.Data.FocusMin} phút → tạo sự kiện \"Tập trung: #{tk.Id}\" + presence DoNotDisturb đến {Tm.Hm(S.FocusUntil.Value)}", LogKind.User);
                    Raise(new MiloAction.StartFocus(tk.Id, tk.Title, S.T, S.T + len));
                    SetPhase(Phase.Confirm, 2.4, Clip.Reminder);
                }
                return;
            case CaseId.CheckIn:
                m.Keys.Add(ep.Item.Key);
                ExitEp(Clip.ClimbOut, "Bấm \"Cảm ơn Milo\"");
                return;
            case CaseId.FocusPlan:
                m.Keys.Add(ep.Item.Key);
                if (act == "dismiss")
                {
                    m.ForDay = true;
                    ExitEp(Clip.ClimbOutShort, "Bấm \"Thôi\" → không đề nghị giữ giờ tập trung nữa hôm nay");
                    return;
                }
                {
                    var (at, end) = (ep.Data.At, ep.Data.At + ep.Data.Min * 60);
                    S.Holds.Add(new Hold("focusPlan", at, end, "Tập trung · Milo giữ chỗ"));
                    ep.Held = true;
                    ep.AfterThanks = true;
                    ep.ThanksText = $"Đã giữ {Tm.Dur(ep.Data.Min)} tập trung lúc {Tm.Hm(at)}! Tới giờ Milo bật Không làm phiền.";
                    Log($"Giữ giờ tập trung {Tm.Hm(at)}–{Tm.Hm(end)} (busy) · tới giờ tự bật Không làm phiền", LogKind.User);
                    Raise(new MiloAction.HoldFocus(at, end));
                    SetPhase(Phase.Confirm, 2.4, Clip.Celebrate);
                }
                return;
            case CaseId.WeekReport:
                m.Keys.Add(ep.Item.Key);
                if (act == "week")
                {
                    // Chuyển thẳng sang dashboard trang tuần, Milo không cần leo xuống rồi lên lại
                    Log("Báo cáo tuần → mở chùm nho 7 ngày", LogKind.User);
                    S.Ep = null;
                    StartEp(new QueueItem { C = CaseId.Dashboard, Key = "d" + S.T, Pri = 0, Sev = 0, Enq = S.T }, swap: true);
                    S.Ep!.Page = DashPage.Week;
                    return;
                }
                ExitEp(Clip.ClimbOut, "Bấm \"Đã rõ\" → leo xuống");
                return;
            case CaseId.EodWrapup:
                S.EodShown = true;
                if (act == "extend")
                {
                    S.ExtendedUntil = S.T + 1800;
                    ExitEp(Clip.ClimbOut, $"Bấm \"Thêm 30 phút\" (chỉ 1 lần) → nhắc lại lúc {Tm.Hm(S.ExtendedUntil.Value)}");
                    return;
                }
                ep.GoHome = true;
                ExitEp(Clip.RunToCar, "Bấm \"Về thôi\" → chạy ra xe");
                return;
            case CaseId.EodNudge:
                S.Nudged = true;
                ep.GoHome = true;
                ExitEp(Clip.RunToCar, "Bấm \"Về thôi\" → chạy ra xe");
                return;
        }
        if (def.Kind == CaseKind.Care) CareReply(act);
    }

    public double MorningRemindAt() => S.T < Tm.T("10:00") ? Tm.T("10:00") : S.T + 3600;
    public double EmailRemindAt() => S.T < Tm.T("16:00") ? Tm.T("16:00") : S.T + 3600;

    private void CareReply(string act)
    {
        var ep = S.Ep!;
        var c = ep.C;
        var def = Catalog.Def(c);
        var m = Mem(c);
        if (act == "accept")
        {
            m.Keys.Add(ep.Item.Key);
            switch (c)
            {
                case CaseId.MeetingOverload or CaseId.NoBreak:
                    Log("Đồng ý → thở 4-4-4 × 3 nhịp", LogKind.User);
                    StartBreathe(3);
                    return;
                case CaseId.LowRest:
                    Log("Đồng ý → thở 5 nhịp, rồi Milo giữ chỗ 15 phút", LogKind.User);
                    StartBreathe(5);
                    ep.LongBreak = true;
                    return;
                case CaseId.LunchMissed:
                    S.AcceptedBreaks++;
                    Log("Đồng ý \"Đi ăn thôi\" → vẫy rồi đi, không vòng thở", LogKind.User);
                    ToThanks("Ăn ngon nha! Milo trông máy cho.");
                    return;
                case CaseId.Overtime:
                    ep.GoHome = true;
                    ExitEp(Clip.RunToCar, "Chốt việc, về thôi → chạy ra xe");
                    return;
                case CaseId.HighFragmentation:
                    S.FocusUntil = S.T + 1800;
                    S.FocusStart = S.T;
                    S.FocusTask = new WorkTask { Id = "gom việc", Title = "Gom việc" };
                    ep.AfterClip = Clip.ClimbOutFast;
                    ep.Confirmed = true;
                    Log("Đồng ý → khoá tập trung 30 phút + Không làm phiền", LogKind.User);
                    Raise(new MiloAction.StartFocus("gom việc", "Gom việc", S.T, S.T + 1800));
                    SetPhase(Phase.Confirm, 2.4, Clip.Reminder);
                    return;
            }
        }
        if (act == "lunchLock")
        {
            m.Keys.Add(ep.Item.Key);
            S.Holds.Add(new Hold("lunch", S.T, S.T + 1800, "Nghỉ trưa · Milo giữ chỗ"));
            S.AcceptedBreaks++;
            Log("Khoá 30' nghỉ trưa trong lịch", LogKind.User);
            Raise(new MiloAction.HoldBreak(S.T, S.T + 1800, "Nghỉ trưa · Milo giữ chỗ"));
            ToThanks("Đã khoá 30 phút trưa. Đi ăn nha!");
            return;
        }
        if (act == "snooze")
        {
            m.Snoozes++;
            m.SnoozedUntil = S.T + def.Snooze * 60 * WaitFactor(c);
            ExitEp(Clip.ClimbOut, $"Để sau → nhắc lại lúc {Tm.Hm(m.SnoozedUntil)} nếu điều kiện vẫn đúng ({m.Snoozes}/2 lần hôm nay)");
            return;
        }
        if (act == "dismiss")
        {
            m.Keys.Add(ep.Item.Key);
            m.Dismisses++;
            if (m.Dismisses >= 2) m.Widen = 3;
            m.DismissedUntil = S.T + def.Dismiss * 60 * WaitFactor(c);
            ExitEp(Clip.ClimbOutShort, $"Không cần → ẩn case {def.Dismiss * WaitFactor(c):0} phút" + (m.Widen > 1 ? " (từ chối lần 2 → giãn ×3)" : "")
                + (Tune(c).WaitFactor > 1 ? " (cá nhân hoá 7 ngày ×2)" : ""));
        }
    }

    private sealed record ChatIntent(string Key, Regex Re, string Say);

    // Thứ tự quan trọng: câu đầu tiên khớp sẽ thắng ("về thôi" là về nhà, không phải "thôi" = từ chối)
    private static readonly ChatIntent[] Intents =
    [
        new("home", new Regex("(về thôi|đi về|về nhà|về nha|về đây|tan làm|tan ca|nghỉ thôi|xong việc)", RegexOptions.IgnoreCase), "Về nhà vui vẻ nha, mai gặp!"),
        new("busy", new Regex("(họp|bận|lát|để sau|đang làm|busy)", RegexOptions.IgnoreCase), "Hiểu rồi, bạn đang bận. Milo quay lại sau nhé."),
        new("tired", new Regex("(mệt|căng|đuối|stress|áp lực)", RegexOptions.IgnoreCase), "Vậy thở cùng Milo vài nhịp nha."),
        new("yes", new Regex("^(?!.*(không|chưa)).*(đồng ý|được|oke|\\bok\\b|ừ|yes|đi thôi|làm luôn|triển|chốt)", RegexOptions.IgnoreCase), "Okie, làm luôn nè!"),
        new("no", new Regex("(không|thôi|ổn|khỏi)", RegexOptions.IgnoreCase), "Okie, Milo tôn trọng bạn."),
        new("thanks", new Regex("(cảm ơn|cám ơn|thanks)", RegexOptions.IgnoreCase), "Hihi, Milo vui lắm!"),
    ];

    /// <summary>Nút chính của từng thẻ — dùng khi người dùng chat "đồng ý".</summary>
    private static string PrimaryAct(CaseId c) => c switch
    {
        CaseId.MorningHello => "gotIt",
        CaseId.MeetingSoon => "join",
        CaseId.EmailWaiting => "openMail",
        CaseId.CheckIn => "thanks",
        CaseId.EodWrapup or CaseId.EodNudge => "goHome",
        CaseId.WeekReport => "gotIt",
        _ => "accept",
    };

    private void Chat(string? text)
    {
        var ep = S.Ep!;
        text = (text ?? "").Trim();
        if (text.Length == 0) return;
        S.ChatHistory.Add(text);
        var history = ep.Chat.TakeLast(6).ToList();
        var def = Catalog.Def(ep.C);
        // Câu có dấu hiệu khủng hoảng: luôn trả lời bằng câu cố định đã duyệt, không giao cho LLM
        if (Talk.IsCrisis(text))
        {
            ep.Chat.Add(new ChatLine(text, Talk.CrisisReply));
            Log("Chat có dấu hiệu khủng hoảng → Milo trả lời bằng câu an toàn cố định, khuyên tìm người hỗ trợ", LogKind.User);
            ep.PhaseEnd = S.T + Dt(Math.Max(def.Timeout, Catalog.CareTimeout));
            ep.CardVer++;
            return;
        }
        if (ep.C == CaseId.Talk)
        {
            // Trò chuyện tự do: không đoán ý định để đóng thẻ, mọi câu đều được trả lời
            var ask = ChatWanted is not null && !S.Instant;
            ep.Chat.Add(ask ? new ChatLine(text, Lines.ChatThinking) : new ChatLine(text, Talk.Reply(this, text), Talk.Suggest(this, text)));
            Log($"Trò chuyện \"{text}\" → " + (ask ? "hỏi LLM" : "trả lời theo luật (chưa bật AI)"), LogKind.User);
            if (ask) ChatWanted!(ep.Id, ep.Chat.Count - 1, new ChatRequest(ep.C, def.Name, Talk.Facts(this), text, history, Offer: Talk.Available(this)));
            ep.PhaseEnd = S.T + Dt(def.Timeout);
            ep.CardVer++;
            return;
        }
        var it = Intents.FirstOrDefault(i => i.Re.IsMatch(text));
        var askLlm = it is null && ChatWanted is not null && !S.Instant;
        // Không khớp ý định và không có AI: câu mặc định + tính năng hợp với câu gõ (vd. "mệt mà nhiều task" → Tìm giờ tập trung)
        ep.Chat.Add(new ChatLine(text, it?.Say ?? (askLlm ? Lines.ChatThinking : Lines.ChatFallback), it is null && !askLlm ? Talk.Suggest(this, text) : null));
        Record(ep.C, Outcome.Chat);
        Log($"Chat \"{text}\" → ý định: " + (it?.Key ?? (askLlm ? "không rõ → hỏi LLM (tối đa 2.5 giây)" : "không rõ (chưa bật LLM)")), LogKind.User);
        if (askLlm) ChatWanted!(ep.Id, ep.Chat.Count - 1, new ChatRequest(ep.C, def.Name, Lines.Facts(this, ep.C, ep.Data), text, history,
            def.Kind == CaseKind.Care || ep.C is CaseId.EodWrapup or CaseId.EodNudge ? Lines.PrimaryLabel(ep.C) : null, Talk.Available(this)));
        ep.PhaseEnd = S.T + Dt(def.Timeout > 0 ? def.Timeout : Catalog.CareTimeout);
        ep.CardVer++;
        if (it is not null)
        {
            ep.Intent = it.Key;
            ep.Phase = Phase.Chat;
            ep.PhaseEnd = S.T + Dt(1.8);
        }
    }

    /// <summary>
    /// Người dùng bấm 1 tính năng Milo đề nghị trong chat. Kiểm tra lại còn dùng được không (lịch/task có thể đã đổi),
    /// rồi làm ngay trên thẻ đang mở hoặc chuyển sang thẻ của tính năng đó.
    /// </summary>
    private void DoAction(string? key)
    {
        var ep = S.Ep!;
        if (key is null || !Talk.Actions.TryGetValue(key, out var a)) return;
        if (!Talk.Available(this).Contains(key))
        {
            ep.Chat.Add(new ChatLine(a.Label, "Tiếc quá, lúc này Milo chưa làm được việc đó. Mình thử cách khác nha."));
            ep.CardVer++;
            return;
        }
        Log($"Bấm tính năng Milo đề nghị trong chat: {a.Label}", LogKind.User);
        if (ep.C != CaseId.Talk)
        {
            Mem(ep.C).Keys.Add(ep.Item.Key); // lời nhắc coi như đã được trả lời
            Record(ep.C, Outcome.Accepted);
        }
        switch (key)
        {
            case "breathe":
                StartBreathe(5);
                break;
            case "break15":
                StartBreathe(5);
                ep.LongBreak = true;
                break;
            case "focus30":
                S.FocusUntil = S.T + 1800;
                S.FocusStart = S.T;
                S.FocusTask = new WorkTask { Id = "tập trung", Title = "Tập trung" };
                ep.AfterClip = Clip.ClimbOutFast;
                ep.Confirmed = true;
                Raise(new MiloAction.StartFocus("tập trung", "Tập trung", S.T, S.T + 1800));
                SetPhase(Phase.Confirm, 2.4, Clip.Reminder);
                break;
            case "planFocus" or "stuck":
                // Thẻ của tính năng đó hiện ngay khi Milo leo xuống xong
                ExitEp(Clip.ClimbOutShort);
                ForceCase(key == "stuck" ? CaseId.StuckTask : CaseId.FocusPlan, fromChat: true);
                break;
            case "dashboard" or "wardrobe":
                S.Ep = null;
                StartEp(new QueueItem { C = CaseId.Dashboard, Key = "d" + S.T, Pri = 0, Sev = 0, Enq = S.T }, swap: true);
                S.Ep!.Wardrobe = key == "wardrobe";
                break;
        }
    }

    private void ApplyIntent(string? k)
    {
        var ep = S.Ep!;
        ep.Phase = Phase.Show;
        var eod = ep.C is CaseId.EodWrapup or CaseId.EodNudge;
        switch (k)
        {
            // Chat "về thôi": ở thẻ tan tầm/quá giờ thì chạy ra xe; thẻ khác coi như đang bận
            case "home" when eod: Reply("goHome"); break;
            case "home" when ep.C == CaseId.Overtime: Reply("accept"); break;
            case "home": Reply("snooze"); break;
            case "yes": Reply(PrimaryAct(ep.C)); break;
            // Thẻ tan tầm không có Để sau/Không cần: "bận"/"chưa" = làm thêm 30 phút (1 lần), rồi để Milo leo xuống
            case "busy" or "no" when ep.C == CaseId.EodWrapup && S.ExtendedUntil is null: Reply("extend"); break;
            case "busy" or "no" when eod:
                if (ep.C == CaseId.EodWrapup) S.EodShown = true;
                else S.Nudged = true;
                ExitEp(Clip.ClimbOut, "Chat: chưa về → Milo leo xuống, case Quá giờ sẽ tiếp quản nếu còn làm");
                break;
            case "busy": Reply("snooze"); break;
            case "no": Reply("dismiss"); break;
            case "tired":
                if (eod) S.Feeling = Feeling.Bad; // "mệt" ở thẻ tan tầm cũng là câu trả lời cho "Hôm nay thấy sao?"
                Mem(ep.C).Keys.Add(ep.Item.Key);
                StartBreathe(3);
                break;
            case "thanks":
                Mem(ep.C).Keys.Add(ep.Item.Key);
                S.ExtraBonus++;
                ExitEp(Clip.ClimbOut, "Cảm ơn → tính nửa Đồng ý (+1)");
                break;
        }
    }
}
