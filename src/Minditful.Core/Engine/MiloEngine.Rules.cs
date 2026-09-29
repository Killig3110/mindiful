namespace Minditful.Core.Engine;

public sealed partial class MiloEngine
{
    private static readonly CaseId[] Conditional =
    [
        CaseId.MeetingOverload, CaseId.LowRest, CaseId.Overtime, CaseId.NoBreak, CaseId.LunchMissed,
        CaseId.HighFragmentation, CaseId.CalendarPacked, CaseId.StuckTask, CaseId.EmailWaiting, CaseId.FocusPlan, CaseId.MicroBreak,
    ];

    private static string HalfOf(double t) => t < Tm.T("12:00") ? "am" : "pm";

    private void Enqueue(CaseId c, string key, CaseData data, int? pri = null, int? sev = null, double? ttl = null)
    {
        S.Live?.Add(c);
        var def = Catalog.Def(c);
        var m = Mem(c);
        var ex = S.Queue.FirstOrDefault(q => q.C == c);
        if (ex is not null)
        {
            data.Line ??= ex.Data.Line; // giữ câu LLM đã viết sẵn khi số liệu được cập nhật
            ex.Data = data;
            ex.Key = key;
            return;
        }
        if (m.Keys.Contains(key)) return;
        if (S.T < m.SnoozedUntil || S.T < m.DismissedUntil || m.ForDay) return;
        if (def.MaxDay > 0 && m.Shown >= def.MaxDay) return;
        if (S.ParkedList.Any(p => p.C == c)) return;
        if (S.Ep is not null && S.Ep.C == c) return;
        var p = pri ?? def.Pri;
        var sv = sev is > 0 ? sev.Value : def.Sev > 0 ? def.Sev : 1;
        var item = new QueueItem { C = c, Key = key, Data = data, Pri = p, Sev = sv, Enq = S.T, Ttl = ttl, Need = def.Need };
        S.Queue.Add(item);
        Log($"{def.Name} đủ điều kiện → vào hàng đợi (P{p})", LogKind.Queue);
        RequestLine(item);
    }

    /// <summary>§14: gọi LLM ngay lúc case vào hàng đợi để khi giao không phải chờ.</summary>
    private void RequestLine(QueueItem item)
    {
        if (LineWanted is null || S.Instant || !Lines.Supports(item.C)) return;
        var variant = S.VariantCounter.GetValueOrDefault(item.C);
        LineWanted(item, new LineRequest(item.C, Catalog.Def(item.C).Name, Lines.Facts(this, item.C, item.Data), Lines.Action(item.C),
            Lines.Template(this, item.C, item.Data, variant)));
    }

    private void EvalRules()
    {
        var t = S.T;
        var g = HardGate();
        if (!S.OffDuty)
        {
            // Sắp họp (7.1): 5 phút trước sự kiện online, không đang trong cuộc gọi khác
            foreach (var e in Meetings)
            {
                var lead = e.Start - t;
                if (lead > 0 && lead <= 300 && e.IsOnline && !InCall() && !S.Skipped.Contains(e.Id))
                    Enqueue(CaseId.MeetingSoon, e.Id, new CaseData { Ev = e }, ttl: e.Start);
            }
            // Lịch kín (7.2)
            foreach (var ch in Chains())
            {
                if (ch.Count >= Math.Ceiling(3 * Tf(CaseId.CalendarPacked)) && ch[0].Start > t && ch[0].Start - t <= 240 * 60 && HalfOf(ch[0].Start) == HalfOf(t)
                    && !Mem(CaseId.CalendarPacked).Half.Contains(HalfOf(t)))
                    Enqueue(CaseId.CalendarPacked, ch[0].Id + HalfOf(t), new CaseData { Chain = ch });
            }
            // Email chờ (7.3) — không trước 10:00 vì bản tin sáng đã báo
            if (t >= Cfg.EmailNotBefore && WaitingEmails().Any(m => m.Days >= Math.Ceiling(Tf(CaseId.EmailWaiting))) && !Mem(CaseId.EmailWaiting).Half.Contains(HalfOf(t)))
                Enqueue(CaseId.EmailWaiting, "mail-" + HalfOf(t), new CaseData());
            // Task kẹt (7.4)
            var st = StuckTasks().Where(x => x.Days >= Math.Ceiling(Cfg.StuckMinDays * Tf(CaseId.StuckTask))).ToList();
            var slot = Math.Min(GapToNext(), (Cfg.End - t) / 60);
            if (st.Count > 0 && slot >= 45 && !FocusActive() && Mem(CaseId.StuckTask).Shown == 0)
                Enqueue(CaseId.StuckTask, st[0].Id, new CaseData { Task = st[0] });
            // Họp liên tục: chuỗi ≥3 vừa xong trong 60'
            foreach (var ch in Chains())
            {
                if (ch.Count < Math.Ceiling(Cfg.OverloadMinChain * Tf(CaseId.MeetingOverload))) continue;
                var last = ch[^1];
                if (last.End <= t && t - last.End <= 3600 && !InCall())
                    Enqueue(CaseId.MeetingOverload, "chain-" + ch[0].Id, new CaseData { Count = ch.Count, Min = (last.End - ch[0].Start) / 60 });
            }
            // Khối Nghỉ đã giữ trong lịch: tới giờ mà đã ra khỏi call thì chạy Họp liên tục
            foreach (var h in S.Holds)
            {
                if (h.Kind == "break" && t >= h.Start && t < h.End && !InCall())
                {
                    var chainStart = Chains().FirstOrDefault(c => c[0].Start <= h.Start && h.Start <= c[^1].End)?[0].Start ?? h.Start;
                    Enqueue(CaseId.MeetingOverload, "hold-" + h.Start, new CaseData { Count = 3, Min = (t - chainStart) / 60, FromHold = true });
                }
            }
            // Chưa nghỉ trưa
            if (t >= Tm.T("12:30") + 90 * 60 * (Tf(CaseId.LunchMissed) - 1) && t <= Tm.T("14:00") && !S.LunchTaken && !S.Away && !S.Locked)
                Enqueue(CaseId.LunchMissed, "lunch", new CaseData());
            // Làm liền
            var sk = Streak();
            if (sk >= Cfg.NoBreakMin * Tf(CaseId.NoBreak)) Enqueue(CaseId.NoBreak, "nb-" + S.LastBreakEnd, new CaseData { Min = sk });
            // Nghỉ quá ít
            var w = Worked();
            if (w >= 180 * Tf(CaseId.LowRest) && S.Rest < 0.5 * 45 * w / 480 && t < Cfg.End)
                Enqueue(CaseId.LowRest, "lr-" + Math.Floor(w / 180), new CaseData { Rest = S.Rest, Worked = w });
            // Phân mảnh
            var sw = SwitchesHour();
            if (sw > Cfg.FragThreshold * Tf(CaseId.HighFragmentation) && !InCall())
                Enqueue(CaseId.HighFragmentation, "frag-" + Math.Floor(t / 3600), new CaseData { Sw = sw });
            // Chào hỏi ngẫu nhiên (thứ tự điều kiện giữ nguyên để rnd() gọi đúng lúc như prototype)
            if (t >= Tm.T("10:00") && t <= Tm.T("16:30") && S.Score >= 60 && t - S.LastEpEnd >= 7200 && g is null
                && S.Queue.Count == 0 && Mem(CaseId.CheckIn).Shown < 2 && S.Rnd.Next() < 0.02)
                Enqueue(CaseId.CheckIn, "ci-" + Mem(CaseId.CheckIn).Shown, new CaseData());
            // Giữ giờ tập trung: 1 lần/ngày, đề nghị khoảng trống dài nhất còn lại (trước 15:00).
            // Chờ 20 phút sau lần mở máy đầu để không nối đuôi ngay Chào sáng.
            if (Cfg.FocusPlan && t >= Cfg.Start && t - (S.FirstAct ?? t) >= 1200 && t <= Tm.T("15:00") && Mem(CaseId.FocusPlan).Shown == 0 && !FocusActive()
                && !S.Holds.Any(h => h.Kind is "focus" or "focusPlan") && FocusSlot() is { } fs)
                Enqueue(CaseId.FocusPlan, "fp", new CaseData { At = fs.Start, Min = fs.Min });
            // Báo cáo tuần: sáng thứ Hai, sau Chào sáng, khi máy còn giữ dữ liệu tuần trước
            if (Cfg.WeekReport && Day.DayOfWeek == DayOfWeek.Monday && Snap.LastWeek is { Days: > 0 } && Mem(CaseId.WeekReport).Shown == 0
                && S.Ep?.C != CaseId.MorningHello && S.Queue.All(q => q.C != CaseId.MorningHello))
                Enqueue(CaseId.WeekReport, "wr", new CaseData());
            // Uống nước / 20-20-20: sau mỗi N phút ngồi máy liên tục (không tính giờ họp)
            if (Cfg.MicroBreakEveryMin > 0 && S.MicroCount < Cfg.MicroBreakMaxPerDay && t < Cfg.End && S.NmRun >= Cfg.MicroBreakEveryMin
                && t - S.LastMicro >= Cfg.MicroBreakEveryMin * 60 && !FocusActive())
                Enqueue(CaseId.MicroBreak, "mb-" + S.MicroCount, new CaseData { Count = S.MicroCount });
            // Tan tầm
            if (t >= Cfg.End && !S.EodShown) Enqueue(CaseId.EodWrapup, "eod", new CaseData());
            if (S.ExtendedUntil is { } ext && t >= ext && !S.Nudged) Enqueue(CaseId.EodNudge, "nudge", new CaseData());
        }
        // Quá giờ — xét cả khi đã "Về thôi"
        var otStart = 30 * Tf(CaseId.Overtime);
        if (S.OtAfter >= otStart)
        {
            var lvl = 1 + (int)Math.Floor((S.OtAfter - otStart) / 30);
            Enqueue(CaseId.Overtime, "ot-" + lvl, new CaseData { Over = S.OtMin }, pri: lvl >= 2 ? 2 : 3, sev: lvl >= 2 ? 3 : 2);
        }
        // Nhắc hẹn giờ
        foreach (var r in S.Reminders.ToList())
        {
            if (t < r.At) continue;
            S.Reminders.Remove(r);
            Mem(r.C).Half = [];
            Mem(r.C).Keys.Remove(r.Key);
            Enqueue(r.C, r.Key + "-r", r.Data);
        }
    }

    /// <summary>Tới giờ khối tập trung đã giữ trước: tự bật Không làm phiền (nếu đang ngồi máy và không họp).</summary>
    private void StartHeldFocus()
    {
        var h = S.Holds.FirstOrDefault(h => h.Kind == "focusPlan" && S.T >= h.Start && S.T < h.End - 300);
        if (h is null || S.Locked || S.OffDuty || InCall() || FocusActive()) return;
        S.Holds.Remove(h);
        S.Holds.Add(h with { Kind = "focus", Start = S.T });
        var tk = StuckTasks().FirstOrDefault() ?? S.Tasks.FirstOrDefault(x => !x.Done) ?? new WorkTask { Id = "tập trung", Title = "Tập trung" };
        S.FocusUntil = h.End;
        S.FocusStart = S.T;
        S.FocusTask = tk;
        Log($"Tới giờ tập trung đã giữ → Không làm phiền tới {Tm.Hm(h.End)}", LogKind.Gate);
        Raise(new MiloAction.StartFocus(tk.Id, tk.Title, S.T, h.End, CalendarHeld: true));
    }

    private void MinuteTick()
    {
        var t = S.T;
        if (S.DayStarted)
        {
            var brk = (S.Locked || S.Away) && !InCall();
            if (brk) S.BreakRun++;
            else
            {
                if (S.BreakRun >= 5)
                {
                    S.Rest += S.BreakRun;
                    S.LastBreakEnd = t;
                    var bs = t - S.BreakRun * 60;
                    if (S.BreakRun >= 20 && bs < Tm.T("14:00") && t > Tm.T("11:00")) S.LunchTaken = true;
                    Log($"Kết thúc lần nghỉ {S.BreakRun} phút → chuỗi làm liền về 0, tổng nghỉ {S.Rest} phút", LogKind.Sig);
                }
                S.BreakRun = 0;
            }
            var active = !S.Locked && !S.Away;
            if (active && !InCall())
            {
                S.NmRun++;
                S.LongestNm = Math.Max(S.LongestNm, S.NmRun);
            }
            else S.NmRun = 0;
            if (active && t > Cfg.End)
            {
                S.OtMin++;
                if (t > Math.Max(Cfg.End, S.ExtendedUntil ?? 0)) S.OtAfter++;
            }
            if (S.OffDuty && active)
            {
                S.OffActive++;
                if (S.OffActive >= 15)
                {
                    S.OffDuty = false;
                    Log("Vẫn làm 15 phút sau \"Về thôi\" → Milo thức lại, case Quá giờ được xét", LogKind.Gate);
                }
            }
            else S.OffActive = 0;
        }
        if (S.DayStarted) StartHeldFocus();
        if (!(S.OffDuty && S.Locked)) ComputeMood();
        MaybeAskMood();
        if (S.DayStarted)
        {
            S.Live = [];
            EvalRules();
            S.Queue = S.Queue.Where(q =>
            {
                if (Conditional.Contains(q.C) && !S.Live.Contains(q.C) && !S.Forced.Contains(q.C))
                {
                    Log($"{Catalog.Def(q.C).Name} không còn đúng → bỏ khỏi hàng đợi", LogKind.Queue);
                    return false;
                }
                return true;
            }).ToList();
            S.Live = null;
        }
    }
}
