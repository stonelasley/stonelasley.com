using System.Security.Cryptography;

namespace StoneLasley.Api.FunctionalTests;

/// <summary>
/// Builds fixture Posts. Every Post gets a fresh ULID and slug, so tests can share one D1 database.
/// </summary>
public static class TestPosts
{
    private const string Crockford = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static string NewUlid() =>
        "0" + new string([.. Enumerable.Range(0, 25).Select(_ => Crockford[RandomNumberGenerator.GetInt32(32)])]);

    public static string NewSlug(string prefix = "post") => $"{prefix}-{Guid.NewGuid():N}";

    public static string Markdown(
        string id,
        string slug,
        string title = "A fixture Post",
        string area = "ai-development",
        string date = "2026-10-01",
        string status = "published",
        string body = "## Section\n\nSome **bold** text.") => $"""
        ---
        id: {id}
        title: {title}
        slug: {slug}
        area: {area}
        date: {date}
        tags: [fixture, testing]
        audience: public
        status: {status}
        ---
        {body}
        """;

    public static string Hash(string markdown) =>
        Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(markdown)));
}
