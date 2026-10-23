using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace StoneLasley.Api.FunctionalTests;

/// <summary>
/// Hosts the API in-process, pointed at a Worker with the given secret.
/// </summary>
public sealed class ApiFactory(Uri workerUrl, string workerSecret) : WebApplicationFactory<Program>
{
    public const string IngestToken = "functional-test-ingest-token";

    public ApiFactory()
        : this(Testing.Worker.BaseUrl, WranglerDev.Secret)
    {
    }

    /// <summary>A client that carries the ingest token.</summary>
    public HttpClient CreateIngestClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IngestToken);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Worker:BaseUrl", workerUrl.ToString());
        builder.UseSetting("Worker:Secret", workerSecret);
        builder.UseSetting("Ingest:Token", IngestToken);
    }
}
