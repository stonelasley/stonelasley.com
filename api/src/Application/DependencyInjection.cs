using Microsoft.Extensions.DependencyInjection;
using StoneLasley.Application.Ingest;
using StoneLasley.Application.Posts;

namespace StoneLasley.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IngestService>();
        services.AddScoped<PostReader>();
        return services;
    }
}
