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
        foreach (var block in Blocks(withoutCode).Select(DropLeadingHeadings).Where(IsProse))
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
        markdown.Split("\n\n").Select(b => b.Trim('\n', ' ')).Where(b => b.Length > 0);

    // A heading line directly above a paragraph, with no blank line between them.
    private static string DropLeadingHeadings(string block)
    {
        var lines = block.Split('\n');
        var start = 0;
        while (start < lines.Length - 1 && lines[start].StartsWith('#'))
        {
            start++;
        }
        return string.Join('\n', lines[start..]);
    }

    private static bool IsProse(string block) =>
        !(block.StartsWith('#') || block.StartsWith('|') || block.StartsWith('>') || block.StartsWith("![", StringComparison.Ordinal)
          || ListItem().IsMatch(block) || block.StartsWith("<", StringComparison.Ordinal) || block.StartsWith("---", StringComparison.Ordinal));

    private static string Plain(string block)
    {
        var text = Image().Replace(block, "");
        text = Link().Replace(text, "$1");
        // Inner emphasis first, so "**bold *and* italic**" unwraps fully.
        for (var pass = 0; pass < 3; pass++)
        {
            text = Emphasis().Replace(text, m => m.Groups[2].Success ? m.Groups[2].Value : m.Groups[4].Value);
        }
        return text.Replace("`", "", StringComparison.Ordinal);
    }

    private static string Cut(string text)
    {
        var length = MaxLength - 1;
        if (char.IsHighSurrogate(text[length - 1]))
        {
            length--;
        }
        var cut = text[..length];
        var space = cut.LastIndexOf(' ');
        return (space > 0 ? cut[..space] : cut).TrimEnd(',', ';', ':', '.', ' ') + "…";
    }

    [GeneratedRegex(@"^(```|~~~).*?^\1[^\n]*$", RegexOptions.Singleline | RegexOptions.Multiline)]
    private static partial Regex Fence();

    [GeneratedRegex(@"!\[[^\]]*\]\([^)]*\)")]
    private static partial Regex Image();

    // One level of parentheses inside the URL, as in Wikipedia links.
    [GeneratedRegex(@"\[([^\]]*)\]\((?:[^()]|\([^()]*\))*\)")]
    private static partial Regex Link();

    // Emphasis must hug its text ("2 * 3" isn't emphasis), and underscores only count at word edges
    // (snake_case stays whole).
    [GeneratedRegex(@"(\*\*|\*)(?=\S)(.+?)(?<=\S)\1|(?<!\w)(__|_)(?=\S)(.+?)(?<=\S)\3(?!\w)")]
    private static partial Regex Emphasis();

    // Up to three digits: "1986. The year…" is prose, not a numbered list.
    [GeneratedRegex(@"^(\s*[-*+]\s|\s*\d{1,3}[.)]\s)")]
    private static partial Regex ListItem();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
