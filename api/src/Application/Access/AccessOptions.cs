using System.ComponentModel.DataAnnotations;

namespace StoneLasley.Application.Access;

public sealed class AccessOptions
{
    public const string SectionName = "Access";

    /// <summary>Signs session values handed to the site. Rotating it signs every Client out.</summary>
    [Required]
    [MinLength(32)]
    public string SessionKey { get; set; } = string.Empty;
}
