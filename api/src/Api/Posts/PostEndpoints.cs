using StoneLasley.Api.Access;
using StoneLasley.Application.Access;
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

        // mode=hybrid|keyword|vector overrides Search:Mode, which v1 sets from the eval.
        app.MapGet("/search", async (string? q, string? area, string? mode, Searcher searcher, CancellationToken cancellationToken) =>
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
            SearchMode? searchMode = null;
            if (!string.IsNullOrEmpty(mode))
            {
                if (!Enum.TryParse<SearchMode>(mode, ignoreCase: true, out var parsedMode) || !Enum.IsDefined(parsedMode) || int.TryParse(mode, out _))
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["mode"] = ["'mode' must be hybrid, keyword or vector."] });
                }
                searchMode = parsedMode;
            }
            return Results.Ok(await searcher.SearchAsync(q ?? "", contentArea, searchMode, cancellationToken));
        });

        app.MapGet("/areas/watches/facets", async (PostReader reader, CancellationToken cancellationToken) =>
            Results.Ok(await reader.WatchFacetsAsync(cancellationToken)));

        var posts = app.MapGroup("/areas/{area}/posts");

        posts.MapGet("/", async (string area, PostReader reader, CancellationToken cancellationToken) =>
            ContentAreas.TryParse(area, out var contentArea)
                ? Results.Ok(await reader.ListAsync(contentArea, cancellationToken))
                : Results.NotFound());

        // A Private Post is served only with a live session (the site forwards it from the Client's cookie).
        posts.MapGet("/{slug}", async (string area, string slug, HttpRequest request, PostReader reader, AccessService access, CancellationToken cancellationToken) =>
        {
            if (!ContentAreas.TryParse(area, out var contentArea))
            {
                return Results.NotFound();
            }
            var session = await access.ResolveAsync(request.Headers[AccessEndpoints.SessionHeader].ToString(), cancellationToken);
            return await reader.FindAsync(contentArea, slug, session?.ClientId, cancellationToken) is { } post
                ? Results.Ok(post)
                : Results.NotFound();
        });

        return app;
    }
}
