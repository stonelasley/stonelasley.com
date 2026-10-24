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
        // No SVG: it can carry script, and these are served from our own domain.
    };

    /// <summary>The largest image ingest accepts.</summary>
    public const int MaxBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Every relative image path the Markdown references, in order of first use: normalised, or as written when it
    /// climbs out of the Post's folder (see <see cref="Normalise"/>), so validation can name it.
    /// </summary>
    public static IReadOnlyList<string> References(string markdown) =>
        [.. Matches(markdown).Select(Source).Where(IsRelative).Select(src => Normalise(src) ?? src).Distinct()];

    /// <summary>
    /// <c>./a/./b.png</c> → <c>a/b.png</c>, <c>a/../b.png</c> → <c>b.png</c>; null for a path that climbs out of the
    /// Post's folder, such as <c>../secret.png</c>.
    /// </summary>
    public static string? Normalise(string path)
    {
        var parts = new List<string>();
        foreach (var part in path.Replace('\\', '/').Split('/'))
        {
            if (part is "" or ".")
            {
                continue;
            }
            if (part == "..")
            {
                if (parts.Count == 0)
                {
                    return null;
                }
                parts.RemoveAt(parts.Count - 1);
                continue;
            }
            parts.Add(part);
        }
        return parts.Count == 0 ? null : string.Join('/', parts);
    }

    /// <summary>
    /// Labels of reference-style images (<c>![alt][label]</c>) whose definition points at a relative path. Ingest
    /// can't rewrite those, so it rejects them.
    /// </summary>
    public static IReadOnlyList<string> ReferenceStyle(string markdown)
    {
        var relative = Definition().Matches(markdown)
            .Where(m => IsRelative(m.Groups["src"].Value))
            .Select(m => m.Groups["label"].Value.ToLowerInvariant())
            .ToHashSet();
        return [.. ReferenceImage().Matches(markdown)
            .Select(m => m.Groups["label"].Value.Length > 0 ? m.Groups["label"].Value : m.Groups["alt"].Value)
            .Where(label => relative.Contains(label.ToLowerInvariant()))
            .Distinct()];
    }

    /// <summary>Replaces each relative reference with its URL from <paramref name="urls"/>, keyed by normalised path.</summary>
    public static string Rewrite(string markdown, IReadOnlyDictionary<string, string> urls)
    {
        var code = CodeSpans(markdown);
        return ImageReference().Replace(markdown, m =>
        {
            var path = Normalise(Source(m)) ?? Source(m);
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

    private static string Source(Match m) => m.Groups["angled"].Success ? m.Groups["angled"].Value : m.Groups["src"].Value;

    private static bool IsRelative(string src) =>
        !(src.Contains("://", StringComparison.Ordinal) || src.StartsWith('/') || src.StartsWith("data:", StringComparison.Ordinal) || src.StartsWith('#'));

    // ![alt](src "title"), ![alt](<src with spaces>), titles in double or single quotes.
    [GeneratedRegex(@"!\[(?<alt>[^\]]*)\]\((?:<(?<angled>[^>\n]+)>|(?<src>[^)\s]+))(?<title>\s+(?:""[^""]*""|'[^']*'))?\)")]
    private static partial Regex ImageReference();

    [GeneratedRegex(@"!\[(?<alt>[^\]]*)\]\[(?<label>[^\]]*)\]")]
    private static partial Regex ReferenceImage();

    [GeneratedRegex(@"(?m)^\s{0,3}\[(?<label>[^\]]+)\]:\s*<?(?<src>[^>\s]+)>?")]
    private static partial Regex Definition();

    // Fenced blocks first, then inline code spans.
    [GeneratedRegex(@"(?ms)^(```|~~~).*?^\1[^\n]*$|`[^`\n]+`")]
    private static partial Regex Code();
}
