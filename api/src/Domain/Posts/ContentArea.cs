namespace StoneLasley.Domain.Posts;

/// <summary>
/// A top-level subject the site publishes in. The set is closed.
/// </summary>
public enum ContentArea
{
    Recipes,
    Watches,
    AiDevelopment,
    Training,
}

public static class ContentAreas
{
    private static readonly Dictionary<ContentArea, string> Slugs = new()
    {
        [ContentArea.Recipes] = "recipes",
        [ContentArea.Watches] = "watches",
        [ContentArea.AiDevelopment] = "ai-development",
        [ContentArea.Training] = "training",
    };

    public static IReadOnlyCollection<ContentArea> All => Slugs.Keys;

    /// <summary>The area's URL segment, such as <c>ai-development</c>.</summary>
    public static string Slug(this ContentArea area) => Slugs[area];

    public static bool TryParse(string? slug, out ContentArea area)
    {
        foreach (var (candidate, candidateSlug) in Slugs)
        {
            if (candidateSlug == slug)
            {
                area = candidate;
                return true;
            }
        }
        area = default;
        return false;
    }
}
