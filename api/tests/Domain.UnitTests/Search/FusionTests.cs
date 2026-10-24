using StoneLasley.Domain.Search;

namespace StoneLasley.Domain.UnitTests.Search;

public class FusionTests
{
    [Test]
    public void Reciprocal_rank_fusion_sums_one_over_k_plus_rank_from_each_list()
    {
        var fused = Fusion.ReciprocalRank(
            [new RankedList(["a", "b", "c"], 1), new RankedList(["b", "d"], 1)],
            k: 60);

        // b: 1/62 + 1/61; a: 1/61; d: 1/62; c: 1/63
        fused.Select(f => f.Id).ShouldBe(["b", "a", "d", "c"]);
        fused[0].Score.ShouldBe((1.0 / 62) + (1.0 / 61), 1e-12);
    }

    [Test]
    public void Leg_weights_scale_each_lists_contribution()
    {
        var fused = Fusion.ReciprocalRank(
            [new RankedList(["a"], 1), new RankedList(["b"], 2)],
            k: 60);

        fused.Select(f => f.Id).ShouldBe(["b", "a"]);
        fused[0].Score.ShouldBe(2.0 / 61, 1e-12);
    }

    [Test]
    public void Ties_keep_the_first_lists_order()
    {
        var fused = Fusion.ReciprocalRank([new RankedList(["a", "b"], 1), new RankedList(["b", "a"], 1)], k: 60);

        fused.Select(f => f.Id).ShouldBe(["a", "b"]);
    }

    [Test]
    public void Empty_lists_fuse_to_nothing()
    {
        Fusion.ReciprocalRank([new RankedList([], 1), new RankedList([], 1)], k: 60).ShouldBeEmpty();
    }
}

public class ChunkRollupTests
{
    [Test]
    public void Scores_each_post_by_its_best_chunk_above_the_floor()
    {
        var posts = ChunkRollup.BestPerPost(
        [
            new ChunkScore("a", 0, 0.70),
            new ChunkScore("a", 3, 0.90),
            new ChunkScore("b", 1, 0.80),
            new ChunkScore("c", 0, 0.55),
        ],
        floor: 0.6);

        posts.ShouldBe([new ChunkScore("a", 3, 0.90), new ChunkScore("b", 1, 0.80)]);
    }

    [Test]
    public void Nothing_survives_when_every_chunk_is_under_the_floor()
    {
        ChunkRollup.BestPerPost([new ChunkScore("a", 0, 0.4)], floor: 0.6).ShouldBeEmpty();
    }
}
