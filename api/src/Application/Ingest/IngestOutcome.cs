namespace StoneLasley.Application.Ingest;

/// <summary>
/// How an ingest step ended. Endpoints map each case to an HTTP status.
/// </summary>
public abstract record IngestOutcome
{
    private IngestOutcome()
    {
    }

    public sealed record Done : IngestOutcome;

    public sealed record RunNotFound : IngestOutcome;

    public sealed record RunAlreadyCommitted : IngestOutcome;

    public sealed record Invalid(IReadOnlyList<string> Errors) : IngestOutcome;
}
