using System.Threading.RateLimiting;
using StoneLasley.Application.Access;
using StoneLasley.Application.Common.Interfaces;

namespace StoneLasley.Api.Access;

public static class AccessEndpoints
{
    public const string ExchangePolicy = "access-exchange";
    public const string SessionHeader = "X-Access-Session";

    public sealed record ExchangeRequest(string? Code, string? TurnstileToken);

    // One message for every failure, so a wrong, revoked or unknown code looks the same.
    private static readonly object Refused = new { error = "That code didn't work." };

    /// <summary>
    /// The caller's IP: the left-most X-Forwarded-For (set by the site for its visitor, or by the Container Apps
    /// ingress), else the connection. It only partitions the rate limit; Turnstile is the real gate.
    /// </summary>
    public static string ClientIp(HttpContext context) =>
        context.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim() is { Length: > 0 } forwarded
            ? forwarded
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    public static IServiceCollection AddAccessRateLimit(this IServiceCollection services) =>
        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.AddPolicy(ExchangePolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                ClientIp(context),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });

    public static IEndpointRouteBuilder MapAccess(this IEndpointRouteBuilder app)
    {
        app.MapPost("/access/exchange", async (ExchangeRequest request, HttpContext context, ITurnstile turnstile, AccessService access, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Code)
                || string.IsNullOrWhiteSpace(request.TurnstileToken)
                || !await turnstile.VerifyAsync(request.TurnstileToken, ClientIp(context), cancellationToken)
                || await access.ExchangeAsync(request.Code, cancellationToken) is not { } exchanged)
            {
                return Results.Json(Refused, statusCode: StatusCodes.Status401Unauthorized);
            }
            return Results.Ok(exchanged);
        }).RequireRateLimiting(ExchangePolicy);

        var admin = app.MapGroup("/admin").AddEndpointFilter(RequireBearerToken.For<AdminOptions>(options => options.Token));

        admin.MapPost("/clients/{clientId}/codes", async (string clientId, AccessService access, CancellationToken cancellationToken) =>
            await access.IssueAsync(clientId, cancellationToken) is { } issued ? Results.Ok(issued) : Results.NotFound());

        admin.MapDelete("/clients/{clientId}/codes", async (string clientId, AccessService access, CancellationToken cancellationToken) =>
        {
            await access.RevokeAsync(clientId, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }
}
