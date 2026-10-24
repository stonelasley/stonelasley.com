using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using StoneLasley.Application.Common.Interfaces;

namespace StoneLasley.Api.FunctionalTests;

/// <summary>
/// Hosts the API in-process, pointed at a Worker with the given secret.
/// </summary>
public sealed class ApiFactory(Uri workerUrl, string workerSecret) : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _settings = [];

    public const string IngestToken = "functional-test-ingest-token";

    public ApiFactory()
        : this(Testing.Worker.BaseUrl, WranglerDev.Secret)
    {
    }

    /// <summary>Stands in for the site's revalidate webhook (an external port).</summary>
    public FakeSiteRevalidator Revalidator { get; } = new();

    public ApiFactory WithSetting(string key, string? value)
    {
        _settings[key] = value;
        return this;
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
        foreach (var (key, value) in _settings)
        {
            builder.UseSetting(key, value);
        }
        builder.ConfigureTestServices(services => services.AddSingleton<ISiteRevalidator>(Revalidator));
    }
}
