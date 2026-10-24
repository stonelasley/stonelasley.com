using System.Net;
using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

public sealed record MediaResponse(string Id, string ImageUrl, string Permalink, string? Caption, string Timestamp);

public class InstagramTests
{
    [Test]
    public async Task Refresh_renews_the_token_caches_media_and_revalidates_the_site()
    {
        await using var api = new ApiFactory();
        using var admin = api.CreateAdminClient();
        (await admin.PutAsJsonAsync("/admin/instagram/token", new { token = "seed-token" })).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await admin.PostAsync("/admin/instagram/refresh", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await admin.PostAsync("/admin/instagram/refresh", null)).EnsureSuccessStatusCode();

        api.Instagram.RefreshedTokens.ShouldBe(["seed-token", "seed-token-refreshed"]);
        api.Revalidator.Calls.ShouldBe(2);
        using var reader = api.CreateClient();
        var media = await reader.GetFromJsonAsync<List<MediaResponse>>("/instagram/media");
        media.ShouldNotBeNull().Select(m => m.Id).ShouldBe(["2", "1"]); // newest first
        media[0].ImageUrl.ShouldBe("https://cdn.test/2.jpg"); // a video shows its thumbnail
        media[1].ImageUrl.ShouldBe("https://cdn.test/1.jpg");
    }

    [Test]
    public async Task Media_is_served_from_the_cache_without_calling_instagram()
    {
        await using var api = new ApiFactory();
        using var admin = api.CreateAdminClient();
        await admin.PutAsJsonAsync("/admin/instagram/token", new { token = "seed" });
        await admin.PostAsync("/admin/instagram/refresh", null);
        api.Instagram.Media = [];

        using var reader = api.CreateClient();
        (await reader.GetFromJsonAsync<List<MediaResponse>>("/instagram/media")).ShouldNotBeNull().Count.ShouldBe(2);
    }

    [Test]
    public async Task Refresh_without_a_stored_token_fails_clearly()
    {
        await using var api = new ApiFactory();
        using var admin = api.CreateAdminClient();
        using (var worker = Testing.WorkerClient())
        {
            await worker.PostAsJsonAsync("/d1/query", new { sql = "DELETE FROM meta WHERE key LIKE 'instagram_%'" });
        }

        var response = await admin.PostAsync("/admin/instagram/refresh", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).ShouldContain("No Instagram token");
    }

    [Test]
    public async Task Instagram_admin_endpoints_need_the_admin_token()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();

        (await client.PostAsync("/admin/instagram/refresh", null)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync("/admin/instagram/token", new { token = "x" })).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
