using System.Net.Http.Json;
using System.Security.Cryptography;

namespace StoneLasley.Api.FunctionalTests;

/// <summary>Search hits and listed Posts carry the image shown beside them: the Post's hero, or what a stand-in needs.</summary>
public class HeroTests
{
    private static string Word() => "zq" + Guid.NewGuid().ToString("N")[..10];

    private static byte[] NewPng() => [.. ImageTests.Png, .. RandomNumberGenerator.GetBytes(8)];

    private static async Task IngestAsync(HttpClient client, string id, string markdown, Dictionary<string, byte[]> images)
    {
        var manifest = await Ingest.ManifestAsync(client, Ingest.ById(markdown));
        (await Ingest.UploadAsync(client, manifest.RunId, id, markdown, images)).EnsureSuccessStatusCode();
        (await Ingest.CommitAsync(client, manifest.RunId)).EnsureSuccessStatusCode();
    }

    private static async Task<SearchHit> OnlyHitAsync(HttpClient client, string mode, string query) =>
        (await client.GetFromJsonAsync<SearchResponse>($"/search?mode={mode}&q={Uri.EscapeDataString(query)}"))!
            .Results.ShouldHaveSingleItem();

    [Test]
    public async Task The_hero_is_the_first_image_stored_with_the_post()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var (word, id) = (Word(), TestPosts.NewUlid());
        var (hero, second) = (NewPng(), NewPng());
        var body = $"Intro about {word}.\n\n![Elsewhere](https://example.com/far.png)\n\n![Plated](./plated.png)\n\n![Prep](./prep.png)";

        await IngestAsync(client, id, TestPosts.Markdown(id, TestPosts.NewSlug(), body: body), new() { ["plated.png"] = hero, ["prep.png"] = second });

        (await OnlyHitAsync(client, "keyword", word)).Hero.ShouldBe($"{ApiFactory.ImagesBaseUrl}/{ImageTests.KeyOf(hero)}");
    }

    [Test]
    public async Task A_post_with_no_image_of_its_own_has_no_hero()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var word = Word();

        await Ingest.RunAsync(client, TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug(), body: $"About {word}.\n\n![Elsewhere](https://example.com/far.png)"));

        (await OnlyHitAsync(client, "keyword", word)).Hero.ShouldBeNull();
    }

    [Test]
    public async Task A_watch_hit_carries_its_make_and_reference_and_others_do_not()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var word = Word();
        var watch = TestPosts.NewUlid();

        await Ingest.RunAsync(
            client,
            TestPosts.Markdown(watch, TestPosts.NewSlug("watch"), area: "watches", body: $"A {word} dive watch.", extra: TestPosts.WatchCore(make: "Patek Philippe", reference: "5167A")),
            TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug(), body: $"A {word} note."));

        var hits = (await client.GetFromJsonAsync<SearchResponse>($"/search?mode=keyword&q={word}"))!.Results;
        hits.Single(hit => hit.Id == watch).Watch.ShouldBe(new SearchWatch("Patek Philippe", "5167A"));
        hits.Single(hit => hit.Id != watch).Watch.ShouldBeNull();
    }

    [Test]
    public async Task The_vector_leg_carries_the_hero_and_watch_too()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var id = TestPosts.NewUlid();
        var hero = NewPng();
        var body = "![Dial](./dial.png)\n\nLacquered gilt dial, tropical patina.";

        await IngestAsync(
            client, id, TestPosts.Markdown(id, TestPosts.NewSlug("watch"), title: "Gilt dial", area: "watches", body: body, extra: TestPosts.WatchCore(reference: "6538")), new() { ["dial.png"] = hero });

        var hit = (await client.GetFromJsonAsync<SearchResponse>("/search?mode=vector&q=lacquered%20gilt%20dial%20tropical%20patina"))!
            .Results.First(r => r.Id == id);
        hit.Hero.ShouldBe($"{ApiFactory.ImagesBaseUrl}/{ImageTests.KeyOf(hero)}");
        hit.Watch.ShouldBe(new SearchWatch("Rolex", "6538"));
    }

    [Test]
    public async Task A_listed_post_carries_its_hero()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var (id, slug) = (TestPosts.NewUlid(), TestPosts.NewSlug());
        var hero = NewPng();

        await IngestAsync(client, id, TestPosts.Markdown(id, slug, area: "training", date: "2099-01-01", body: "![Bar](./bar.png)\n\nWords."), new() { ["bar.png"] = hero });

        var listed = await client.GetFromJsonAsync<List<ListedPost>>("/areas/training/posts");
        listed.ShouldNotBeNull().Single(post => post.Id == id).Hero.ShouldBe($"{ApiFactory.ImagesBaseUrl}/{ImageTests.KeyOf(hero)}");
    }

    private sealed record ListedPost(string Id, string? Hero);

    [Test]
    public async Task A_post_stored_before_heroes_existed_is_asked_for_again()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var id = TestPosts.NewUlid();
        var markdown = TestPosts.Markdown(id, TestPosts.NewSlug());
        await Ingest.RunAsync(client, markdown);
        using (var worker = Testing.WorkerClient())
        {
            (await worker.PostAsJsonAsync("/d1/query", new { sql = "UPDATE posts SET hero = NULL WHERE id = ?", @params = new[] { id } })).EnsureSuccessStatusCode();
        }

        var manifest = await Ingest.ManifestAsync(client, Ingest.ById(markdown));

        manifest.Requested.ShouldContain(id);
    }
}
