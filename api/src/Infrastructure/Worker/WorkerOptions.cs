using System.ComponentModel.DataAnnotations;

namespace StoneLasley.Infrastructure.Worker;

/// <summary>
/// Where the Worker data proxy lives and the bearer secret it trusts (ADR 0003).
/// </summary>
public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    [Required]
    public Uri? BaseUrl { get; set; }

    [Required]
    public string Secret { get; set; } = string.Empty;
}
