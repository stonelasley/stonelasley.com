using StoneLasley.Application.Posts;
using StoneLasley.Domain.Posts;

namespace StoneLasley.Api.Posts;

public static class PostEndpoints
{
    public static IEndpointRouteBuilder MapPosts(this IEndpointRouteBuilder app)
    {
        var area = app.MapGroup("/areas/{area}/posts");

        area.MapGet("/", async (string area, PostReader reader, CancellationToken cancellationToken) =>
            ContentAreas.TryParse(area, out var contentArea)
                ? Results.Ok(await reader.ListAsync(contentArea, cancellationToken))
                : Results.NotFound());

        area.MapGet("/{slug}", async (string area, string slug, PostReader reader, CancellationToken cancellationToken) =>
            ContentAreas.TryParse(area, out var contentArea) && await reader.FindAsync(contentArea, slug, cancellationToken) is { } post
                ? Results.Ok(post)
                : Results.NotFound());

        return app;
    }
}
