using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StoneLasley.Application.Ingest;
using StoneLasley.Application.Posts;

namespace StoneLasley.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IngestService>();
        services.AddScoped<PostReader>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
