using StoneLasley.Application.Ingest;

namespace StoneLasley.Api.Ingest;

public static class IngestEndpoints
{
    public sealed record ManifestRequest(Dictionary<string, string> Posts);

    public sealed record UploadRequest(string Markdown);

    public static IEndpointRouteBuilder MapIngest(this IEndpointRouteBuilder app)
    {
        var ingest = app.MapGroup("/ingest").AddEndpointFilter<RequireIngestToken>();

        ingest.MapPost("/manifest", async (ManifestRequest request, IngestService service, CancellationToken cancellationToken, bool dryRun = false, bool includeDrafts = false) =>
            Results.Ok(await service.StartAsync(request.Posts, dryRun, includeDrafts, cancellationToken)));

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
