namespace Minditful.Core.Engine;

/// <summary>Loại phản hồi được ghi log cho mỗi lời nhắc (§14 · Log và cá nhân hoá).</summary>
public enum Outcome { Shown, Accepted, Snoozed, Dismissed, Ignored, Chat, Gated, Folded }

public sealed record OutcomeEvent(DateOnly Day, double T, CaseId Case, Outcome Kind, int Score);

/// <summary>Điều chỉnh cho 1 case: nhân thời gian chờ, nâng ngưỡng kích hoạt, ưu tiên khoảng trống dài nhất.</summary>
public sealed record CaseTuning(double WaitFactor = 1, double ThresholdFactor = 1, bool PreferLongGap = false)
{
    public static readonly CaseTuning None = new();
    public bool IsNone => WaitFactor == 1 && ThresholdFactor == 1 && !PreferLongGap;
}

/// <summary>
/// Luật cá nhân hoá theo 7 ngày (§14):
/// 2. Case hiện ≥ 5 lần mà ≥ 60% là Không cần hoặc bị bỏ qua → thời gian chờ ×2, ngưỡng kích hoạt +15%.
/// 3. Case bị Để sau ≥ 60% → giao ở khoảng trống dài nhất kế tiếp thay vì khoảng trống đầu tiên.
/// Không bao giờ tắt hẳn một case — chỉ làm nó thưa đi. (Luật 1 — trong ngày — nằm trong engine.)
/// </summary>
public static class Personalizer
{
    public const int WindowDays = 7;
    public const int MinShown = 5;
    public const double Share = 0.6;

    public static IReadOnlyDictionary<CaseId, CaseTuning> Compute(IEnumerable<OutcomeEvent> events, DateOnly today)
    {
        var from = today.AddDays(-WindowDays);
        var result = new Dictionary<CaseId, CaseTuning>();
        foreach (var g in events.Where(x => x.Day >= from && x.Day < today).GroupBy(x => x.Case))
        {
            if (g.Key is CaseId.Dashboard or CaseId.Talk or CaseId.TaskDone or CaseId.FocusDone or CaseId.MeetingSoon or CaseId.MorningHello) continue;
            var shown = g.Count(x => x.Kind == Outcome.Shown);
            if (shown < MinShown) continue;
            var rejected = g.Count(x => x.Kind is Outcome.Dismissed or Outcome.Ignored);
            var snoozed = g.Count(x => x.Kind == Outcome.Snoozed);
            var t = CaseTuning.None;
            if (rejected >= Share * shown) t = t with { WaitFactor = 2, ThresholdFactor = 1.15 };
            if (snoozed >= Share * shown) t = t with { PreferLongGap = true };
            if (!t.IsNone) result[g.Key] = t;
        }
        return result;
    }

    public static string Describe(IReadOnlyDictionary<CaseId, CaseTuning> tuning)
    {
        if (tuning.Count == 0) return "Chưa có điều chỉnh (cần ≥ 5 lần hiện/case trong 7 ngày).";
        return string.Join("; ", tuning.Select(kv =>
        {
            var parts = new List<string>();
            if (kv.Value.WaitFactor != 1) parts.Add($"chờ ×{kv.Value.WaitFactor:0.#}, ngưỡng +{(kv.Value.ThresholdFactor - 1) * 100:0}%");
            if (kv.Value.PreferLongGap) parts.Add("giao ở khoảng trống dài nhất");
            return $"{Catalog.Def(kv.Key).Name}: {string.Join(", ", parts)}";
        }));
    }
}
