using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using StoneLasley.Application.Access;
using StoneLasley.Application.Common.Interfaces;
using StoneLasley.Application.Ingest;
using StoneLasley.Application.Instagram;
using StoneLasley.Application.Posts;
using StoneLasley.Application.Search;

namespace StoneLasley.Api.Access;

public static class AccessEndpoints
{
    public const string ExchangePolicy = "access-exchange";
    public const string SessionHeader = "X-Access-Session";

    public sealed record ExchangeRequest(string? Code, string? TurnstileToken);

    public sealed record InstagramTokenRequest(string Token);

    // One message for every failure, so a wrong, revoked or unknown code looks the same.
    private static readonly object Refused = new { error = "That code didn't work." };

    /// <summary>
    /// The caller's IP as our own ingress saw it: the right-most X-Forwarded-For hop (Container Apps appends it,
    /// so a caller can't choose it), else the connection. Left-most entries are whatever the caller sent.
    /// </summary>
    public static string ClientIp(HttpContext context) =>
        context.Request.Headers["X-Forwarded-For"].ToString().Split(',')[^1].Trim() is { Length: > 0 } forwarded
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

        app.MapGet("/instagram/media", async (InstagramFeed feed, CancellationToken cancellationToken) =>
            Results.Ok(await feed.MediaAsync(cancellationToken)));

        app.MapGet("/access/me", async (HttpRequest request, AccessService access, CancellationToken cancellationToken) =>
            await access.ResolveAsync(request.Headers[SessionHeader].ToString(), cancellationToken) is { } session
                ? Results.Ok(new { session.ClientName })
                : Results.Unauthorized());

        // "Your content": everything the unlocked Client can read beyond the public site.
        app.MapGet("/access/posts", async (HttpRequest request, AccessService access, PostReader reader, CancellationToken cancellationToken) =>
            await access.ResolveAsync(request.Headers[SessionHeader].ToString(), cancellationToken) is { } session
                ? Results.Ok(new { session.ClientName, Posts = await reader.PrivateForClientAsync(session.ClientId, cancellationToken) })
                : Results.Unauthorized());

        var admin = app.MapGroup("/admin").AddEndpointFilter(RequireBearerToken.For<AdminOptions>(options => options.Token));

        admin.MapPost("/clients/{clientId}/codes", async (string clientId, AccessService access, CancellationToken cancellationToken) =>
            await access.IssueAsync(clientId, cancellationToken) is { } issued ? Results.Ok(issued) : Results.NotFound());

        admin.MapPut("/instagram/token", async (InstagramTokenRequest request, InstagramFeed feed, CancellationToken cancellationToken) =>
        {
            await feed.SetTokenAsync(request.Token, cancellationToken);
            return Results.NoContent();
        });

        // Called by the instagram-refresh GitHub Actions cron.
        admin.MapPost("/instagram/refresh", async (InstagramFeed feed, CancellationToken cancellationToken) =>
            await feed.RefreshAsync(cancellationToken)
                ? Results.NoContent()
                : Results.Conflict(new { error = "No Instagram token is stored; PUT one to /admin/instagram/token first." }));

        // Re-indexing (#44): build the new version's index alongside, then promote once the API runs on it.
        admin.MapPost("/vectors/rebuild", async (string version, VectorIndexer indexer, IOptions<SearchOptions> options, CancellationToken cancellationToken) =>
        {
            if (version == options.Value.Vector.IndexVersion)
            {
                return Results.Conflict(new { error = $"{version} is the active index version; rebuild into a new one." });
            }
            var (posts, vectors) = await indexer.RebuildAsync(version, cancellationToken);
            return Results.Ok(new { version, posts, vectors });
        });

        admin.MapPost("/vectors/promote", async (string version, VectorIndexer indexer, IOptions<SearchOptions> options, CancellationToken cancellationToken) =>
        {
            if (version != options.Value.Vector.IndexVersion)
            {
                return Results.Conflict(new { error = $"The API runs index version {options.Value.Vector.IndexVersion}; deploy it on {version} first." });
            }
            return await indexer.PromoteAsync(version, cancellationToken) ? Results.NoContent() : Results.NotFound();
        });

        admin.MapDelete("/clients/{clientId}/codes", async (string clientId, AccessService access, CancellationToken cancellationToken) =>
        {
            await access.RevokeAsync(clientId, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }
}
