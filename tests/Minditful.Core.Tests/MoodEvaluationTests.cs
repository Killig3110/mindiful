using Minditful.Core.Engine;
using Xunit;

namespace Minditful.Core.Tests;

/// <summary>
/// Kiểm tra chính bộ kiểm chứng (3 bộ case) bằng AI giả: AI giả giống luật phải đạt hết; AI giả chấm lung tung
/// hoặc chấm ngược phải bị bắt. Nhờ vậy khi chạy với AI thật, kết quả "đạt / chưa đạt" đáng tin.
/// </summary>
public class MoodEvaluationTests
{
    private static Func<string, CancellationToken, Task<int?>> Fake(Func<MoodScenario, int, int?> f)
    {
        var calls = new Dictionary<string, int>();
        return (facts, _) =>
        {
            var s = MoodEvaluation.Scenarios.First(x => MoodEvaluation.Facts(x.Inputs) == facts);
            calls[s.Id] = calls.GetValueOrDefault(s.Id) + 1;
            return Task.FromResult(f(s, calls[s.Id]));
        };
    }

    [Fact]
    public void Rules_suite_passes()
    {
        var rep = MoodEvaluation.RunRules();
        Assert.True(rep.Passed, string.Join("\n", rep.Checks.Where(c => !c.Passed).Select(c => c.Name + ": " + c.Detail)));
        Assert.Equal(MoodEvaluation.Pairs.Length + 5, rep.Checks.Count);
    }

    [Fact]
    public void Facts_carry_numbers_only_and_no_rule_score()
    {
        foreach (var s in MoodEvaluation.Scenarios)
        {
            var f = MoodEvaluation.Facts(s.Inputs);
            Assert.DoesNotContain("luật", f);
            Assert.Contains("họp tổng", f);
        }
    }

    [Fact]
    public async Task An_ai_that_agrees_with_the_rules_passes_all_three_suites()
    {
        var run = await MoodEvaluation.RunLlmAsync("giả · giống luật", Fake((s, k) => Math.Min(100, MoodModel.Score(s.Inputs) + (k % 2))), 3, 0);
        Assert.Equal(MoodEvaluation.RequestsNeeded(3), run.Requests);
        Assert.True(MoodEvaluation.LlmSuite(run).Passed);
        var cmp = MoodEvaluation.Compare(run);
        Assert.True(cmp.Passed, string.Join("\n", cmp.Checks.Select(c => c.Name + ": " + c.Detail)));
    }

    [Fact]
    public async Task An_unstable_ai_is_caught()
    {
        var rnd = new Random(1);
        var run = await MoodEvaluation.RunLlmAsync("giả · chấm lung tung", Fake((s, _) => rnd.Next(0, 101)), 3, 0);
        var rep = MoodEvaluation.LlmSuite(run);
        Assert.False(rep.Checks.First(c => c.Name.StartsWith("Ổn định")).Passed);
    }

    [Fact]
    public async Task An_ai_that_scores_backwards_is_caught_by_suites_2_and_3()
    {
        var run = await MoodEvaluation.RunLlmAsync("giả · chấm ngược", Fake((s, _) => 100 - MoodModel.Score(s.Inputs)), 2, 0);
        Assert.False(MoodEvaluation.LlmSuite(run).Passed);
        var cmp = MoodEvaluation.Compare(run);
        Assert.False(cmp.Checks.First(c => c.Name.StartsWith("Tương quan")).Passed);
        Assert.False(cmp.Checks.First(c => c.Name.StartsWith("Cùng thứ tự")).Passed);
    }

    [Fact]
    public async Task An_ai_that_often_fails_to_answer_is_caught()
    {
        var run = await MoodEvaluation.RunLlmAsync("giả · hay lỗi", Fake((s, k) => k == 1 ? null : MoodModel.Score(s.Inputs)), 2, 0);
        Assert.False(MoodEvaluation.LlmSuite(run).Checks[0].Passed);
        var md = MoodEvaluation.ToMarkdown(new DateTime(2026, 9, 29), MoodEvaluation.RunRules(500), MoodEvaluation.LlmSuite(run), MoodEvaluation.Compare(run));
        Assert.Contains("CHƯA ĐẠT", md);
        Assert.Contains("Bộ 3", md);
    }
}
