using System.Net;
using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

public sealed record PostSummaryResponse(string Id, string Title, string Slug, string Area, string Date, IReadOnlyList<string> Tags);

public sealed record PostResponse(string Id, string Title, string Slug, string Area, string Date, IReadOnlyList<string> Tags, string Body);

public class PostReadTests
{
    [Test]
    public async Task An_ingested_post_is_listed_in_its_content_area()
    {
        await using var api = new ApiFactory();
        using var ingest = api.CreateIngestClient();
        using var reader = api.CreateClient();
        var (id, slug) = (TestPosts.NewUlid(), TestPosts.NewSlug());
        await Ingest.RunAsync(ingest, TestPosts.Markdown(id, slug, title: "Listed Post", area: "training", date: "2026-09-15"));

        var posts = await reader.GetFromJsonAsync<List<PostSummaryResponse>>("/areas/training/posts");

        var post = posts.ShouldNotBeNull().Where(p => p.Id == id).ShouldHaveSingleItem();
        post.ShouldSatisfyAllConditions(
            () => post.Title.ShouldBe("Listed Post"),
            () => post.Slug.ShouldBe(slug),
            () => post.Area.ShouldBe("training"),
            () => post.Date.ShouldBe("2026-09-15"),
            () => post.Tags.ShouldBe(["fixture", "testing"]));
    }

    [Test]
    public async Task An_ingested_post_is_read_by_area_and_slug()
    {
        await using var api = new ApiFactory();
        using var ingest = api.CreateIngestClient();
        using var reader = api.CreateClient();
        var (id, slug) = (TestPosts.NewUlid(), TestPosts.NewSlug());
        await Ingest.RunAsync(ingest, TestPosts.Markdown(id, slug, title: "Read Me", body: "## Hello\n\nWorld."));

        var post = await reader.GetFromJsonAsync<PostResponse>($"/areas/ai-development/posts/{slug}");

        post.ShouldNotBeNull();
        post.Id.ShouldBe(id);
        post.Title.ShouldBe("Read Me");
        post.Body.ShouldBe("## Hello\n\nWorld.");
    }

    [Test]
    public async Task A_reingested_post_serves_its_new_content()
    {
        await using var api = new ApiFactory();
        using var ingest = api.CreateIngestClient();
        using var reader = api.CreateClient();
        var (id, slug) = (TestPosts.NewUlid(), TestPosts.NewSlug());
        await Ingest.RunAsync(ingest, TestPosts.Markdown(id, slug, title: "Before"));

        await Ingest.RunAsync(ingest, TestPosts.Markdown(id, slug, title: "After"));

        var post = await reader.GetFromJsonAsync<PostResponse>($"/areas/ai-development/posts/{slug}");
        post.ShouldNotBeNull().Title.ShouldBe("After");
    }

    [Test]
    public async Task Posts_in_an_area_are_newest_first()
    {
        await using var api = new ApiFactory();
        using var ingest = api.CreateIngestClient();
        using var reader = api.CreateClient();
        var (older, newer) = (TestPosts.NewUlid(), TestPosts.NewUlid());
        await Ingest.RunAsync(
            ingest,
            TestPosts.Markdown(older, TestPosts.NewSlug(), area: "training", date: "2001-01-01"),
            TestPosts.Markdown(newer, TestPosts.NewSlug(), area: "training", date: "2001-01-02"));

        var posts = await reader.GetFromJsonAsync<List<PostSummaryResponse>>("/areas/training/posts");

        var ids = posts.ShouldNotBeNull().Select(p => p.Id).Where(i => i == older || i == newer).ToList();
        ids.ShouldBe([newer, older]);
    }

    [Test]
    public async Task A_draft_is_not_served()
    {
        await using var api = new ApiFactory();
        using var ingest = api.CreateIngestClient();
        using var reader = api.CreateClient();
        var (id, slug) = (TestPosts.NewUlid(), TestPosts.NewSlug());
        await Ingest.RunAsync(ingest, TestPosts.Markdown(id, slug, status: "draft"));

        var posts = await reader.GetFromJsonAsync<List<PostSummaryResponse>>("/areas/ai-development/posts");
        var post = await reader.GetAsync($"/areas/ai-development/posts/{slug}");

        posts.ShouldNotBeNull().ShouldNotContain(p => p.Id == id);
        post.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task An_unknown_slug_is_not_found()
    {
        await using var api = new ApiFactory();
        using var reader = api.CreateClient();

        var response = await reader.GetAsync($"/areas/recipes/posts/{TestPosts.NewSlug()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task An_unknown_area_is_not_found()
    {
        await using var api = new ApiFactory();
        using var reader = api.CreateClient();

        var response = await reader.GetAsync("/areas/blog/posts");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
