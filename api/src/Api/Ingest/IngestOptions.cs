using System.ComponentModel.DataAnnotations;

namespace StoneLasley.Api.Ingest;

/// <summary>
/// The bearer token the content repo's CI sends to ingest. Separate from the Worker secret and the admin token.
/// </summary>
public sealed class IngestOptions
{
    public const string SectionName = "Ingest";

    [Required]
    public string Token { get; set; } = string.Empty;
}
