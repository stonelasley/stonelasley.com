using System.Net;
using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

public sealed record RebuildResponse(string Version, int Posts, int Vectors);

/// <summary>
/// Bumping the index version (embedding model + chunker) builds a new index while search keeps reading the old
/// one, then switches. On the in-process index, which keeps one store per version.
/// </summary>
public class IndexVersionTests
{
    private static Task<SearchResponse?> VectorSearchAsync(HttpClient client, string query) =>
        client.GetFromJsonAsync<SearchResponse>($"/search?mode=vector&q={Uri.EscapeDataString(query)}");

    [Test]
    public async Task A_version_bump_builds_the_new_index_alongside_and_switches_only_when_promoted()
    {
        var api = new ApiFactory().WithSetting("Search:Vector:IndexVersion", "v1");
        using var ingest = api.CreateIngestClient();
        var id = TestPosts.NewUlid();
        await Ingest.RunAsync(ingest, TestPosts.Markdown(id, TestPosts.NewSlug(), title: "Kettlebells", body: "Kettlebell swings build the posterior chain."));
        using var admin = api.CreateAdminClient();

        var rebuild = await admin.PostAsync("/admin/vectors/rebuild?version=v2", null);

        rebuild.StatusCode.ShouldBe(HttpStatusCode.OK);
        var built = await rebuild.Content.ReadFromJsonAsync<RebuildResponse>();
        built.ShouldNotBeNull().Posts.ShouldBeGreaterThanOrEqualTo(1);
        // Search still reads v1 during and after the build.
        using (var reader = api.CreateClient())
        {
            (await VectorSearchAsync(reader, "kettlebell swings posterior chain"))!.Results.ShouldContain(r => r.Id == id);
        }

        // Promote: the API now runs on v2 (in a deploy, its Search:Vector:IndexVersion changes) and adopts the build.
        await using var promoted = api.WithSettingOnNewHost("Search:Vector:IndexVersion", "v2");
        using var promotedAdmin = promoted.CreateAdminClient();
        (await promotedAdmin.PostAsync("/admin/vectors/promote?version=v2", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var promotedReader = promoted.CreateClient();
        (await VectorSearchAsync(promotedReader, "kettlebell swings posterior chain"))!.Results.ShouldContain(r => r.Id == id);
        await api.DisposeAsync();
    }

    [Test]
    public async Task Promoting_a_version_the_api_is_not_running_is_refused()
    {
        await using var api = new ApiFactory().WithSetting("Search:Vector:IndexVersion", "v1");
        using var admin = api.CreateAdminClient();

        (await admin.PostAsync("/admin/vectors/promote?version=v2", null)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Rebuilding_the_active_version_is_refused()
    {
        await using var api = new ApiFactory().WithSetting("Search:Vector:IndexVersion", "v1");
        using var admin = api.CreateAdminClient();

        (await admin.PostAsync("/admin/vectors/rebuild?version=v1", null)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Promoting_without_a_build_changes_nothing()
    {
        await using var api = new ApiFactory().WithSetting("Search:Vector:IndexVersion", "v9");
        using var admin = api.CreateAdminClient();

        (await admin.PostAsync("/admin/vectors/promote?version=v9", null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
