using System.ComponentModel.DataAnnotations;

namespace StoneLasley.Infrastructure.Worker;

/// <summary>
/// The public origin Post images are served from, such as https://img.stonelasley.com (an R2 custom domain).
/// </summary>
public sealed class ImageOptions
{
    public const string SectionName = "Images";

    [Required]
    public Uri? BaseUrl { get; set; }
}
