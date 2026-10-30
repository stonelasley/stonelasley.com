namespace StoneLasley.Domain.Posts;

/// <summary>
/// Splits a Post into the passages the vector leg embeds: one per H2 section, with long sections broken at
/// paragraph boundaries. Every chunk starts with the Post title and its section heading, so a passage keeps its
/// context once it stands alone. HTML comments are left out, as they are from the keyword index. Changing this
/// changes every vector, so it is part of the index version (#44).
/// </summary>
public static class Chunker
{
    public const int Version = 1;
    public const int MaxTokens = 400;

    public static IReadOnlyList<string> Split(string title, string body)
    {
        var chunks = new List<string>();
        foreach (var (heading, paragraphs) in Sections(body))
        {
            var prefix = heading is null ? title : $"{title} — {heading}";
            var current = new List<string>();
            foreach (var paragraph in paragraphs)
            {
                if (current.Count > 0 && EstimateTokens(string.Join("\n\n", [prefix, .. current, paragraph])) > MaxTokens)
                {
                    chunks.Add(string.Join("\n\n", [prefix, .. current]));
                    current.Clear();
                }
                current.Add(paragraph);
            }
            if (current.Count > 0)
            {
                chunks.Add(string.Join("\n\n", [prefix, .. current]));
            }
        }
        return chunks.Count > 0 ? chunks : [title];
    }

    /// <summary>A rough token count: words plus punctuation runs, close enough to keep chunks under the model's window.</summary>
    public static int EstimateTokens(string text) => text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    private static IEnumerable<(string? Heading, List<string> Paragraphs)> Sections(string body)
    {
        string? heading = null;
        var paragraphs = new List<string>();
        foreach (var block in SearchText.WithoutComments(body).ReplaceLineEndings("\n").Split("\n\n").Select(b => b.Trim()).Where(b => b.Length > 0))
        {
            if (block.StartsWith("## ", StringComparison.Ordinal))
            {
                if (paragraphs.Count > 0)
                {
                    yield return (heading, paragraphs);
                }
                var lines = block.Split('\n', 2);
                heading = lines[0][3..].Trim();
                paragraphs = lines.Length > 1 ? [lines[1].Trim()] : [];
                continue;
            }
            paragraphs.Add(block);
        }
        if (paragraphs.Count > 0)
        {
            yield return (heading, paragraphs);
        }
    }
}
