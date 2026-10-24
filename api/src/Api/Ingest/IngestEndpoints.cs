using StoneLasley.Application.Ingest;
using StoneLasley.Domain.Posts;

namespace StoneLasley.Api.Ingest;

public static class IngestEndpoints
{
    /// <param name="Posts">ULID → content hash for every Post in the content repo.</param>
    /// <param name="Intros">Content Area slug → intro Markdown. When present it replaces every intro, so an area
    /// left out loses its intro; when absent, intros are left alone.</param>
    public sealed record ManifestRequest(Dictionary<string, string> Posts, Dictionary<string, string>? Intros);

    public sealed record UploadRequest(string Markdown);

    public static IEndpointRouteBuilder MapIngest(this IEndpointRouteBuilder app)
    {
        var ingest = app.MapGroup("/ingest").AddEndpointFilter<RequireIngestToken>();

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
            return Results.Ok(await service.StartAsync(request.Posts, intros, dryRun, includeDrafts, cancellationToken));
        });

        ingest.MapPut("/runs/{runId}/posts/{postId}", async (string runId, string postId, UploadRequest request, IngestService service, CancellationToken cancellationToken) =>
            ToResult(await service.UploadAsync(runId, postId, request.Markdown, cancellationToken)));

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
