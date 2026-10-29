using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

/// <summary>Helpers for tests against the real ci Vectorize index, whose writes are eventually consistent.</summary>
internal static class Vectors
{
    // Vectorize writes can take a couple of minutes to become queryable.
    private static readonly TimeSpan Visible = TimeSpan.FromMinutes(6);

    public const string Deadlift = """
        ## Setup

        Stand with the bar over your midfoot. Hinge at the hips, grip just outside your knees, and brace your trunk before the bar leaves the floor.

        ## Lockout

        Drive the floor away and finish with the hips through, not by leaning back.
        """;

    public const string DiveWatch = """
        A steel dive watch rated to 300 metres, with a unidirectional rotating bezel and an aluminium insert.

        ## Movement

        An automatic movement with a date window under a cyclops lens.
        """;

    public static Task<SearchResponse?> SearchAsync(HttpClient client, string query, string? area = null) =>
        client.GetFromJsonAsync<SearchResponse>($"/search?mode=vector&q={Uri.EscapeDataString(query)}" + (area is null ? "" : $"&area={area}"));

    /// <summary>
    /// Reads until <paramref name="done"/> or the deadline. Vectorize sometimes answers a read with a 500, so a failed
    /// read is tried again like an unfinished one, and only the last one's failure is thrown.
    /// </summary>
    public static async Task<T> EventuallyAsync<T>(Func<Task<T>> read, Func<T, bool> done)
    {
        var deadline = DateTime.UtcNow + Visible;
        while (true)
        {
            try
            {
                var value = await read();
                if (done(value) || DateTime.UtcNow >= deadline)
                {
                    return value;
                }
            }
            catch (HttpRequestException) when (DateTime.UtcNow < deadline)
            {
            }
            await Task.Delay(2000);
        }
    }

    /// <summary>Which of a Post's first <paramref name="n"/> chunk ids the index holds.</summary>
    public static async Task<List<string>> StoredIdsAsync(string postId, int n)
    {
        using var worker = Testing.WorkerClient();
        var response = await worker.PostAsJsonAsync("/vectors/get", new { ids = Enumerable.Range(0, n).Select(i => $"{postId}:{i}") });
        response.EnsureSuccessStatusCode();
        var vectors = await response.Content.ReadFromJsonAsync<List<StoredVector>>();
        return [.. vectors!.Select(v => v.Id).Order(StringComparer.Ordinal)];
    }

    private sealed record StoredVector(string Id);
}

/// <summary>
/// The vector leg against the real ci index (one namespace per run). Waiting on Vectorize is slow, so the
/// read-only tests share one corpus, ingested and waited on once.
/// </summary>
public class VectorSearchTests
{
    private static readonly string Training = TestPosts.NewUlid();
    private static readonly string Watches = TestPosts.NewUlid();
    private ApiFactory? _api;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task IngestCorpusAsync()
    {
        Assume.That(WranglerDev.FakeVectors, Is.False, "Semantic matching needs the real embedding model.");
        _api = new ApiFactory().WithRealVectors();
        _client = _api.CreateIngestClient();
        await Ingest.RunAsync(
            _client,
            TestPosts.Markdown(Training, TestPosts.NewSlug(), title: "Deadlift technique", area: "training", body: Vectors.Deadlift),
            TestPosts.Markdown(Watches, TestPosts.NewSlug("watch"), title: "Submariner Date", area: "watches", body: Vectors.DiveWatch, extra: TestPosts.WatchCore()));
        (await Vectors.EventuallyAsync(() => Vectors.SearchAsync(_client, "deadlift setup hinge at the hips"), s => s!.Results.Any(r => r.Id == Training)))!
            .Results.ShouldContain(r => r.Id == Training, "the corpus never became queryable");
        (await Vectors.EventuallyAsync(() => Vectors.SearchAsync(_client, "dive watch aluminium bezel"), s => s!.Results.Any(r => r.Id == Watches)))!
            .Results.ShouldContain(r => r.Id == Watches, "the corpus never became queryable");
    }

    [OneTimeTearDown]
    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_api is not null)
        {
            await _api.DisposeAsync();
        }
    }

    [Test]
    public async Task A_paraphrased_query_finds_the_post_by_its_best_chunk()
    {
        // Just after a write, Vectorize replicas can disagree, so even reads after setup poll.
        var search = await Vectors.EventuallyAsync(
            () => Vectors.SearchAsync(_client, "how do I pick a heavy barbell up off the ground safely"), s => s!.Results.Count == 1);

        var hit = search.ShouldNotBeNull().Results.ShouldHaveSingleItem();
        hit.Id.ShouldBe(Training);
        string.Concat(hit.Excerpt.Select(p => p.Text)).ShouldStartWith("Stand with the bar over your midfoot.");
        search.Counts.ShouldBe(new Dictionary<string, int> { ["training"] = 1 });
    }

    [Test]
    public async Task An_off_topic_query_falls_below_the_cosine_floor_and_returns_nothing()
    {
        var search = await Vectors.SearchAsync(_client, "lattice gauge theory in quantum chromodynamics");

        search.ShouldNotBeNull().Results.ShouldBeEmpty();
    }

    [Test]
    public async Task The_content_area_filter_applies_to_the_vector_leg()
    {
        var everywhere = await Vectors.EventuallyAsync(
            () => Vectors.SearchAsync(_client, "a dive watch with a rotating bezel"), s => s!.Results.Any(r => r.Id == Watches));
        var trainingOnly = await Vectors.EventuallyAsync(
            () => Vectors.SearchAsync(_client, "a dive watch with a rotating bezel", area: "training"), s => s!.Counts.SequenceEqual(everywhere!.Counts));

        everywhere.ShouldNotBeNull().Results.ShouldContain(r => r.Id == Watches);
        trainingOnly.ShouldNotBeNull().Results.ShouldNotContain(r => r.Id == Watches);
        trainingOnly.Counts.ShouldBe(everywhere.Counts);
    }
}

/// <summary>Updates and deletes against the real index; separate because it rewrites the corpus.</summary>
public class VectorIndexMaintenanceTests
{
    [Test]
    public async Task Updating_a_post_replaces_its_vectors_and_deleting_it_removes_them()
    {
        Assume.That(WranglerDev.FakeVectors, Is.False, "The fake index lives inside the API process.");
        await using var api = new ApiFactory().WithRealVectors();
        using var client = api.CreateIngestClient();
        var (id, slug) = (TestPosts.NewUlid(), TestPosts.NewSlug());
        await Ingest.RunAsync(client, TestPosts.Markdown(id, slug, area: "training", body: Vectors.Deadlift)); // 2 chunks
        await Vectors.EventuallyAsync(() => Vectors.StoredIdsAsync(id, 3), ids => ids.Count == 2);

        await Ingest.RunAsync(client, TestPosts.Markdown(id, slug, area: "training", body: "One short paragraph now."));
        var afterUpdate = await Vectors.EventuallyAsync(() => Vectors.StoredIdsAsync(id, 3), ids => ids.SequenceEqual([$"{id}:0"]));

        await Ingest.RunAsync(client, TestPosts.Markdown(TestPosts.NewUlid(), TestPosts.NewSlug(), body: "Something else."));
        var afterDelete = await Vectors.EventuallyAsync(() => Vectors.StoredIdsAsync(id, 3), ids => ids.Count == 0);

        afterUpdate.ShouldBe([$"{id}:0"]);
        afterDelete.ShouldBeEmpty();
    }
}
