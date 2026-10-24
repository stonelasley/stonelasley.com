using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace StoneLasley.Domain.Posts;

/// <summary>
/// Images kept next to a Post and referenced relatively, such as <c>![Hero](hero.jpg)</c>. Ingest stores each
/// under a key from the hash of its bytes and rewrites the reference to the public URL (ADR 0002).
/// </summary>
public static partial class PostImages
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".avif"] = "image/avif",
        [".svg"] = "image/svg+xml",
    };

    /// <summary>Every relative image path the Markdown references, normalised and in order of first use.</summary>
    public static IReadOnlyList<string> References(string markdown) =>
        [.. Matches(markdown).Select(m => Normalise(m.Groups["src"].Value)).Where(IsRelative).Distinct()];

    /// <summary>Replaces each relative reference with its URL from <paramref name="urls"/>, keyed by normalised path.</summary>
    public static string Rewrite(string markdown, IReadOnlyDictionary<string, string> urls)
    {
        var code = CodeSpans(markdown);
        return ImageReference().Replace(markdown, m =>
        {
            var path = Normalise(m.Groups["src"].Value);
            return IsInside(m.Index, code) || !urls.TryGetValue(path, out var url)
                ? m.Value
                : $"![{m.Groups["alt"].Value}]({url}{m.Groups["title"].Value})";
        });
    }

    public static string? ContentType(string path) => ContentTypes.GetValueOrDefault(Path.GetExtension(path));

    /// <summary><c>images/{sha256}{ext}</c>: identical bytes always land on the same key.</summary>
    public static string Key(string path, byte[] bytes) =>
        $"images/{Convert.ToHexStringLower(SHA256.HashData(bytes))}{Path.GetExtension(path).ToLowerInvariant()}";

    private static IEnumerable<Match> Matches(string markdown)
    {
        var code = CodeSpans(markdown);
        return ImageReference().Matches(markdown).Where(m => !IsInside(m.Index, code));
    }

    private static List<(int Start, int End)> CodeSpans(string markdown) =>
        [.. Code().Matches(markdown).Select(m => (m.Index, m.Index + m.Length))];

    private static bool IsInside(int index, List<(int Start, int End)> spans) => spans.Any(s => index >= s.Start && index < s.End);

    private static string Normalise(string src) => src.StartsWith("./", StringComparison.Ordinal) ? src[2..] : src;

    private static bool IsRelative(string src) =>
        !(src.Contains("://", StringComparison.Ordinal) || src.StartsWith('/') || src.StartsWith("data:", StringComparison.Ordinal) || src.StartsWith('#'));

    [GeneratedRegex(@"!\[(?<alt>[^\]]*)\]\((?<src>[^)\s]+)(?<title>\s+""[^""]*"")?\)")]
    private static partial Regex ImageReference();

    // Fenced blocks first, then inline code spans.
    [GeneratedRegex(@"(?ms)^(```|~~~).*?^\1[^\n]*$|`[^`\n]+`")]
    private static partial Regex Code();
}
