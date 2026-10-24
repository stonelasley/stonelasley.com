using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

public class PreviewTests
{
    [Test]
    public async Task Preview_serves_everything_with_noindex()
    {
        await using var api = new ApiFactory().WithSetting("Site:NoIndex", "true");
        using var client = api.CreateClient();

        var health = await client.GetAsync("/health");
        var missing = await client.GetAsync("/areas/blog/posts");

        health.Headers.GetValues("X-Robots-Tag").ShouldBe(["noindex"]);
        missing.Headers.GetValues("X-Robots-Tag").ShouldBe(["noindex"]);
    }

    [Test]
    public async Task Production_serves_without_noindex()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();

        var health = await client.GetAsync("/health");

        health.Headers.Contains("X-Robots-Tag").ShouldBeFalse();
    }
}

public class RevalidationTests
{
    [Test]
    public async Task A_commit_that_changes_posts_revalidates_the_site()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();

        await Ingest.RunAsync(client, TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug()));

        api.Revalidator.Calls.ShouldBe(1);
    }

    [Test]
    public async Task A_dry_run_does_not_revalidate()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();

        await Ingest.RunAsync(client, dryRun: true, includeDrafts: false, TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug()));

        api.Revalidator.Calls.ShouldBe(0);
    }

    [Test]
    public async Task A_commit_that_changes_nothing_does_not_revalidate()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var markdown = TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug());
        await Ingest.RunAsync(client, markdown);

        await Ingest.RunAsync(client, markdown);

        api.Revalidator.Calls.ShouldBe(1);
    }

    [Test]
    public async Task A_commit_that_changes_intros_revalidates_the_site()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var manifest = await client.PostAsJsonAsync(
            "/ingest/manifest",
            new { posts = new Dictionary<string, string>(), intros = new Dictionary<string, string> { ["recipes"] = "New intro." } });

        await Ingest.CommitAsync(client, (await manifest.Content.ReadFromJsonAsync<ManifestResponse>())!.RunId);

        api.Revalidator.Calls.ShouldBe(1);
    }
}
