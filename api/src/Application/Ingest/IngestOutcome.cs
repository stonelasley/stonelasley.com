namespace StoneLasley.Application.Ingest;

/// <summary>What a commit changed, or would change for a dry run.</summary>
public sealed record CommitSummary(bool DryRun, IReadOnlyList<string> Upserted, IReadOnlyList<string> Deleted);

/// <summary>
/// How an ingest step ended. Endpoints map each case to an HTTP status.
/// </summary>
public abstract record IngestOutcome
{
    private IngestOutcome()
    {
    }

    public sealed record Uploaded : IngestOutcome;

    public sealed record Committed(CommitSummary Summary) : IngestOutcome;

    public sealed record RunNotFound : IngestOutcome;

    public sealed record RunAlreadyCommitted : IngestOutcome;

    public sealed record Invalid(IReadOnlyList<string> Errors) : IngestOutcome;
}
