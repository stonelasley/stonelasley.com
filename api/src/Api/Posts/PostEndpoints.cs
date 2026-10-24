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

        app.MapGet("/search", async (string? q, string? area, KeywordSearch search, CancellationToken cancellationToken) =>
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
            return Results.Ok(await search.SearchAsync(q ?? "", contentArea, cancellationToken));
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
