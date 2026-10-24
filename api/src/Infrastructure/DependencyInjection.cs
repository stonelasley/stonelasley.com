using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StoneLasley.Application.Common.Interfaces;
using StoneLasley.Application.Search;
using StoneLasley.Infrastructure.Fakes;
using StoneLasley.Infrastructure.Site;
using StoneLasley.Infrastructure.Worker;

namespace StoneLasley.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WorkerOptions>()
            .BindConfiguration(WorkerOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<ID1Database, WorkerD1Database>((provider, http) =>
        {
            var options = provider.GetRequiredService<IOptions<WorkerOptions>>().Value;
            http.BaseAddress = options.BaseUrl;
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Secret);
            http.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddOptions<ImageOptions>()
            .BindConfiguration(ImageOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IImageStore, WorkerImageStore>((provider, http) =>
        {
            var options = provider.GetRequiredService<IOptions<WorkerOptions>>().Value;
            http.BaseAddress = options.BaseUrl;
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Secret);
            http.Timeout = TimeSpan.FromSeconds(60);
        });

        if (configuration.GetValue<bool>($"{SearchOptions.SectionName}:Vector:Fake"))
        {
            services.AddSingleton<IEmbedder, FakeEmbedder>();
            services.AddSingleton<IVectorIndex, InMemoryVectorIndex>();
        }
        else
        {
            services.AddHttpClient<IEmbedder, WorkerEmbedder>(WorkerClient);
            services.AddHttpClient<IVectorIndex, WorkerVectorIndex>(WorkerClient);
        }

        services.AddHealthChecks().AddCheck<D1HealthCheck>("d1");

        services.AddOptions<RevalidationOptions>().BindConfiguration(RevalidationOptions.SectionName);
        services.AddHttpClient<ISiteRevalidator, WebhookSiteRevalidator>(http => http.Timeout = TimeSpan.FromSeconds(10));

        return services;
    }

    private static void WorkerClient(IServiceProvider provider, HttpClient http)
    {
        var options = provider.GetRequiredService<IOptions<WorkerOptions>>().Value;
        http.BaseAddress = options.BaseUrl;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Secret);
        http.Timeout = TimeSpan.FromSeconds(60);
    }
}
