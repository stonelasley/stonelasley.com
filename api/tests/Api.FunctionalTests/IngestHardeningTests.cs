using System.Net;
using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

/// <summary>
/// Ingest is safe to run from CI: changed-only, delete-by-absence, all-or-nothing, dry-run and the Drafts flag.
/// Each run sends the full set, so these tests read only the Posts they ingested themselves.
/// </summary>
public class IngestHardeningTests
{
    [Test]
    public async Task A_post_absent_from_the_manifest_is_deleted_on_commit()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var (kept, removed) = (TestPosts.NewUlid(), TestPosts.NewUlid());
        var keptMarkdown = TestPosts.Markdown(kept, TestPosts.NewSlug());
        var removedSlug = TestPosts.NewSlug();
        await Ingest.RunAsync(client, keptMarkdown, TestPosts.Markdown(removed, removedSlug));

        var commit = await Ingest.RunAsync(client, keptMarkdown);

        commit.Deleted.ShouldContain(removed);
        commit.Deleted.ShouldNotContain(kept);
        (await client.GetAsync($"/areas/ai-development/posts/{removedSlug}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Rerunning_the_same_ingest_changes_nothing()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var markdown = TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug());
        await Ingest.RunAsync(client, markdown);

        var manifest = await Ingest.ManifestAsync(client, Ingest.ById(markdown));
        var commit = await Ingest.CommitAsync(client, manifest.RunId);

        manifest.Requested.ShouldBeEmpty();
        var result = await commit.Content.ReadFromJsonAsync<CommitResponse>();
        result.ShouldNotBeNull().Upserted.ShouldBeEmpty();
        result.Deleted.ShouldBeEmpty();
    }

    [Test]
    public async Task An_invalid_post_fails_the_whole_run_and_leaves_stored_posts_untouched()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var (good, bad) = (TestPosts.NewUlid(), TestPosts.NewUlid());
        var slug = TestPosts.NewSlug();
        await Ingest.RunAsync(client, TestPosts.Markdown(good, slug, title: "Original"));

        var changed = TestPosts.Markdown(good, slug, title: "Changed");
        var invalid = TestPosts.Markdown(bad, TestPosts.NewSlug(), date: "someday");
        var manifest = await Ingest.ManifestAsync(client, Ingest.ById(changed, invalid));
        (await Ingest.UploadAsync(client, manifest.RunId, good, changed)).EnsureSuccessStatusCode();
        var upload = await Ingest.UploadAsync(client, manifest.RunId, bad, invalid);
        var commit = await Ingest.CommitAsync(client, manifest.RunId);

        upload.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await upload.Content.ReadAsStringAsync()).ShouldContain("'date' must be a date");
        commit.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var post = await client.GetFromJsonAsync<PostResponse>($"/areas/ai-development/posts/{slug}");
        post.ShouldNotBeNull().Title.ShouldBe("Original");
    }

    [Test]
    public async Task Two_posts_with_the_same_slug_in_an_area_fail_the_run()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var (first, second) = (TestPosts.NewUlid(), TestPosts.NewUlid());
        var slug = TestPosts.NewSlug();
        var byId = Ingest.ById(TestPosts.Markdown(first, slug), TestPosts.Markdown(second, slug));
        var manifest = await Ingest.ManifestAsync(client, byId);
        foreach (var id in manifest.Requested)
        {
            (await Ingest.UploadAsync(client, manifest.RunId, id, byId[id])).EnsureSuccessStatusCode();
        }

        var commit = await Ingest.CommitAsync(client, manifest.RunId);

        commit.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await commit.Content.ReadAsStringAsync()).ShouldContain($"slug '{slug}'");
        (await client.GetAsync($"/areas/ai-development/posts/{slug}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task The_same_slug_in_different_areas_is_allowed()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var slug = TestPosts.NewSlug();

        await Ingest.RunAsync(
            client,
            TestPosts.Markdown(TestPosts.NewUlid(), slug, area: "recipes", extra: TestPosts.RecipeCore),
            TestPosts.Markdown(TestPosts.NewUlid(), slug, area: "training"));

        (await client.GetAsync($"/areas/recipes/posts/{slug}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync($"/areas/training/posts/{slug}")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task A_post_may_take_the_slug_of_a_post_deleted_in_the_same_run()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var slug = TestPosts.NewSlug();
        await Ingest.RunAsync(client, TestPosts.Markdown(TestPosts.NewUlid(), slug, title: "Old"));

        await Ingest.RunAsync(client, TestPosts.Markdown(TestPosts.NewUlid(), slug, title: "New"));

        var post = await client.GetFromJsonAsync<PostResponse>($"/areas/ai-development/posts/{slug}");
        post.ShouldNotBeNull().Title.ShouldBe("New");
    }

    [Test]
    public async Task A_dry_run_reports_what_would_change_and_writes_nothing()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var (existing, added) = (TestPosts.NewUlid(), TestPosts.NewUlid());
        var existingSlug = TestPosts.NewSlug();
        var addedSlug = TestPosts.NewSlug();
        await Ingest.RunAsync(client, TestPosts.Markdown(existing, existingSlug));

        var commit = await Ingest.RunAsync(client, dryRun: true, includeDrafts: false, TestPosts.Markdown(added, addedSlug));

        commit.DryRun.ShouldBeTrue();
        commit.Upserted.ShouldBe([added]);
        commit.Deleted.ShouldContain(existing);
        (await client.GetAsync($"/areas/ai-development/posts/{existingSlug}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync($"/areas/ai-development/posts/{addedSlug}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task A_dry_run_still_fails_on_an_invalid_set()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var slug = TestPosts.NewSlug();
        var byId = Ingest.ById(TestPosts.Markdown(TestPosts.NewUlid(), slug), TestPosts.Markdown(TestPosts.NewUlid(), slug));
        var manifest = await Ingest.ManifestAsync(client, byId, dryRun: true);
        foreach (var id in manifest.Requested)
        {
            (await Ingest.UploadAsync(client, manifest.RunId, id, byId[id])).EnsureSuccessStatusCode();
        }

        var commit = await Ingest.CommitAsync(client, manifest.RunId);

        commit.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task With_drafts_excluded_a_draft_is_not_stored()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var (id, slug) = (TestPosts.NewUlid(), TestPosts.NewSlug());
        var draft = TestPosts.Markdown(id, slug, status: "draft");

        var commit = await Ingest.RunAsync(client, dryRun: false, includeDrafts: false, draft);

        commit.Upserted.ShouldBeEmpty();
        (await client.GetAsync($"/areas/ai-development/posts/{slug}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task An_unchanged_excluded_draft_is_not_requested_again()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var id = TestPosts.NewUlid();
        var slug = TestPosts.NewSlug();
        var draft = TestPosts.Markdown(id, slug, status: "draft");
        await Ingest.RunAsync(client, draft);
        await Ingest.RunAsync(client, draft);

        (await Ingest.ManifestAsync(client, Ingest.ById(draft))).Requested.ShouldBeEmpty();
        (await Ingest.ManifestAsync(client, Ingest.ById(TestPosts.Markdown(id, slug)))).Requested.ShouldBe([id]);
        (await Ingest.ManifestAsync(client, Ingest.ById(draft), includeDrafts: true)).Requested.ShouldBe([id]);
    }

    [Test]
    public async Task With_drafts_included_a_draft_is_stored_but_not_served()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var (id, slug) = (TestPosts.NewUlid(), TestPosts.NewSlug());
        var draft = TestPosts.Markdown(id, slug, status: "draft");

        var commit = await Ingest.RunAsync(client, dryRun: false, includeDrafts: true, draft);

        commit.Upserted.ShouldBe([id]);
        (await Ingest.ManifestAsync(client, Ingest.ById(draft), includeDrafts: true)).Requested.ShouldBeEmpty();
        (await client.GetAsync($"/areas/ai-development/posts/{slug}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task A_published_post_turned_draft_is_removed_when_drafts_are_excluded()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var (id, slug) = (TestPosts.NewUlid(), TestPosts.NewSlug());
        await Ingest.RunAsync(client, TestPosts.Markdown(id, slug));

        var commit = await Ingest.RunAsync(client, TestPosts.Markdown(id, slug, status: "draft"));

        commit.Deleted.ShouldBe([id]);
        (await client.GetAsync($"/areas/ai-development/posts/{slug}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Upload_rejects_a_post_the_manifest_did_not_request()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var markdown = TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug());
        await Ingest.RunAsync(client, markdown);
        var id = Ingest.ById(markdown).Keys.Single();
        var manifest = await Ingest.ManifestAsync(client, Ingest.ById(markdown));

        var upload = await Ingest.UploadAsync(client, manifest.RunId, id, markdown);

        upload.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await upload.Content.ReadAsStringAsync()).ShouldContain("was not requested");
    }

    [Test]
    public async Task A_committed_run_cannot_be_committed_again()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var manifest = await Ingest.ManifestAsync(client, new Dictionary<string, string>());
        (await Ingest.CommitAsync(client, manifest.RunId)).EnsureSuccessStatusCode();

        var again = await Ingest.CommitAsync(client, manifest.RunId);

        again.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Two_posts_can_swap_slugs_in_one_run()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var (first, second) = (TestPosts.NewUlid(), TestPosts.NewUlid());
        var (slugA, slugB) = (TestPosts.NewSlug(), TestPosts.NewSlug());
        await Ingest.RunAsync(client, TestPosts.Markdown(first, slugA, title: "First"), TestPosts.Markdown(second, slugB, title: "Second"));

        await Ingest.RunAsync(client, TestPosts.Markdown(first, slugB, title: "First"), TestPosts.Markdown(second, slugA, title: "Second"));

        (await client.GetFromJsonAsync<PostResponse>($"/areas/ai-development/posts/{slugA}")).ShouldNotBeNull().Id.ShouldBe(second);
        (await client.GetFromJsonAsync<PostResponse>($"/areas/ai-development/posts/{slugB}")).ShouldNotBeNull().Id.ShouldBe(first);
    }

    [Test]
    public async Task A_stored_draft_is_removed_by_a_run_that_excludes_drafts()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var id = TestPosts.NewUlid();
        var draft = TestPosts.Markdown(id, TestPosts.NewSlug(), status: "draft");
        await Ingest.RunAsync(client, dryRun: false, includeDrafts: true, draft);

        var commit = await Ingest.RunAsync(client, dryRun: false, includeDrafts: false, draft);

        commit.Deleted.ShouldBe([id]);
    }

    [Test]
    public async Task A_stored_draft_does_not_block_a_slug_when_drafts_are_excluded()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var slug = TestPosts.NewSlug();
        var draft = TestPosts.Markdown(TestPosts.NewUlid(), slug, status: "draft");
        await Ingest.RunAsync(client, dryRun: false, includeDrafts: true, draft);

        await Ingest.RunAsync(client, dryRun: false, includeDrafts: false, draft, TestPosts.Markdown(TestPosts.NewUlid(), slug, title: "Published"));

        (await client.GetFromJsonAsync<PostResponse>($"/areas/ai-development/posts/{slug}")).ShouldNotBeNull().Title.ShouldBe("Published");
    }
}
