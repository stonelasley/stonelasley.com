namespace StoneLasley.Api.FunctionalTests.SearchEval;

public class EvalMetricsTests
{
    private static readonly Dictionary<string, int> Judged = new() { ["a"] = 2, ["b"] = 1 };

    [Test]
    public void Ndcg_rewards_graded_hits_by_rank()
    {
        // DCG = (2^1-1)/log2(2) + (2^2-1)/log2(4) = 2.5; ideal = 3/log2(2) + 1/log2(3) ≈ 3.63093.
        EvalMetrics.NdcgAt(5, ["b", "x", "a"], Judged).ShouldBe(0.68853, tolerance: 0.00001);
    }

    [Test]
    public void Ndcg_is_one_for_the_ideal_ranking_and_zero_with_no_hits()
    {
        EvalMetrics.NdcgAt(5, ["a", "b"], Judged).ShouldBe(1, tolerance: 0.00001);
        EvalMetrics.NdcgAt(5, ["x", "y"], Judged).ShouldBe(0);
    }

    [Test]
    public void Ndcg_ignores_hits_below_the_cutoff()
    {
        EvalMetrics.NdcgAt(2, ["x", "y", "a", "b"], Judged).ShouldBe(0);
    }

    [Test]
    public void Recall_counts_judged_posts_found_in_the_top_k()
    {
        var judged = new Dictionary<string, int> { ["a"] = 2, ["b"] = 1, ["c"] = 1 };

        EvalMetrics.RecallAt(10, ["a", "x", "c"], judged).ShouldBe(2.0 / 3, tolerance: 0.00001);
    }

    [Test]
    public void Reciprocal_rank_is_one_over_the_first_judged_hit()
    {
        EvalMetrics.ReciprocalRank(["x", "y", "b"], Judged).ShouldBe(1.0 / 3, tolerance: 0.00001);
        EvalMetrics.ReciprocalRank(["x"], Judged).ShouldBe(0);
    }
}
