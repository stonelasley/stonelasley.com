using System.ComponentModel.DataAnnotations;

namespace StoneLasley.Api.Access;

/// <summary>The admin CLI's bearer token. Separate from the ingest token and the Worker secret.</summary>
public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    [Required]
    public string Token { get; set; } = string.Empty;
}
