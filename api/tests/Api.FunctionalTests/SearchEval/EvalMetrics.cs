namespace StoneLasley.Api.FunctionalTests.SearchEval;

/// <summary>
/// Ranking metrics over one query's results. <c>judged</c> maps each relevant Post's slug to its grade
/// (2 = the answer, 1 = also relevant); anything unjudged counts as irrelevant.
/// </summary>
public static class EvalMetrics
{
    /// <summary>Normalised discounted cumulative gain at <paramref name="k"/>, with gain 2^grade - 1.</summary>
    public static double NdcgAt(int k, IReadOnlyList<string> ranked, IReadOnlyDictionary<string, int> judged)
    {
        var ideal = Dcg(judged.Values.OrderDescending().Take(k));
        return ideal == 0 ? 0 : Dcg(ranked.Take(k).Select(slug => judged.GetValueOrDefault(slug))) / ideal;
    }

    public static double RecallAt(int k, IReadOnlyList<string> ranked, IReadOnlyDictionary<string, int> judged) =>
        judged.Count == 0 ? 0 : (double)ranked.Take(k).Count(judged.ContainsKey) / judged.Count;

    public static double ReciprocalRank(IReadOnlyList<string> ranked, IReadOnlyDictionary<string, int> judged)
    {
        for (var i = 0; i < ranked.Count; i++)
        {
            if (judged.ContainsKey(ranked[i]))
            {
                return 1.0 / (i + 1);
            }
        }
        return 0;
    }

    private static double Dcg(IEnumerable<int> grades) =>
        grades.Select((grade, i) => (Math.Pow(2, grade) - 1) / Math.Log2(i + 2)).Sum();
}
