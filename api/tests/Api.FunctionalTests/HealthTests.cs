using System.Net;

namespace StoneLasley.Api.FunctionalTests;

public class HealthTests
{
    [Test]
    public async Task Is_healthy_when_the_api_reaches_d1_through_the_worker()
    {
        await using var api = new ApiFactory(Testing.Worker.BaseUrl, WranglerDev.Secret);
        using var client = api.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Test]
    public async Task Is_unhealthy_when_the_worker_rejects_the_api_secret()
    {
        await using var api = new ApiFactory(Testing.Worker.BaseUrl, "not-the-worker-secret");
        using var client = api.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Unhealthy");
    }

    [Test]
    public async Task Is_unhealthy_when_the_worker_is_unreachable()
    {
        await using var api = new ApiFactory(new Uri("http://127.0.0.1:9"), WranglerDev.Secret);
        using var client = api.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Unhealthy");
    }
}
