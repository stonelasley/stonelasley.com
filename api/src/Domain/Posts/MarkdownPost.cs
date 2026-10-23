using System.Globalization;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace StoneLasley.Domain.Posts;

public sealed record MarkdownPostResult(Post? Post, IReadOnlyList<string> Errors);

/// <summary>
/// Reads a Post from Markdown with YAML frontmatter, as authored in the content repo (ADR 0002).
/// </summary>
public static partial class MarkdownPost
{
    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static MarkdownPostResult Parse(string markdown)
    {
        var text = markdown.ReplaceLineEndings("\n");
        var match = FrontmatterPattern().Match(text);
        if (!match.Success)
        {
            return Failed("The Post must start with a frontmatter block between '---' lines.");
        }

        Frontmatter? frontmatter;
        try
        {
            frontmatter = Yaml.Deserialize<Frontmatter>(match.Groups["yaml"].Value);
        }
        catch (YamlException exception)
        {
            return Failed($"The frontmatter is not valid YAML: {exception.Message}");
        }
        frontmatter ??= new Frontmatter();

        var errors = new List<string>();

        var id = frontmatter.Id?.Trim().ToUpperInvariant();
        if (id is null || !UlidPattern().IsMatch(id))
        {
            errors.Add("'id' must be a ULID.");
        }

        var title = frontmatter.Title?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            errors.Add("'title' is required.");
        }

        var slug = frontmatter.Slug?.Trim();
        if (slug is null || !SlugPattern().IsMatch(slug))
        {
            errors.Add("'slug' must be lowercase letters, digits and single hyphens.");
        }

        if (!ContentAreas.TryParse(frontmatter.Area?.Trim(), out var area))
        {
            errors.Add($"'area' must be one of: {string.Join(", ", ContentAreas.All.Select(a => a.Slug()))}.");
        }

        if (!DateOnly.TryParseExact(frontmatter.Date?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            errors.Add("'date' must be a date in the form YYYY-MM-DD.");
        }

        var tags = (frontmatter.Tags ?? []).Select(t => t?.Trim() ?? string.Empty).ToList();
        if (tags.Any(string.IsNullOrEmpty))
        {
            errors.Add("'tags' must not contain empty values.");
        }

        if (frontmatter.Audience?.Trim() != "public")
        {
            errors.Add("'audience' must be 'public'.");
        }

        PostStatus status = default;
        switch (frontmatter.Status?.Trim())
        {
            case "published":
                status = PostStatus.Published;
                break;
            case "draft":
                status = PostStatus.Draft;
                break;
            default:
                errors.Add("'status' must be 'published' or 'draft'.");
                break;
        }

        if (errors.Count > 0)
        {
            return new MarkdownPostResult(null, errors);
        }

        var body = match.Groups["body"].Value.Trim('\n');
        var post = new Post(id!, title!, slug!, area, date, tags, Audience.Public, status, body);
        return new MarkdownPostResult(post, []);
    }

    private static MarkdownPostResult Failed(string error) => new(null, [error]);

    [GeneratedRegex(@"\A---\n(?<yaml>.*?)\n---(?:\n(?<body>.*))?\z", RegexOptions.Singleline)]
    private static partial Regex FrontmatterPattern();

    // Crockford base32, 26 characters; a leading digit above 7 would overflow 128 bits.
    [GeneratedRegex("^[0-7][0-9A-HJKMNP-TV-Z]{25}$")]
    private static partial Regex UlidPattern();

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();

    private sealed class Frontmatter
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Slug { get; set; }
        public string? Area { get; set; }
        public string? Date { get; set; }
        public List<string?>? Tags { get; set; }
        public string? Audience { get; set; }
        public string? Status { get; set; }
    }
}
