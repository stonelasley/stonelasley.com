using StoneLasley.Application.Posts;
using StoneLasley.Application.Search;
using StoneLasley.Domain.Posts;

namespace StoneLasley.Api.Posts;

public static class PostEndpoints
{
    public static IEndpointRouteBuilder MapPosts(this IEndpointRouteBuilder app)
    {
        app.MapGet("/areas/{area}", async (string area, PostReader reader, CancellationToken cancellationToken) =>
            ContentAreas.TryParse(area, out var contentArea)
                ? Results.Ok(await reader.AreaAsync(contentArea, cancellationToken))
                : Results.NotFound());

        app.MapGet("/posts/latest", async (string? area, int? limit, PostReader reader, CancellationToken cancellationToken) =>
        {
            if (limit is < 1 or > 50)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["limit"] = ["'limit' must be 1 to 50."] });
            }
            ContentArea? contentArea = null;
            if (!string.IsNullOrEmpty(area))
            {
                if (!ContentAreas.TryParse(area, out var parsed))
                {
                    return Results.NotFound();
                }
                contentArea = parsed;
            }
            return Results.Ok(await reader.LatestAsync(contentArea, limit ?? 5, cancellationToken));
        });

        // mode=keyword (default) or vector; hybrid fusion arrives with #42.
        app.MapGet("/search", async (string? q, string? area, string? mode, KeywordSearch keyword, VectorSearch vector, CancellationToken cancellationToken) =>
        {
            ContentArea? contentArea = null;
            if (!string.IsNullOrEmpty(area))
            {
                if (!ContentAreas.TryParse(area, out var parsed))
                {
                    return Results.NotFound();
                }
                contentArea = parsed;
            }
            return mode switch
            {
                null or "" or "keyword" => Results.Ok(await keyword.SearchAsync(q ?? "", contentArea, cancellationToken)),
                "vector" => Results.Ok(await vector.SearchAsync(q ?? "", contentArea, cancellationToken)),
                _ => Results.ValidationProblem(new Dictionary<string, string[]> { ["mode"] = ["'mode' must be keyword or vector."] }),
            };
        });

        app.MapGet("/areas/watches/facets", async (PostReader reader, CancellationToken cancellationToken) =>
            Results.Ok(await reader.WatchFacetsAsync(cancellationToken)));

        var posts = app.MapGroup("/areas/{area}/posts");

        posts.MapGet("/", async (string area, PostReader reader, CancellationToken cancellationToken) =>
            ContentAreas.TryParse(area, out var contentArea)
                ? Results.Ok(await reader.ListAsync(contentArea, cancellationToken))
                : Results.NotFound());

        posts.MapGet("/{slug}", async (string area, string slug, PostReader reader, CancellationToken cancellationToken) =>
            ContentAreas.TryParse(area, out var contentArea) && await reader.FindAsync(contentArea, slug, cancellationToken) is { } post
                ? Results.Ok(post)
                : Results.NotFound());

        return app;
    }
}
