using System.Text.RegularExpressions;

namespace StoneLasley.Domain.Posts;

/// <summary>
/// What the keyword index stores for a Post, and how a reader's query becomes a safe FTS5 match expression.
/// </summary>
public static partial class SearchText
{
    /// <summary>The typed core flattened to words: a Watch's make, Reference, model name, variants and calibre, or a Recipe's ingredients.</summary>
    public static string Structured(Post post)
    {
        IEnumerable<string?> parts = post switch
        {
            { Watch: { } watch } => [watch.Make.Name(), watch.Reference, watch.ModelName, .. watch.Variants, watch.Calibre],
            { Recipe: { } recipe } => recipe.Ingredients.SelectMany(i => new[] { i.Name, i.Note }),
            _ => [],
        };
        return string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    /// <summary>The Markdown body as plain words: syntax gone, code kept as text.</summary>
    public static string Body(string markdown)
    {
        var text = Fence().Replace(markdown, "");
        text = Image().Replace(text, "$1");
        text = Link().Replace(text, "$1");
        text = Syntax().Replace(text, " ");
        return Whitespace().Replace(text, " ").Trim();
    }

    /// <summary>
    /// Each word of the query quoted and OR-joined, so nothing a reader types is read as FTS5 syntax. Null when
    /// the query has no words.
    /// </summary>
    public static string? MatchExpression(string query)
    {
        var terms = Term().Matches(query).Select(m => m.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return terms.Count == 0 ? null : string.Join(" OR ", terms.Select(t => $"\"{t}\""));
    }

    [GeneratedRegex(@"(?m)^(```|~~~)[^\n]*$")]
    private static partial Regex Fence();

    [GeneratedRegex(@"!\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex Image();

    [GeneratedRegex(@"\[([^\]]*)\]\((?:[^()]|\([^()]*\))*\)")]
    private static partial Regex Link();

    [GeneratedRegex(@"[#*_`>|~]+|^\s*[-+]\s", RegexOptions.Multiline)]
    private static partial Regex Syntax();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    // Letters and digits only, the same split the unicode61 tokenizer makes.
    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex Term();
}
