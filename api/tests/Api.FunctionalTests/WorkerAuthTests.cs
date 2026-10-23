using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

/// <summary>
/// The Worker trusts only the API, through the bearer secret (ADR 0003).
/// </summary>
public class WorkerAuthTests
{
    private static readonly object Query = new { sql = "SELECT 1" };

    [Test]
    public async Task Rejects_a_query_without_the_bearer_secret()
    {
        using var worker = new HttpClient { BaseAddress = Testing.Worker.BaseUrl };

        var response = await worker.PostAsJsonAsync("/d1/query", Query);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Rejects_a_query_with_the_wrong_bearer_secret()
    {
        using var worker = new HttpClient { BaseAddress = Testing.Worker.BaseUrl };
        worker.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-the-worker-secret");

        var response = await worker.PostAsJsonAsync("/d1/query", Query);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
