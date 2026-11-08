namespace StoneLasley.Domain.Posts;

/// <summary>A Make Post's address, with the make it is about (as the makes file names it).</summary>
public sealed record MakePostAddress(PostAddress Address, string Make);

public static class MakePostConflicts
{
    /// <summary>One error for each make with more than one Make Post. Makes are stored under their name, so they compare exactly.</summary>
    public static IReadOnlyList<string> Find(IEnumerable<MakePostAddress> posts) =>
    [
        .. posts
            .GroupBy(post => post.Make, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var slugs = group.Select(post => post.Address.Slug).Order(StringComparer.Ordinal).ToList();
                var both = slugs.Count == 2 ? "both" : "all";
                return $"Posts {string.Join(", ", slugs[..^1])} and {slugs[^1]} are {both} Make Posts for {group.Key}.";
            }),
    ];
}
