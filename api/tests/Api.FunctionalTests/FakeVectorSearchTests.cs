using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

/// <summary>
/// The vector pipeline end to end on the in-process fake (word overlap, not meaning). Runs everywhere, fork PRs
/// included, so ingest → chunk → embed → index → query → roll-up is always exercised.
/// </summary>
public class FakeVectorSearchTests
{
    private static Task<SearchResponse?> SearchAsync(HttpClient client, string query, string? area = null) =>
        client.GetFromJsonAsync<SearchResponse>($"/search?mode=vector&q={Uri.EscapeDataString(query)}" + (area is null ? "" : $"&area={area}"));

    [Test]
    public async Task Vector_mode_finds_posts_by_their_chunks_and_filters_by_area()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var (training, watches) = (TestPosts.NewUlid(), TestPosts.NewUlid());
        await Ingest.RunAsync(
            client,
            TestPosts.Markdown(training, TestPosts.NewSlug(), title: "Squat depth", area: "training", body: "## Depth\n\nSquat below parallel with knees tracking the toes."),
            TestPosts.Markdown(watches, TestPosts.NewSlug("watch"), title: "Bezel", area: "watches", body: "Unidirectional bezel insert with lume pip.", extra: TestPosts.WatchCore()));

        var squat = await SearchAsync(client, "squat below parallel knees toes");
        var inWatches = await SearchAsync(client, "squat below parallel knees toes", area: "watches");

        squat.ShouldNotBeNull().Results.First().Id.ShouldBe(training);
        string.Concat(squat.Results.First().Excerpt.Select(p => p.Text)).ShouldBe("Squat below parallel with knees tracking the toes.");
        inWatches.ShouldNotBeNull().Results.ShouldNotContain(r => r.Id == training);
    }

    [Test]
    public async Task A_rewritten_post_is_found_by_its_new_words_only()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var (id, slug) = (TestPosts.NewUlid(), TestPosts.NewSlug());
        await Ingest.RunAsync(client, TestPosts.Markdown(id, slug, title: "Notes", body: "Kettlebell swings and snatches daily."));

        await Ingest.RunAsync(client, TestPosts.Markdown(id, slug, title: "Notes", body: "Sourdough starter feeding schedule."));

        (await SearchAsync(client, "kettlebell swings snatches")).ShouldNotBeNull().Results.ShouldNotContain(r => r.Id == id);
        (await SearchAsync(client, "sourdough starter feeding")).ShouldNotBeNull().Results.ShouldContain(r => r.Id == id);
    }
}
