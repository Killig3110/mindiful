namespace Minditful.Core.Engine;

public sealed partial class MiloEngine
{
    // ================= sự kiện thế giới =================
    private void Unlock()
    {
        S.Locked = false;
        if (!S.DayStarted)
        {
            S.DayStarted = true;
            var resumed = S.KnownDayStart is { } known && known < S.T;
            S.FirstAct = resumed ? S.KnownDayStart : S.T;
            S.LastBreakEnd = S.T;
            S.NextVisit = S.T + RandMin(Cfg.VisitMinMinutes, Cfg.VisitMaxMinutes);
            if (Cfg.IsFlexible) SetFlexibleDay(S.FirstAct!.Value);
            if (resumed)
            {
                // Mở lại app giữa ngày: giữ giờ bắt đầu thật, không chào sáng lần nữa
                Log($"Mở lại app · hôm nay bắt đầu làm từ {Tm.Hm(S.FirstAct!.Value)}", LogKind.User);
                return;
            }
            Log("Mở khoá lần đầu trong ngày", LogKind.User);
            if (Cfg.MorningHelloUntil is not { } until || S.T < until)
                Enqueue(CaseId.MorningHello, "mh", new CaseData());
        }
        else Log("Mở khoá máy", LogKind.User);
    }

    /// <summary>
    /// Giờ linh hoạt: bắt đầu = lần mở máy đầu ngày (làm tròn xuống 5 phút), kẹp trong [08:00, 10:00];
    /// giờ về = bắt đầu + 9 tiếng → vào 8h về 17h, 9h về 18h, 10h về 19h.
    /// </summary>
    private void SetFlexibleDay(double firstAct)
    {
        var start = Math.Clamp(Math.Floor(firstAct / 300) * 300, Cfg.FlexEarliestStart!.Value, Cfg.FlexLatestStart);
        Cfg.Start = start;
        Cfg.End = start + Cfg.FlexHours * 3600;
        Log($"Giờ làm linh hoạt: hôm nay {Tm.Hm(Cfg.Start)}–{Tm.Hm(Cfg.End)} (tính từ lúc mở máy {Tm.Hm(firstAct)})", LogKind.Sig);
    }

    /// <summary>Host báo giờ bắt đầu thật của hôm nay (lưu trên máy) trước khi mở khoá lần đầu.</summary>
    public void RestoreDayStart(double firstAct) => S.KnownDayStart = firstAct;

    private void WorldAct(string a, string? id = null)
    {
        switch (a)
        {
            case "unlock": Unlock(); break;
            case "lock":
                S.Locked = true;
                S.Peek = false;
                Log("Khoá máy", LogKind.User);
                break;
            case "away":
                S.Away = true;
                Log("Rời khỏi máy (idle)", LogKind.User);
                break;
            case "back":
                S.Away = false;
                Log("Quay lại máy", LogKind.User);
                break;
            case "done": CompleteTask(id, null); break;
        }
    }

    private void CompleteTask(string? id, string? title)
    {
        var tk = S.Tasks.FirstOrDefault(k => k.Id == id && !k.Done);
        if (tk is null && id is not null && title is not null)
        {
            tk = new WorkTask { Id = id, Title = title };
            S.Tasks.Add(tk);
        }
        tk ??= S.Tasks.FirstOrDefault(k => !k.Done);
        if (tk is null) return;
        tk.Done = true;
        S.TasksDone++;
        Log($"#{tk.Id} chuyển sang Done", LogKind.User);
        ComputeMood();
        Enqueue(CaseId.TaskDone, "done-" + tk.Id, new CaseData { Task = tk }, ttl: S.T + 7200);
    }

    private void UiAct(string a)
    {
        switch (a)
        {
            case "hover":
                if (Presence() == PresenceState.Hidden)
                {
                    S.Peek = true;
                    Log("Rê chuột lên chóp đuôi 600ms → ló đầu + thì thầm", LogKind.User);
                }
                break;
            case "unhover": S.Peek = false; break;
            case "openDash": OpenDash(); break;
        }
    }

    private void OpenDash()
    {
        if (S.Ep is not null)
        {
            if (S.Ep.C == CaseId.Dashboard) Reply("close");
            return;
        }
        var g = HardGate();
        if (g is Gate.Off or Gate.Locked) return;
        if (S.ParkedList.Count > 0 && g is null)
        {
            var p = S.ParkedList[0];
            S.ParkedList.RemoveAt(0);
            Mem(p.C).SnoozedUntil = 0;
            Log($"Bấm chóp đuôi có chấm → mở lại {Catalog.Def(p.C).Name}", LogKind.User);
            StartEp(p.Item, fromParked: true);
            return;
        }
        Log("Bấm chóp đuôi → mở dashboard", LogKind.User);
        StartEp(new QueueItem { C = CaseId.Dashboard, Key = "d" + S.T, Pri = 0, Sev = 0, Enq = S.T });
        S.Ep!.Page = DashPage.Today;
    }

    private void DotClick()
    {
        SortQueue();
        var top = S.Queue.FirstOrDefault();
        if (top is null || S.Ep is not null) return;
        Log($"Bấm chấm chờ → mở {Catalog.Def(top.C).Name} ngay, kể cả đang họp (P0)", LogKind.User);
        StartEp(top, fromDot: true);
    }

    // ================= vòng thời gian =================
    private void GateWatch()
    {
        var g = HardGate();
        if (g != S.Gate)
        {
            var old = S.Gate;
            S.Gate = g;
            if (g is { } ng && ng is not (Gate.Off or Gate.Locked))
                Log($"Cổng {Catalog.GateLabel[ng]} đóng → Milo im lặng" + (S.Queue.Count > 0 ? ", lời nhắc hiện thành chấm chờ" : ""), LogKind.Gate);
            if (g is null && old is { } og)
            {
                if (og == Gate.Meeting)
                {
                    S.SettleUntil = S.T + Cfg.Settle;
                    S.JumpUntil = S.T + Cfg.JumpWindow;
                    Log("Hết họp → " + (S.Queue.Count > 0
                        ? $"có {S.Queue.Count} lời nhắc chờ, đợi 2 phút rồi nhảy ra"
                        : "hàng đợi rỗng, chỉ hiện lại chóp đuôi (không nhảy ra)"), LogKind.Gate);
                }
                else if (og == Gate.Focus) S.JumpUntil = S.T + Cfg.JumpWindow;
                else if (og is not (Gate.Off or Gate.Locked)) Log($"Cổng {Catalog.GateLabel[og]} mở", LogKind.Gate);
            }
            if (S.Ep is { } ep && g is { } gg && ep.Phase is Phase.Enter or Phase.Show or Phase.Breathe or Phase.Chat or Phase.Bubble && !ep.FromDot)
            {
                Log($"Bị ngắt bởi cổng {Catalog.GateLabel[gg]} → thụt xuống nhanh, "
                    + (ep.C is CaseId.MeetingSoon or CaseId.Dashboard ? "bỏ thẻ" : "thẻ quay lại hàng đợi"), LogKind.Gate);
                if (ep.Phase is Phase.Enter or Phase.Show) Requeue(ep);
                Record(ep.C, Outcome.Gated);
                ep.Card = false;
                ExitEp(Clip.ClimbOutFast);
            }
            if (S.Visit is not null && g is not null) S.Visit = null;
            if (g is not null) S.Peek = false;
        }
        if (S.FocusUntil is { } fu && S.T >= fu)
        {
            var len = (fu - S.FocusStart!.Value) / 60;
            S.FocusDone++;
            S.FocusMinDone += len;
            S.FocusUntil = null;
            Log($"Hết khối tập trung {Tm.JsRound(len)} phút → +4 điểm, tắt Không làm phiền", LogKind.Gate);
            Raise(new MiloAction.EndFocus());
            ComputeMood();
            Enqueue(CaseId.FocusDone, "fd-" + S.T, new CaseData { Min = len }, ttl: S.T + 7200);
        }
    }

    private void TryVisit()
    {
        if (S.Ep is not null || S.Visit is not null || S.Peek || S.Queue.Count > 0 || S.NextVisit is null) return;
        if (HardGate() is not null || S.Away || S.Typing) return;
        if (S.T < S.NextVisit) return;
        S.Visit = new Visit { Phase = VisitPhase.In, End = S.T + Dt(Catalog.ClipLength(Clip.PeekIn)), Clip = Clip.PeekIn };
        Log("Ghé ngang: ló lên 8 giây rồi đi (không bóng thoại, không tính ngân sách)", LogKind.Sig);
    }

    private void AdvanceVisit()
    {
        var v = S.Visit!;
        if (v.Phase == VisitPhase.In)
        {
            v.Phase = VisitPhase.Look;
            v.Clip = Clip.LookAround;
            v.End = S.T + Dt(8);
        }
        else if (v.Phase == VisitPhase.Look)
        {
            v.Phase = VisitPhase.Out;
            var dig = S.BandIdx == 3 && S.Rnd.Next() < 0.04;
            v.Clip = dig ? Clip.DigExhausted : Clip.ClimbOutShort;
            v.End = S.T + Dt(Catalog.ClipLength(v.Clip));
        }
        else
        {
            S.Visit = null;
            S.NextVisit = S.T + RandMin(Cfg.VisitMinMinutes, Cfg.VisitMaxMinutes);
        }
    }

    public bool Busy => S.Ep is not null || S.Visit is not null || S.Peek;

    private void Tick()
    {
        if (Script is not null)
        {
            while (S.Wi < Script.World.Count && Script.World[S.Wi].T <= S.T)
            {
                var w = Script.World[S.Wi++];
                WorldAct(w.Action, w.Id);
            }
            while (S.Ui < Script.Ui.Count && Script.Ui[S.Ui].T <= S.T)
            {
                var u = Script.Ui[S.Ui++];
                if (S.Auto || S.Instant) UiAct(u.Action);
            }
        }
        var minute = (long)Math.Floor(S.T / 60);
        if (minute != S.LastMin)
        {
            S.LastMin = minute;
            MinuteTick();
        }
        GateWatch();
        if (S.Ep is { } ep)
        {
            if (ep.Phase == Phase.Show && ep.AutoPlan.Count > 0 && S.T - ep.PhaseStart >= (S.Instant ? 0 : ep.AutoPlan.Peek().Delay))
                Reply(ep.AutoPlan.Dequeue().Act);
            var guard = 0;
            while (S.Ep is not null && S.T >= S.Ep.PhaseEnd && guard++ < 20) AdvanceEp();
        }
        if (S.Visit is not null)
        {
            var g2 = 0;
            while (S.Visit is not null && S.T >= S.Visit.End && g2++ < 5) AdvanceVisit();
        }
        Arbitrate();
        TryVisit();
        if (Cfg.Scripted && S.T >= Cfg.DayClose && !S.Ended)
        {
            S.Ended = true;
            Log("Hết ngày mẫu. Bấm \"Làm lại\" để xem lại từ đầu.", LogKind.Gate);
        }
    }

    /// <summary>
    /// Chạy thêm <paramref name="sec"/> giây. Ngày mẫu tua nhanh nên dừng lại ngay khi Milo bắt đầu hiện
    /// (host sẽ chạy tiếp với tốc độ thật để xem trọn hoạt ảnh).
    /// </summary>
    public void Advance(double sec, bool stopWhenBusy = true)
    {
        var target = S.T + sec;
        var wasBusy = Busy;
        while (S.T < target)
        {
            var step = Math.Min(1, target - S.T);
            S.T += step;
            Tick();
            if (stopWhenBusy && !wasBusy && Busy) break;
            if (S.Ended) break;
        }
    }

    /// <summary>Chạy tới đúng giờ thật (Prod/Sandbox).</summary>
    public void AdvanceTo(double t)
    {
        if (t > S.T) Advance(t - S.T, stopWhenBusy: false);
    }

    /// <summary>Nhảy tới một mốc của ngày mẫu: chạy lại từ đầu thật nhanh.</summary>
    public void RunTo(double target)
    {
        var auto = S.Auto;
        S = NewState(Cfg.DayOpen);
        S.Auto = auto;
        S.Instant = true;
        while (S.T < target)
        {
            S.T += 1;
            Tick();
        }
        S.Instant = false;
        AssessMeetings();
        if (S.Ep is { } ep)
        {
            ep.PhaseEnd = Math.Max(ep.PhaseEnd, S.T);
            if (ep.Phase == Phase.Show) ep.PhaseStart = S.T;
            ep.CardVer++;
        }
    }

    public void Reset()
    {
        var auto = S.Auto;
        S = NewState(Cfg.DayOpen);
        S.Auto = auto;
        AssessMeetings();
    }

    // ================= API cho UI & tín hiệu thật =================
    public void SetAuto(bool on)
    {
        S.Auto = on;
        if (S.Ep is { } ep)
            ep.AutoPlan = new Queue<(double, string)>(on && Script is not null && Script.AutoReplies.TryGetValue(ep.C, out var p) ? p : []);
    }

    /// <summary>Người dùng tự bấm nút trên thẻ: huỷ kế hoạch tự trả lời rồi xử lý.</summary>
    public void UserReply(string act, string? val = null)
    {
        if (S.Ep is null) return;
        S.Ep.AutoPlan.Clear();
        Reply(act, val);
    }

    public void SetLocked(bool locked)
    {
        if (locked == S.Locked) return;
        if (locked) WorldAct("lock");
        else Unlock();
    }

    /// <param name="backfillMinutes">Idle đã kéo dài bao lâu trước khi được coi là rời máy (tính vào lần nghỉ).</param>
    public void SetAway(bool away, int backfillMinutes = 0)
    {
        if (away == S.Away) return;
        WorldAct(away ? "away" : "back");
        if (away && backfillMinutes > 0 && !InCall()) S.BreakRun += backfillMinutes;
    }

    public void SetTyping(bool typing)
    {
        if (typing == S.Typing) return;
        S.Typing = typing;
        Log(typing ? "Bạn bắt đầu gõ phím liên tục" : "Bạn dừng gõ", LogKind.User);
    }

    public void SetFullscreen(bool on)
    {
        if (on == S.Fullscreen) return;
        S.Fullscreen = on;
        Log(on ? "Mở app toàn màn hình" : "Thoát toàn màn hình", LogKind.User);
    }

    public void SetUserDnd(bool on)
    {
        if (on == S.UserDnd) return;
        S.UserDnd = on;
        Log(on ? "Tự đặt Teams sang Không làm phiền" : "Tắt Không làm phiền", LogKind.User);
    }

    public void SetInCallOverride(bool? inCall) => S.InCallOverride = inCall;

    /// <summary>Teams presence "Presenting" (hoặc Demo/Sandbox giả lập): Milo trốn hẳn tới khi thôi trình chiếu.</summary>
    public void SetPresenting(bool on)
    {
        if (on == S.Presenting) return;
        S.Presenting = on;
        Log(on ? "Bạn đang trình chiếu → Milo trốn hẳn, kể cả chóp đuôi" : "Thôi trình chiếu → chóp đuôi hiện lại", LogKind.User);
    }

    public void LeaveMeeting()
    {
        if (Ongoing() is not { } ev) return;
        S.Skipped.Add(ev.Id);
        Log("Rời cuộc họp " + ev.Subject, LogKind.User);
    }

    public void RecordSwitch() => S.Switches.Add(S.T);

    public void SimulateFragmentation()
    {
        for (var i = 0; i < 12; i++) S.Switches.Add(S.T - i * 290);
        Log("Nhảy qua lại giữa IDE, Teams, trình duyệt 12 lần trong giờ qua", LogKind.User);
        ComputeMood();
        if (S.DayStarted) EvalRules();
    }

    public void MarkTaskDone(string? id = null, string? title = null) => CompleteTask(id, title);

    public void ToggleStress()
    {
        S.Stress = S.Stress > 0 ? 0 : 30;
        Log(S.Stress > 0 ? "Giả lập ngày căng: trừ thêm 30 điểm để xem dáng mệt" : "Bỏ giả lập ngày căng", LogKind.User);
        ComputeMood();
    }

    public void Hover() => UiAct("hover");
    public void Unhover() => S.Peek = false;
    public void TailClick() => OpenDash();
    public void DotPillClick() => DotClick();

    public void MiloClick()
    {
        if (S.Ep is not { } ep) return;
        if (ep.C == CaseId.MorningHello && ep.Phase == Phase.Enter && ep.Clip == Clip.HangPull)
        {
            Log("Bấm Milo lúc đang leo → nhảy thẳng tới Hello", LogKind.User);
            ep.PhaseEnd = S.T;
            return;
        }
        if (ep.C == CaseId.Dashboard && ep.Phase == Phase.Show) Reply("close");
    }

    public void Escape()
    {
        if (S.Ep is { C: CaseId.Dashboard, Phase: Phase.Show }) Reply("close");
    }

    /// <summary>Sandbox: đưa thẳng 1 case vào hàng đợi để thử tích hợp thật, bỏ qua điều kiện.</summary>
    public void ForceCase(CaseId c)
    {
        var d = new CaseData();
        switch (c)
        {
            case CaseId.MeetingSoon:
                d.Ev = NextMeeting() ?? Meetings.LastOrDefault() ?? new CalendarEvent { Id = "sbx", Subject = "Cuộc họp thử", Start = S.T + 300, End = S.T + 1800 };
                break;
            case CaseId.CalendarPacked:
                d.Chain = Chains().FirstOrDefault(ch => ch.Count >= 2)?.ToList();
                if (d.Chain is null || d.Chain.Count < 2)
                {
                    Log("Lịch kín cần ≥ 2 cuộc họp liền nhau hôm nay để tính giờ giữ chỗ", LogKind.Error);
                    return;
                }
                break;
            case CaseId.StuckTask:
                d.Task = StuckTasks().FirstOrDefault() ?? S.Tasks.FirstOrDefault(x => !x.Done);
                if (d.Task is null)
                {
                    Log("Không có work item nào đang mở để thử Task kẹt", LogKind.Error);
                    return;
                }
                break;
            // Số liệu minh hoạ khi số thật còn bằng 0, để thẻ đọc được như trong tài liệu
            case CaseId.MeetingOverload: d.Count = 3; d.Min = Math.Max(160, MeetingMin()); break;
            case CaseId.NoBreak: d.Min = Math.Max(130, Streak()); break;
            case CaseId.Overtime: d.Over = Math.Max(35, S.OtMin); break;
            case CaseId.HighFragmentation: d.Sw = Math.Max(11, SwitchesHour()); break;
            case CaseId.FocusDone: d.Min = 90; break;
            case CaseId.FocusPlan:
                if (FocusSlot() is not { } fs)
                {
                    Log("Giữ giờ tập trung cần 1 khoảng trống ≥ 60 phút từ 15 phút nữa tới hết giờ làm", LogKind.Error);
                    return;
                }
                (d.At, d.Min) = fs;
                break;
            case CaseId.WeekReport:
                if (Snap.LastWeek is not { Days: > 0 })
                {
                    Log("Báo cáo tuần cần dữ liệu tuần trước trên máy (chưa có)", LogKind.Error);
                    return;
                }
                break;
            case CaseId.MicroBreak: d.Count = S.MicroCount; break;
            case CaseId.TaskDone:
                MarkTaskDone();
                return;
            case CaseId.Dashboard:
                if (S.Ep is null) OpenDash();
                return;
        }
        var m = Mem(c);
        m.SnoozedUntil = m.DismissedUntil = 0;
        m.ForDay = false;
        m.Shown = 0;
        m.Half.Clear();
        S.Queue.RemoveAll(q => q.C == c);
        var def = Catalog.Def(c);
        S.Queue.Add(new QueueItem { C = c, Key = "force-" + S.T, Data = d, Pri = def.Pri, Sev = Math.Max(1, def.Sev), Enq = S.T, Need = 0 });
        S.Forced.Add(c);
        S.LastProactive = -1e9;
        S.HourList.Clear();
        Log($"Sandbox: đưa {def.Name} vào hàng đợi (bỏ qua điều kiện & ngân sách)", LogKind.Queue);
    }
}
