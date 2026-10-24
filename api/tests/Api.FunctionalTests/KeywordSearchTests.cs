using System.Net;
using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

public sealed record HighlightPart(string Text, bool Match);

public sealed record SearchHit(string Id, string Title, string Slug, string Area, string Date, IReadOnlyList<HighlightPart> Excerpt);

public sealed record SearchResponse(string Query, string? Area, IReadOnlyList<SearchHit> Results, IReadOnlyDictionary<string, int> Counts);

/// <summary>
/// Each ingest sends the full set, so every test ingests exactly the corpus it searches. Words are made unique
/// per test so nothing else can match.
/// </summary>
public class KeywordSearchTests
{
    private static string Word() => "zq" + Guid.NewGuid().ToString("N")[..10];

    private static async Task<SearchResponse> SearchAsync(HttpClient client, string query, string? area = null) =>
        (await client.GetFromJsonAsync<SearchResponse>(
            $"/search?q={Uri.EscapeDataString(query)}" + (area is null ? "" : $"&area={area}")))!;

    [Test]
    public async Task Finds_a_post_by_a_word_in_its_body_with_a_highlighted_excerpt()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var word = Word();
        var id = TestPosts.NewUlid();
        await Ingest.RunAsync(client, TestPosts.Markdown(id, TestPosts.NewSlug(), title: "Notes", area: "training", body: $"Squats build the {word} posterior chain."));

        var search = await SearchAsync(client, word);

        var hit = search.Results.ShouldHaveSingleItem();
        hit.Id.ShouldBe(id);
        hit.Area.ShouldBe("training");
        hit.Excerpt.ShouldContain(new HighlightPart(word, true));
        string.Concat(hit.Excerpt.Select(p => p.Text)).ShouldContain("posterior chain");
        search.Counts.ShouldBe(new Dictionary<string, int> { ["training"] = 1 });
    }

    [Test]
    public async Task An_exact_reference_puts_that_watch_first()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var reference = "16" + Random.Shared.Next(100000, 999999);
        var watch = TestPosts.NewUlid();
        await Ingest.RunAsync(
            client,
            TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug(), title: "Collecting notes", body: $"I once saw a {reference} in a shop window, and many others."),
            TestPosts.Markdown(watch, TestPosts.NewSlug("watch"), title: "Submariner Date", area: "watches", body: "A dive watch.", extra: TestPosts.WatchCore(reference: reference)));

        var search = await SearchAsync(client, reference);

        search.Results.First().Id.ShouldBe(watch);
    }

    [Test]
    public async Task Recipe_ingredients_are_searchable()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var ingredient = Word();
        var recipe = TestPosts.NewUlid();
        var core = TestPosts.RecipeCore.Replace("name: oats", $"name: {ingredient}");
        await Ingest.RunAsync(client, TestPosts.Markdown(recipe, TestPosts.NewSlug(), area: "recipes", body: "Mix.", extra: core));

        (await SearchAsync(client, ingredient)).Results.ShouldHaveSingleItem().Id.ShouldBe(recipe);
    }

    [Test]
    public async Task The_area_filter_narrows_results_but_counts_cover_every_area()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var word = Word();
        await Ingest.RunAsync(
            client,
            TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug(), area: "training", body: word),
            TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug(), area: "ai-development", body: word),
            TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug(), area: "ai-development", body: word));

        var search = await SearchAsync(client, word, area: "ai-development");

        search.Area.ShouldBe("ai-development");
        search.Results.Count.ShouldBe(2);
        search.Results.ShouldAllBe(r => r.Area == "ai-development");
        search.Counts.ShouldBe(new Dictionary<string, int> { ["ai-development"] = 2, ["training"] = 1 });
    }

    [Test]
    public async Task A_query_with_no_match_returns_nothing()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        await Ingest.RunAsync(client, TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug(), body: "Lifting and eating."));

        var search = await SearchAsync(client, Word());

        search.Results.ShouldBeEmpty();
        search.Counts.ShouldBeEmpty();
    }

    [TestCase("\"unbalanced")]
    [TestCase("NEAR(a b)")]
    [TestCase("title:x OR *")]
    [TestCase("^-()")]
    public async Task Fts5_syntax_in_a_query_is_never_an_error(string query)
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();

        var response = await client.GetAsync($"/search?q={Uri.EscapeDataString(query)}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Drafts_and_removed_posts_never_appear()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var word = Word();
        var removed = TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug(), body: word);
        await Ingest.RunAsync(client, dryRun: false, includeDrafts: true, removed, TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug(), status: "draft", body: word));
        (await SearchAsync(client, word)).Results.Count.ShouldBe(1);

        await Ingest.RunAsync(client, TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug(), body: "something else"));

        (await SearchAsync(client, word)).Results.ShouldBeEmpty();
    }

    [Test]
    public async Task A_changed_post_is_found_by_its_new_words_only()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var (before, after) = (Word(), Word());
        var (id, slug) = (TestPosts.NewUlid(), TestPosts.NewSlug());
        await Ingest.RunAsync(client, TestPosts.Markdown(id, slug, body: before));

        await Ingest.RunAsync(client, TestPosts.Markdown(id, slug, body: after));

        (await SearchAsync(client, before)).Results.ShouldBeEmpty();
        (await SearchAsync(client, after)).Results.ShouldHaveSingleItem().Id.ShouldBe(id);
    }

    [Test]
    public async Task An_unknown_area_is_not_found()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();

        (await client.GetAsync("/search?q=x&area=blog")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
