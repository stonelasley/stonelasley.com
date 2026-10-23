using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

public class IngestTests
{
    [Test]
    public async Task Manifest_requests_posts_the_api_has_not_seen()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var id = TestPosts.NewUlid();

        var manifest = await Ingest.ManifestAsync(client, new Dictionary<string, string> { [id] = TestPosts.Markdown(id, TestPosts.NewSlug()) });

        manifest.Requested.ShouldBe([id]);
    }

    [Test]
    public async Task Manifest_skips_unchanged_posts_and_requests_changed_ones()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var (unchanged, changed) = (TestPosts.NewUlid(), TestPosts.NewUlid());
        var unchangedMarkdown = TestPosts.Markdown(unchanged, TestPosts.NewSlug());
        var changedSlug = TestPosts.NewSlug();
        await Ingest.RunAsync(client, unchangedMarkdown, TestPosts.Markdown(changed, changedSlug));

        var manifest = await Ingest.ManifestAsync(client, new Dictionary<string, string>
        {
            [unchanged] = unchangedMarkdown,
            [changed] = TestPosts.Markdown(changed, changedSlug, title: "A new title"),
        });

        manifest.Requested.ShouldBe([changed]);
    }

    [Test]
    public async Task Upload_rejects_invalid_frontmatter()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var id = TestPosts.NewUlid();
        var markdown = TestPosts.Markdown(id, "Not A Slug");
        var manifest = await Ingest.ManifestAsync(client, new Dictionary<string, string> { [id] = markdown });

        var response = await Ingest.UploadAsync(client, manifest.RunId, id, markdown);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("'slug' must be lowercase letters");
    }

    [Test]
    public async Task Upload_rejects_a_post_whose_id_does_not_match_the_route()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var id = TestPosts.NewUlid();
        var markdown = TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug());
        var manifest = await Ingest.ManifestAsync(client, new Dictionary<string, string> { [id] = markdown });

        var response = await Ingest.UploadAsync(client, manifest.RunId, id, markdown);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Commit_fails_while_a_requested_post_is_missing()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var id = TestPosts.NewUlid();
        var manifest = await Ingest.ManifestAsync(client, new Dictionary<string, string> { [id] = TestPosts.Markdown(id, TestPosts.NewSlug()) });

        var response = await Ingest.CommitAsync(client, manifest.RunId);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain(id);
    }

    [Test]
    public async Task Commit_fails_for_an_unknown_run()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();

        var response = await Ingest.CommitAsync(client, "no-such-run");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestCase(null)]
    [TestCase("not-the-ingest-token")]
    public async Task Ingest_requires_the_ingest_token(string? token)
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var manifest = await client.PostAsJsonAsync("/ingest/manifest", new { posts = new Dictionary<string, string>() });
        var upload = await client.PutAsJsonAsync($"/ingest/runs/any/posts/{TestPosts.NewUlid()}", new { markdown = "" });
        var commit = await client.PostAsync("/ingest/runs/any/commit", null);

        manifest.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        upload.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        commit.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
