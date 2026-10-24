using System.Text.RegularExpressions;

namespace StoneLasley.Domain.Posts;

/// <summary>
/// A Post's opening prose as plain text, for index summaries, feeds and link previews.
/// </summary>
public static partial class Excerpt
{
    public const int MaxLength = 160;

    public static string From(string markdown)
    {
        var withoutCode = Fence().Replace(markdown.ReplaceLineEndings("\n"), "\n\n");
        foreach (var block in Blocks(withoutCode).Where(IsProse))
        {
            var text = Whitespace().Replace(Plain(block), " ").Trim();
            if (text.Length > 0)
            {
                return text.Length <= MaxLength ? text : Cut(text);
            }
        }
        return string.Empty;
    }

    private static IEnumerable<string> Blocks(string markdown) =>
        markdown.ReplaceLineEndings("\n").Split("\n\n").Select(b => b.Trim('\n', ' ')).Where(b => b.Length > 0);

    private static bool IsProse(string block) =>
        !(block.StartsWith('#') || block.StartsWith('|') || block.StartsWith('>') || block.StartsWith("![", StringComparison.Ordinal)
          || ListItem().IsMatch(block) || block.StartsWith("<", StringComparison.Ordinal) || block.StartsWith("---", StringComparison.Ordinal));

    private static string Plain(string block)
    {
        var text = Image().Replace(block, "");
        text = Link().Replace(text, "$1");
        text = Emphasis().Replace(text, "$2");
        return text.Replace("`", "", StringComparison.Ordinal);
    }

    private static string Cut(string text)
    {
        var cut = text[..(MaxLength - 1)];
        var space = cut.LastIndexOf(' ');
        return (space > 0 ? cut[..space] : cut).TrimEnd(',', ';', ':', '.', ' ') + "…";
    }

    [GeneratedRegex(@"^(```|~~~).*?^\1[^\n]*$", RegexOptions.Singleline | RegexOptions.Multiline)]
    private static partial Regex Fence();

    [GeneratedRegex(@"!\[[^\]]*\]\([^)]*\)")]
    private static partial Regex Image();

    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex Link();

    [GeneratedRegex(@"(\*\*|__|\*|_)(.+?)\1")]
    private static partial Regex Emphasis();

    [GeneratedRegex(@"^(\s*[-*+]\s|\s*\d+[.)]\s)")]
    private static partial Regex ListItem();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
