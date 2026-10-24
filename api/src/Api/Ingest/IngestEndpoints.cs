using StoneLasley.Application.Ingest;
using StoneLasley.Domain.Access;
using StoneLasley.Domain.Posts;

namespace StoneLasley.Api.Ingest;

public static class IngestEndpoints
{
    /// <param name="Posts">ULID → content hash for every Post in the content repo.</param>
    /// <param name="Intros">Content Area slug → intro Markdown. When present it replaces every intro, so an area
    /// left out loses its intro; when absent, intros are left alone.</param>
    /// <param name="Clients">The content repo's clients file (YAML). When present it replaces every Client.</param>
    public sealed record ManifestRequest(Dictionary<string, string> Posts, Dictionary<string, string>? Intros, string? Clients);

    /// <param name="Markdown">The Post file, frontmatter and body.</param>
    /// <param name="Images">Each image the Markdown references relatively, by that path, as base64.</param>
    public sealed record UploadRequest(string Markdown, IReadOnlyList<ImageUpload>? Images);

    public sealed record ImageUpload(string Path, string Content);

    public static IEndpointRouteBuilder MapIngest(this IEndpointRouteBuilder app)
    {
        var ingest = app.MapGroup("/ingest").AddEndpointFilter(RequireBearerToken.For<IngestOptions>(options => options.Token));

        ingest.MapPost("/manifest", async (ManifestRequest request, IngestService service, CancellationToken cancellationToken, bool dryRun = false, bool includeDrafts = false) =>
        {
            Dictionary<ContentArea, string>? intros = request.Intros is null ? null : [];
            foreach (var (slug, intro) in request.Intros ?? [])
            {
                if (!ContentAreas.TryParse(slug, out var area))
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["intros"] = [$"'{slug}' is not a Content Area."] });
                }
                intros![area] = intro;
            }
            IReadOnlyList<Client>? clients = null;
            if (request.Clients is not null)
            {
                var parsed = ClientsFile.Parse(request.Clients);
                if (parsed.Errors.Count > 0)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["clients"] = [.. parsed.Errors] });
                }
                clients = parsed.Clients;
            }
            return Results.Ok(await service.StartAsync(request.Posts, intros, clients, dryRun, includeDrafts, cancellationToken));
        });

        ingest.MapPut("/runs/{runId}/posts/{postId}", async (string runId, string postId, UploadRequest request, IngestService service, CancellationToken cancellationToken) =>
        {
            List<UploadedImage> images = [];
            foreach (var image in request.Images ?? [])
            {
                try
                {
                    images.Add(new UploadedImage(image.Path, Convert.FromBase64String(image.Content)));
                }
                catch (FormatException)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["images"] = [$"Image '{image.Path}' is not valid base64."] });
                }
            }
            return ToResult(await service.UploadAsync(runId, postId, request.Markdown, images, cancellationToken));
        });

        ingest.MapPost("/runs/{runId}/commit", async (string runId, IngestService service, CancellationToken cancellationToken) =>
            ToResult(await service.CommitAsync(runId, cancellationToken)));

        return app;
    }

    private static IResult ToResult(IngestOutcome outcome) => outcome switch
    {
        IngestOutcome.Uploaded => Results.NoContent(),
        IngestOutcome.Committed committed => Results.Ok(committed.Summary),
        IngestOutcome.RunNotFound => Results.NotFound(),
        IngestOutcome.RunAlreadyCommitted => Results.Conflict(),
        IngestOutcome.Invalid invalid => Results.ValidationProblem(new Dictionary<string, string[]> { ["post"] = [.. invalid.Errors] }),
        _ => throw new InvalidOperationException($"Unhandled ingest outcome {outcome}."),
    };
}
