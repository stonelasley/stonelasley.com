using Microsoft.Extensions.Diagnostics.HealthChecks;
using StoneLasley.Application.Common.Interfaces;

namespace StoneLasley.Infrastructure.Worker;

/// <summary>
/// Healthy only when a query makes the full round trip: API → Worker → D1.
/// </summary>
internal sealed class D1HealthCheck(ID1Database database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await database.QueryAsync("SELECT 1", cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy("D1 is unreachable through the Worker.", exception);
        }
    }
}
