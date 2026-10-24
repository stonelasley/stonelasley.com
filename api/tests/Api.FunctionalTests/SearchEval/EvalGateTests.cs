namespace StoneLasley.Api.FunctionalTests.SearchEval;

public class EvalGateTests
{
    // Hybrid leads overall; on exact it trails keyword by exactly the allowed 0.05.
    private static EvalScores Scores(double hybridExact = 0.85, double hybridConceptual = 0.80, double keywordOverall = 0.70) => new(
        new Dictionary<string, ModeScores>
        {
            ["hybrid"] = Mode(overall: 0.82, exact: hybridExact, conceptual: hybridConceptual),
            ["keyword"] = Mode(overall: keywordOverall, exact: 0.90, conceptual: 0.40),
            ["vector"] = Mode(overall: 0.75, exact: 0.70, conceptual: 0.78),
        });

    [Test]
    public void Hybrid_that_leads_overall_and_stays_close_on_each_type_passes()
    {
        EvalGate.Failures(Scores(), committed: null).ShouldBeEmpty();
    }

    [Test]
    public void Hybrid_must_beat_both_legs_overall_not_tie()
    {
        EvalGate.Failures(Scores(keywordOverall: 0.82), committed: null)
            .ShouldHaveSingleItem().ShouldContain("keyword");
    }

    [Test]
    public void Hybrid_more_than_005_behind_the_best_leg_on_a_type_fails()
    {
        EvalGate.Failures(Scores(hybridExact: 0.849), committed: null)
            .ShouldHaveSingleItem().ShouldContain("exact");
    }

    [Test]
    public void A_per_type_drop_of_more_than_002_against_the_committed_scores_fails()
    {
        EvalGate.Failures(Scores(hybridConceptual: 0.77), committed: Scores(hybridConceptual: 0.80))
            .ShouldHaveSingleItem().ShouldContain("conceptual");
        EvalGate.Failures(Scores(hybridConceptual: 0.78), committed: Scores(hybridConceptual: 0.80)).ShouldBeEmpty();
    }

    private static ModeScores Mode(double overall, double exact, double conceptual) => new(
        new TypeScores(overall, 0, 0, 2),
        new Dictionary<string, TypeScores>
        {
            ["exact"] = new(exact, 0, 0, 1),
            ["conceptual"] = new(conceptual, 0, 0, 1),
        });
}
