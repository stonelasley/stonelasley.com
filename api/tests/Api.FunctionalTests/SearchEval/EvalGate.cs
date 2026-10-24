namespace StoneLasley.Api.FunctionalTests.SearchEval;

public sealed record TypeScores(double Ndcg5, double Recall10, double Mrr, int Queries);

public sealed record ModeScores(TypeScores Overall, IReadOnlyDictionary<string, TypeScores> ByType);

/// <summary>Scores per search mode (hybrid, keyword, vector), as committed in scores.json.</summary>
public sealed record EvalScores(IReadOnlyDictionary<string, ModeScores> Modes);

/// <summary>The pass rules for search quality, all on nDCG@5.</summary>
public static class EvalGate
{
    /// <summary>How far hybrid may trail the better single leg on one query type.</summary>
    public const double TypeSlack = 0.05;

    /// <summary>How far a query type's hybrid score may fall against the committed scores.</summary>
    public const double RegressionSlack = 0.02;

    // Scores are rounded when committed; this keeps a boundary value from failing on float noise.
    private const double Epsilon = 1e-9;

    public static IReadOnlyList<string> Failures(EvalScores current, EvalScores? committed)
    {
        var failures = new List<string>();
        var hybrid = current.Modes["hybrid"];
        foreach (var leg in new[] { "keyword", "vector" })
        {
            var legScore = current.Modes[leg].Overall.Ndcg5;
            if (hybrid.Overall.Ndcg5 <= legScore + Epsilon)
            {
                failures.Add($"hybrid nDCG@5 {hybrid.Overall.Ndcg5:F3} does not beat {leg} {legScore:F3} overall");
            }
        }

        foreach (var (type, scores) in hybrid.ByType)
        {
            var best = Math.Max(current.Modes["keyword"].ByType[type].Ndcg5, current.Modes["vector"].ByType[type].Ndcg5);
            if (scores.Ndcg5 < best - TypeSlack - Epsilon)
            {
                failures.Add($"hybrid nDCG@5 {scores.Ndcg5:F3} on {type} is more than {TypeSlack} behind the best leg ({best:F3})");
            }

            if (committed?.Modes["hybrid"].ByType.GetValueOrDefault(type) is { } before
                && scores.Ndcg5 < before.Ndcg5 - RegressionSlack - Epsilon)
            {
                failures.Add($"hybrid nDCG@5 on {type} fell from {before.Ndcg5:F3} to {scores.Ndcg5:F3} (more than {RegressionSlack})");
            }
        }
        return failures;
    }
}
