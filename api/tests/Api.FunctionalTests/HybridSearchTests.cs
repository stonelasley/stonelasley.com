using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StoneLasley.Application.Common.Interfaces;

namespace StoneLasley.Api.FunctionalTests;

/// <summary>
/// Hybrid search and leg selection on the in-process vector index, with a concept embedder so the two legs can
/// match different Posts. Runs everywhere, fork PRs included.
/// </summary>
public class HybridSearchTests
{
    private static ApiFactory Api() =>
        new ApiFactory().WithServices(services => services.AddSingleton<IEmbedder, ConceptEmbedder>());

    private static Task<SearchResponse?> SearchAsync(HttpClient client, string query, string? mode = null) =>
        client.GetFromJsonAsync<SearchResponse>($"/search?q={Uri.EscapeDataString(query)}" + (mode is null ? "" : $"&mode={mode}"));

    private static async Task<(string Keyword, string Vector)> IngestAsync(HttpClient client, string word)
    {
        var (keyword, vector) = (TestPosts.NewUlid(), TestPosts.NewUlid());
        // The keyword leg finds only the unique word; the vector leg finds the deadlift Post for "barbell".
        await Ingest.RunAsync(
            client,
            TestPosts.Markdown(keyword, TestPosts.NewSlug(), title: "Notes", body: $"A note that mentions {word} once."),
            TestPosts.Markdown(vector, TestPosts.NewSlug(), title: "Setup", body: "Hinge at the hips before the pull."));
        return (keyword, vector);
    }

    [Test]
    public async Task Hybrid_fuses_both_legs_with_the_keyword_snippet_where_the_keyword_leg_matched()
    {
        await using var api = Api();
        using var client = api.CreateIngestClient();
        var word = "zq" + Guid.NewGuid().ToString("N")[..8];
        var (keyword, vector) = await IngestAsync(client, word);

        var search = await SearchAsync(client, $"{word} barbell");

        search.ShouldNotBeNull().Results.Select(r => r.Id).ShouldBe([keyword, vector], ignoreOrder: true);
        search.Results.Single(r => r.Id == keyword).Excerpt.ShouldContain(new HighlightPart(word, true));
        search.Results.Single(r => r.Id == vector).Excerpt.ShouldAllBe(p => !p.Match);
        search.Counts.Values.Sum().ShouldBe(2);
    }

    [Test]
    public async Task Each_mode_answers_with_its_own_leg()
    {
        await using var api = Api();
        using var client = api.CreateIngestClient();
        var word = "zq" + Guid.NewGuid().ToString("N")[..8];
        var (keyword, vector) = await IngestAsync(client, word);
        var query = $"{word} barbell";

        (await SearchAsync(client, query, "keyword")).ShouldNotBeNull().Results.Select(r => r.Id).ShouldBe([keyword]);
        (await SearchAsync(client, query, "vector")).ShouldNotBeNull().Results.Select(r => r.Id).ShouldContain(vector);
        (await SearchAsync(client, query, "vector"))!.Results.ShouldNotContain(r => r.Id == keyword);
        (await SearchAsync(client, query, "hybrid")).ShouldNotBeNull().Results.Count.ShouldBe(2);
    }

    [Test]
    public async Task The_leg_selection_setting_switches_the_default_mode_without_code_changes()
    {
        await using var api = Api().WithSetting("Search:Mode", "Keyword");
        using var client = api.CreateIngestClient();
        var word = "zq" + Guid.NewGuid().ToString("N")[..8];
        var (keyword, _) = await IngestAsync(client, word);

        var search = await SearchAsync(client, $"{word} barbell");

        search.ShouldNotBeNull().Results.Select(r => r.Id).ShouldBe([keyword]);
    }

    [Test]
    public async Task An_off_topic_query_returns_nothing_in_hybrid_mode()
    {
        await using var api = Api();
        using var client = api.CreateIngestClient();
        await IngestAsync(client, "zq" + Guid.NewGuid().ToString("N")[..8]);

        var search = await SearchAsync(client, "xylophone zeppelin quasar");

        search.ShouldNotBeNull().Results.ShouldBeEmpty();
    }

    [Test]
    public async Task An_unknown_mode_is_rejected()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();

        (await client.GetAsync("/search?q=x&mode=semantic")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.GetAsync("/search?q=x&mode=1")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
