namespace StoneLasley.Domain.Posts;

/// <summary>Where a Post lives: its URL is <c>/{area}/{slug}</c>.</summary>
public sealed record PostAddress(string Id, ContentArea Area, string Slug);

public static class SlugConflicts
{
    /// <summary>One error for each slug used by more than one Post in the same Content Area.</summary>
    public static IReadOnlyList<string> Find(IEnumerable<PostAddress> posts) =>
    [
        .. posts
            .GroupBy(post => (post.Area, post.Slug))
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key.Area).ThenBy(group => group.Key.Slug, StringComparer.Ordinal)
            .Select(group =>
            {
                var ids = group.Select(post => post.Id).Order(StringComparer.Ordinal).ToList();
                var both = ids.Count == 2 ? "both" : "all";
                return $"Posts {string.Join(", ", ids[..^1])} and {ids[^1]} {both} use slug '{group.Key.Slug}' in {group.Key.Area.Slug()}.";
            }),
    ];
}
