using System.Text.RegularExpressions;

namespace StoneLasley.Domain.Posts;

/// <summary>
/// What the keyword index stores for a Post, and how a reader's query becomes a safe FTS5 match expression.
/// </summary>
public static partial class SearchText
{
    public const int MaxTerms = 20;

    /// <summary>
    /// The typed core flattened to words: a Watch's make, Reference, model name, variants and calibre, or a
    /// Recipe's ingredients. References and variants are also split where digits meet letters, so "16610" finds
    /// "16610LV" (FTS5 indexes "16610LV" as one token).
    /// </summary>
    public static string Structured(Post post)
    {
        IEnumerable<string?> parts = post switch
        {
            { Watch: { } watch } =>
            [
                watch.Make.Name(), watch.Reference, watch.ModelName, .. watch.Variants, watch.Calibre,
                .. new[] { watch.Reference }.Concat(watch.Variants).SelectMany(SplitParts),
            ],
            { Recipe: { } recipe } => recipe.Ingredients.SelectMany(i => new[] { i.Name, i.Note }),
            _ => [],
        };
        return Clean(string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p))));
    }

    // "16610LV" → ["16610", "LV"]; "5711/1A" → ["1", "A"] (5711 is already a token). Only parts that differ
    // from the whole tokens are added.
    private static IEnumerable<string> SplitParts(string reference)
    {
        var tokens = Term().Matches(reference).Select(m => m.Value).ToHashSet();
        return Term().Matches(reference)
            .SelectMany(m => DigitLetterRun().Matches(m.Value).Select(r => r.Value))
            .Where(part => !tokens.Contains(part))
            .Distinct();
    }

    /// <summary>The Markdown body as plain words: syntax gone, code kept as text.</summary>
    public static string Body(string markdown)
    {
        var text = Fence().Replace(markdown, "");
        text = Image().Replace(text, "$1");
        text = Link().Replace(text, "$1");
        text = Syntax().Replace(text, " ");
        return Clean(text);
    }

    /// <summary>
    /// Each word of the query quoted and OR-joined, so nothing a reader types is read as FTS5 syntax. Null when
    /// the query has no words.
    /// </summary>
    public static string? MatchExpression(string query)
    {
        var terms = Term().Matches(query).Select(m => m.Value).Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxTerms).ToList();
        return terms.Count == 0 ? null : string.Join(" OR ", terms.Select(t => $"\"{t}\""));
    }

    // Control characters are what snippet() marks matches with, so indexed text must never contain them.
    private static string Clean(string text) => Whitespace().Replace(Control().Replace(text, " "), " ").Trim();

    [GeneratedRegex(@"\p{Cc}")]
    private static partial Regex Control();

    [GeneratedRegex(@"\p{N}+|\p{L}+")]
    private static partial Regex DigitLetterRun();

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
