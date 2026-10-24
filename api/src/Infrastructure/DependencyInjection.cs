using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StoneLasley.Application.Common.Interfaces;
using StoneLasley.Infrastructure.Site;
using StoneLasley.Infrastructure.Worker;

namespace StoneLasley.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
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

        services.AddHealthChecks().AddCheck<D1HealthCheck>("d1");

        services.AddOptions<RevalidationOptions>().BindConfiguration(RevalidationOptions.SectionName);
        services.AddHttpClient<ISiteRevalidator, WebhookSiteRevalidator>(http => http.Timeout = TimeSpan.FromSeconds(30));

        return services;
    }
}
