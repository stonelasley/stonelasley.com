using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

[SetUpFixture]
public class Testing
{
    private static WranglerDev? _worker;

    public static WranglerDev Worker => _worker ?? throw new InvalidOperationException("wrangler dev is not running.");

    /// <summary>This run's slice of the shared ci Vectorize index.</summary>
    public static string VectorNamespace { get; } = $"ci-{Guid.NewGuid():N}";

    [OneTimeSetUp]
    public async Task StartWorker() => _worker = await WranglerDev.StartAsync();

    [OneTimeTearDown]
    public async Task StopWorker()
    {
        if (_worker is not null)
        {
            await DeleteRunVectorsAsync();
            await _worker.DisposeAsync();
        }
    }

    // Vectorize has no delete-by-namespace, so remove every vector the run's Posts still have, by id.
    private static async Task DeleteRunVectorsAsync()
    {
        if (WranglerDev.FakeVectors)
        {
            return;
        }
        using var worker = WorkerClient();
        var response = await worker.PostAsJsonAsync("/d1/query", new { sql = "SELECT id, vector_chunks FROM posts WHERE vector_chunks > 0" });
        var rows = (await response.Content.ReadFromJsonAsync<D1Rows>())!.Results;
        var ids = rows.SelectMany(row => Enumerable.Range(0, row.Vector_chunks).Select(i => $"{row.Id}:{i}")).ToList();
        foreach (var batch in ids.Chunk(1000))
        {
            await worker.PostAsJsonAsync("/vectors/delete", new { ids = batch });
        }
    }

    public static HttpClient WorkerClient()
    {
        var client = new HttpClient { BaseAddress = Worker.BaseUrl };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", WranglerDev.Secret);
        return client;
    }

    private sealed record D1Rows(IReadOnlyList<D1Row> Results);

    private sealed record D1Row(string Id, int Vector_chunks);
}
