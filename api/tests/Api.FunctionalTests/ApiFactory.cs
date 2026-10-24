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
    public const string ImagesBaseUrl = "https://img.test";
    public const string AdminToken = "functional-test-admin-token";

    public ApiFactory()
        : this(Testing.Worker.BaseUrl, WranglerDev.Secret)
    {
    }

    /// <summary>Stands in for Cloudflare Turnstile's siteverify (an external port).</summary>
    public FakeTurnstile Turnstile { get; } = new();

    /// <summary>Stands in for the Instagram API (an external port).</summary>
    public FakeInstagram Instagram { get; } = new();

    /// <summary>A client that carries the admin token.</summary>
    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken);
        return client;
    }

    /// <summary>Stands in for the site's revalidate webhook (an external port).</summary>
    public FakeSiteRevalidator Revalidator { get; } = new();

    private bool _realVectors;
    private Action<IServiceCollection>? _services;

    /// <summary>Replace services at external ports, such as the embedder.</summary>
    public ApiFactory WithServices(Action<IServiceCollection> configure)
    {
        _services = configure;
        return this;
    }

    /// <summary>Use Workers AI and the ci Vectorize index instead of the fake (unless SL_FAKE_VECTORS=1).</summary>
    public ApiFactory WithRealVectors()
    {
        _realVectors = true;
        return this;
    }

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
        builder.UseSetting("Images:BaseUrl", ImagesBaseUrl);
        builder.UseSetting("Admin:Token", AdminToken);
        builder.UseSetting("Access:SessionKey", "functional-test-session-key-0123456789abcdef");
        builder.UseSetting("Search:Vector:Namespace", Testing.VectorNamespace);
        // Only the vector tests pay for real embeddings; everything else uses the in-process fake.
        builder.UseSetting("Search:Vector:Fake", WranglerDev.FakeVectors || !_realVectors ? "true" : "false");
        foreach (var (key, value) in _settings)
        {
            builder.UseSetting(key, value);
        }
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ISiteRevalidator>(Revalidator);
            services.AddSingleton<ITurnstile>(Turnstile);
            services.AddSingleton<IInstagram>(Instagram);
            _services?.Invoke(services);
        });
    }
}
