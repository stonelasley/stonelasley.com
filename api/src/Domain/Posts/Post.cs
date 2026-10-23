namespace StoneLasley.Domain.Posts;

/// <summary>
/// A single piece of content in one Content Area. Identified by its ULID, so the slug can change.
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
    string Body);
