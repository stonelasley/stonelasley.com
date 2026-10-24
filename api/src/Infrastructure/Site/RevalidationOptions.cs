namespace StoneLasley.Infrastructure.Site;

/// <summary>
/// The site's revalidate webhook and the secret it checks. With no URL, revalidation is skipped (local dev).
/// </summary>
public sealed class RevalidationOptions
{
    public const string SectionName = "Revalidation";

    public Uri? Url { get; set; }

    public string Secret { get; set; } = string.Empty;

    /// <summary>Vercel's "Protection Bypass for Automation" secret, for previews behind Deployment Protection.</summary>
    public string? ProtectionBypass { get; set; }
}
