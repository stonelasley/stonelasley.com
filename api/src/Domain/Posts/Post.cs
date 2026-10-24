namespace StoneLasley.Domain.Posts;

/// <summary>
/// A single piece of content in one Content Area. Identified by its ULID, so the slug can change.
/// A Post in the Recipes area also carries its <see cref="Recipe"/> typed core, and one in Watches its <see cref="Watch"/>.
/// </summary>
public sealed record Post(
    string Id,
    string Title,
    string Slug,
    ContentArea Area,
    DateOnly Date,
    IReadOnlyList<string> Tags,
    Audience Audience,
    PostStatus Status,
    string Body,
    Recipe? Recipe = null,
    Watch? Watch = null);
