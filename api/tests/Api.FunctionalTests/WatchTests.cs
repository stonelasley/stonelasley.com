using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

public sealed record WatchResponse(
    string Make,
    string Reference,
    string ModelName,
    IReadOnlyList<string> Variants,
    int? ProductionStart,
    int? ProductionEnd,
    bool InProduction,
    decimal? CaseDiameterMm,
    string? Calibre,
    string? CaseMaterial,
    int? WaterResistanceM,
    int? WaterResistanceFt);

public sealed record WatchPostResponse(string Id, string Slug, WatchResponse? Watch);

public sealed record WatchSummaryResponse(string Make, string Reference, string ModelName, IReadOnlyList<int> Decades);

public sealed record WatchListItem(string Id, string Slug, WatchSummaryResponse? Watch);

public sealed record FacetsResponse(IReadOnlyList<string> Makes, IReadOnlyList<string> ModelNames, IReadOnlyList<int> Decades);

/// <summary>Each run sends the full set, so every test ingests exactly the Watches it reads.</summary>
public class WatchTests
{
    [Test]
    public async Task A_watch_is_read_with_its_typed_core_and_feet_calculated()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var slug = TestPosts.NewSlug("watch");
        await Ingest.RunAsync(client, TestPosts.Markdown(TestPosts.NewUlid(), slug, area: "watches", extra: TestPosts.WatchCore(make: "Patek Philippe")));

        var post = await client.GetFromJsonAsync<WatchPostResponse>($"/areas/watches/posts/{slug}");

        var watch = post.ShouldNotBeNull().Watch.ShouldNotBeNull();
        watch.Make.ShouldBe("Patek Philippe");
        watch.Reference.ShouldBe("16610");
        watch.ModelName.ShouldBe("Submariner Date");
        watch.Variants.ShouldBe(["16610LV"]);
        watch.ProductionStart.ShouldBe(1988);
        watch.ProductionEnd.ShouldBe(2010);
        watch.InProduction.ShouldBeFalse();
        watch.CaseDiameterMm.ShouldBe(40m);
        watch.Calibre.ShouldBe("3135");
        watch.CaseMaterial.ShouldBe("Oystersteel");
        watch.WaterResistanceM.ShouldBe(300);
        watch.WaterResistanceFt.ShouldBe(984);
    }

    [Test]
    public async Task The_watches_index_carries_what_its_filters_need()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        var id = TestPosts.NewUlid();
        await Ingest.RunAsync(client, TestPosts.Markdown(id, TestPosts.NewSlug("watch"), area: "watches", extra: TestPosts.WatchCore()));

        var posts = await client.GetFromJsonAsync<List<WatchListItem>>("/areas/watches/posts");

        var watch = posts.ShouldNotBeNull().Single(p => p.Id == id).Watch.ShouldNotBeNull();
        watch.Make.ShouldBe("Rolex");
        watch.ModelName.ShouldBe("Submariner Date");
        watch.Decades.ShouldBe([1980, 1990, 2000, 2010]);
    }

    [Test]
    public async Task Facets_list_each_make_model_name_and_decade_once()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateIngestClient();
        await Ingest.RunAsync(
            client,
            TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug("watch"), area: "watches", extra: TestPosts.WatchCore()),
            TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug("watch"), area: "watches", extra: TestPosts.WatchCore(reference: "116610LN", production: "productionStart: 2010")),
            TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug("watch"), area: "watches", extra: TestPosts.WatchCore(make: "Patek Philippe", reference: "5711", modelName: "Nautilus", production: "")),
            TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug("draft"), area: "watches", status: "draft", extra: TestPosts.WatchCore(modelName: "Explorer", production: "productionStart: 1950\nproductionEnd: 1955")));

        var facets = await client.GetFromJsonAsync<FacetsResponse>("/areas/watches/facets");

        facets.ShouldNotBeNull();
        facets.Makes.ShouldBe(["Patek Philippe", "Rolex"]);
        facets.ModelNames.ShouldBe(["Nautilus", "Submariner Date"]);
        facets.Decades.ShouldBe(Enumerable.Range(198, (DateTime.UtcNow.Year / 10) - 198 + 1).Select(d => d * 10));
    }
}
