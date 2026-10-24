using System.Net;
using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

public sealed record DraftSummary(string Id, string Slug, bool Draft);

public class DraftTests
{
    private static async Task<(string Draft, string Published, string DraftSlug)> IngestAsync(ApiFactory api)
    {
        using var ingest = api.CreateIngestClient();
        var (draft, published, slug) = (TestPosts.NewUlid(), TestPosts.NewUlid(), TestPosts.NewSlug("draft"));
        await Ingest.RunAsync(
            ingest,
            dryRun: false,
            includeDrafts: true,
            TestPosts.Markdown(draft, slug, area: "training", status: "draft"),
            TestPosts.Markdown(published, TestPosts.NewSlug(), area: "training"));
        return (draft, published, slug);
    }

    [Test]
    public async Task Production_never_serves_drafts()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();
        var (draft, _, slug) = await IngestAsync(api);

        (await client.GetFromJsonAsync<List<DraftSummary>>("/areas/training/posts")).ShouldNotBeNull().ShouldNotContain(p => p.Id == draft);
        (await client.GetFromJsonAsync<List<DraftSummary>>("/posts/latest?limit=50")).ShouldNotBeNull().ShouldNotContain(p => p.Id == draft);
        (await client.GetAsync($"/areas/training/posts/{slug}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Preview_serves_drafts_flagged_as_drafts_in_lists_and_pages()
    {
        await using var api = new ApiFactory().WithSetting("Site:ShowDrafts", "true");
        using var client = api.CreateClient();
        var (draft, published, slug) = await IngestAsync(api);

        var listed = await client.GetFromJsonAsync<List<DraftSummary>>("/areas/training/posts");
        var post = await client.GetFromJsonAsync<DraftSummary>($"/areas/training/posts/{slug}");

        listed.ShouldNotBeNull().Single(p => p.Id == draft).Draft.ShouldBeTrue();
        listed.Single(p => p.Id == published).Draft.ShouldBeFalse();
        post.ShouldNotBeNull().Draft.ShouldBeTrue();
    }

    [Test]
    public async Task Drafts_stay_out_of_search_even_on_preview()
    {
        await using var api = new ApiFactory().WithSetting("Site:ShowDrafts", "true");
        using var client = api.CreateClient();
        using var ingest = api.CreateIngestClient();
        var word = "zq" + Guid.NewGuid().ToString("N")[..8];
        await Ingest.RunAsync(ingest, dryRun: false, includeDrafts: true, TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug(), status: "draft", body: word));

        (await client.GetFromJsonAsync<SearchResponse>($"/search?mode=keyword&q={word}")).ShouldNotBeNull().Results.ShouldBeEmpty();
    }
}
