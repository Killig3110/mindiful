using static Minditful.Core.Engine.Tm;

namespace Minditful.Core.Engine;

/// <summary>Cách tính điểm mood.</summary>
public enum MoodMode
{
    /// <summary>Chỉ Mood Engine theo luật (tài liệu §11).</summary>
    Rules,
    /// <summary>Luật làm nền, Claude chỉnh ±10 điểm và viết 1 câu nhận xét.</summary>
    Hybrid,
    /// <summary>Claude chấm điểm; luật dùng khi Claude chưa trả lời hoặc lỗi.</summary>
    Llm,
}

/// <summary>Cách đánh giá cuộc họp.</summary>
public enum MeetingMode { Rules, Llm }

/// <summary>Số liệu 1 cuộc họp gửi đi đánh giá — không có tiêu đề, người tham dự hay nội dung (§14).</summary>
public sealed record MeetingRequest(
    string EventId, double DurationMin, int Attendees, string Role, bool Online, string Start,
    int ChainIndex, int ChainLength, double GapAfterMin, bool AfterHours, bool OverLunch);

/// <summary>Kết quả đánh giá cuộc họp: mức nặng 1–5, loại, số phút nên nghỉ sau đó.</summary>
public sealed record MeetingAssessment(string EventId, int Load, string Kind, int RecoveryMin, string Note, string Source);

/// <summary>Số liệu cả ngày gửi đi đánh giá cảm xúc.</summary>
public sealed record MoodRequest(int RuleScore, string Facts, IReadOnlyList<string> Chat);

/// <summary>Nhận xét mood của Claude. <see cref="Adjust"/> dùng ở Hybrid, <see cref="Score"/> dùng ở chế độ Llm.</summary>
public sealed record MoodInsight(int? Score, int Adjust, int? Focus, int? Energy, int? Stress, string Label, string Insight, string Source, double At);

/// <summary>Đánh giá cuộc họp bằng luật — luôn có, kể cả khi không có LLM.</summary>
public static class MeetingRules
{
    public static readonly string[] Kinds = ["Trình bày", "1:1", "Họp đông", "Trao đổi", "Ra quyết định", "Cập nhật"];

    public static MeetingAssessment Assess(MeetingRequest r)
    {
        var load = r.DurationMin <= 30 ? 1 : r.DurationMin <= 60 ? 2 : r.DurationMin <= 90 ? 3 : 4;
        if (r.Attendees >= 6) load++;
        if (r.Role == "Trình bày") load++;
        if (r.ChainIndex >= 2) load++;           // cuộc thứ 3 trở đi trong chuỗi liền nhau
        if (r.AfterHours || r.OverLunch) load++;
        load = Math.Clamp(load, 1, 5);
        var kind = r.Role == "Trình bày" ? "Trình bày" : r.Attendees <= 2 ? "1:1" : r.Attendees >= 6 ? "Họp đông" : "Trao đổi";
        var recovery = load >= 4 ? 10 : load == 3 ? 5 : 0;
        var notes = new List<string>();
        if (r.ChainLength >= 3) notes.Add($"cuộc {r.ChainIndex + 1}/{r.ChainLength} trong chuỗi liền");
        if (recovery > 0 && r.GapAfterMin < recovery) notes.Add("không có khoảng nghỉ sau cuộc này");
        if (r.OverLunch) notes.Add("đè giờ ăn trưa");
        if (r.AfterHours) notes.Add("ngoài giờ làm");
        return new MeetingAssessment(r.EventId, load, kind, recovery, notes.Count > 0 ? string.Join(", ", notes) : "nhẹ nhàng", "Luật");
    }
}

public sealed partial class MiloEngine
{
    /// <summary>Cuộc họp mới cần Claude đánh giá (chế độ MeetingMode.Llm). Host trả về bằng <see cref="SetMeetingAssessment"/>.</summary>
    public event Action<MeetingRequest>? MeetingWanted;

    /// <summary>Tới lúc hỏi Claude về mood (chế độ Hybrid/Llm). Host trả về bằng <see cref="SetMoodInsight"/>.</summary>
    public event Action<MoodRequest>? MoodWanted;

    public MeetingAssessment? Assessment(string eventId) => S.Assessments.GetValueOrDefault(eventId);

    public MeetingRequest BuildMeetingRequest(CalendarEvent ev)
    {
        var chain = Chains().FirstOrDefault(c => c.Contains(ev)) ?? [ev];
        var next = Meetings.Where(m => m.Start >= ev.End).OrderBy(m => m.Start).FirstOrDefault();
        var gapAfter = (next?.Start ?? Math.Max(Cfg.End, ev.End)) - ev.End;
        var attendees = ev.People.Sum(p => p.Initials.StartsWith('+') && int.TryParse(p.Initials[1..], out var n) ? n : 1) + 1;
        return new MeetingRequest(ev.Id, (ev.End - ev.Start) / 60, attendees, ev.Role, ev.IsOnline, Hm(ev.Start),
            chain.IndexOf(ev), chain.Count, gapAfter / 60, ev.End > Cfg.End || ev.Start < Cfg.Start,
            ev.Start < T("13:00") && ev.End > T("12:00"));
    }

    /// <summary>Đánh giá bằng luật mọi cuộc họp chưa có; ở chế độ Llm thì nhờ host hỏi thêm Claude.</summary>
    private void AssessMeetings()
    {
        foreach (var ev in Meetings)
        {
            if (S.Assessments.ContainsKey(ev.Id)) continue;
            var req = BuildMeetingRequest(ev);
            S.Assessments[ev.Id] = MeetingRules.Assess(req);
            if (Cfg.MeetingMode == MeetingMode.Llm && !S.Instant) MeetingWanted?.Invoke(req);
        }
    }

    /// <summary>Đánh giá lại các cuộc họp chưa có kết quả của Claude (vừa bật LLM hoặc vừa nhập API key).</summary>
    public void ReassessMeetings()
    {
        foreach (var id in S.Assessments.Where(kv => kv.Value.Source == "Luật").Select(kv => kv.Key).ToList()) S.Assessments.Remove(id);
        AssessMeetings();
    }

    public void SetMeetingAssessment(MeetingAssessment a)
    {
        if (Meetings.FirstOrDefault(m => m.Id == a.EventId) is not { } ev) return;
        S.Assessments[a.EventId] = a;
        Log($"Đánh giá cuộc họp {Hm(ev.Start)} ({a.Source}): nặng {a.Load}/5 · {a.Kind} · nên nghỉ {a.RecoveryMin}' sau đó — {a.Note}", LogKind.Action);
        if (S.Ep is { } ep) ep.CardVer++;
    }

    public MoodRequest BuildMoodRequest()
    {
        var p = S.Pen;
        string P(string k) => p.TryGetValue(k, out var v) && v >= 0.5 ? $"{k} −{JsRound(v)}" : "";
        var meetings = Meetings.Where(m => m.Start <= S.T).Select(m => S.Assessments.GetValueOrDefault(m.Id))
            .Where(a => a is not null).Select(a => $"nặng {a!.Load}/5 ({a.Kind})").ToList();
        var facts = string.Join("; ", new[]
        {
            $"bây giờ {Hm(S.T)}, khung giờ làm {Hm(Cfg.Start)}–{Hm(Cfg.End)}",
            $"điểm theo luật {S.RuleScore}/100",
            $"đã họp {Dur(MeetingMin())} ({meetings.Count} cuộc: {string.Join(", ", meetings)})",
            $"chuỗi làm liền hiện tại {Dur(Streak())}, tổng nghỉ {S.Rest} phút sau {Dur(Worked())} làm việc",
            $"quá giờ {S.OtMin} phút, chuyển việc {SwitchesHour()} lần/giờ",
            $"{InProgress()} task đang làm (trung bình {Snap.AvgInProgress:0.#}), {StuckTasks().Count} task kẹt, {WaitingEmails().Count} email chờ trả lời",
            $"đã đồng ý nghỉ {S.AcceptedBreaks} lần, tập trung sâu {Dur(S.FocusMinDone)}, xong {S.TasksDone} task",
            S.Feeling > 0 ? $"người dùng tự nói hôm nay thấy: {Feeling.Label(S.Feeling).ToLowerInvariant()}" : "",
            "các khoản trừ theo luật: " + string.Join(", ", new[] { "meet", "chain", "streak", "ot", "rest", "frag", "work", "stuck", "email" }.Select(P).Where(x => x.Length > 0)),
        }.Where(x => x.Length > 0));
        return new MoodRequest(S.RuleScore, facts, Cfg.IncludeChatInMood ? S.ChatHistory.TakeLast(5).ToList() : []);
    }

    public void SetMoodInsight(MoodInsight m)
    {
        S.MoodInsight = m with { At = S.T };
        ComputeMood();
        Log($"Mood ({m.Source}): {m.Label} · {(Cfg.MoodMode == MoodMode.Llm && m.Score is { } s ? $"điểm {s}" : $"chỉnh {m.Adjust:+#;-#;0}")} — {m.Insight}", LogKind.Mood);
    }

    /// <summary>Nhận xét còn dùng được nếu chưa quá 2 chu kỳ hỏi.</summary>
    private MoodInsight? FreshInsight() =>
        S.MoodInsight is { } m && S.T - m.At <= Cfg.MoodIntervalMinutes * 60 * 2 ? m : null;

    /// <summary>Hỏi AI chấm mood ngay (bảng điều khiển vừa bật chế độ AI hoặc bấm "Hỏi AI ngay").</summary>
    public void AskMoodNow()
    {
        if (Cfg.MoodMode == MoodMode.Rules || MoodWanted is null) return;
        S.LastMoodAsk = S.T;
        MoodWanted(BuildMoodRequest());
    }

    private void MaybeAskMood()
    {
        if (Cfg.MoodMode == MoodMode.Rules || MoodWanted is null || S.Instant || !S.DayStarted || S.OffDuty) return;
        if (S.T - S.LastMoodAsk < Cfg.MoodIntervalMinutes * 60) return;
        S.LastMoodAsk = S.T;
        MoodWanted(BuildMoodRequest());
    }
}
