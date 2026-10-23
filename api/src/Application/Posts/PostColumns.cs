using StoneLasley.Domain.Posts;

namespace StoneLasley.Application.Posts;

/// <summary>
/// How domain values are written to D1 columns.
/// </summary>
internal static class PostColumns
{
    public const string PublicAudience = "public";
    public const string PublishedStatus = "published";

    public static string Audience(Audience audience) => audience switch
    {
        Domain.Posts.Audience.Public => PublicAudience,
        _ => throw new ArgumentOutOfRangeException(nameof(audience), audience, null),
    };

    public static string Status(PostStatus status) => status switch
    {
        PostStatus.Published => PublishedStatus,
        PostStatus.Draft => "draft",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
