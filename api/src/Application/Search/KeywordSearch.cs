using System.Text.Json;
using Microsoft.Extensions.Options;
using StoneLasley.Application.Common.Interfaces;
using StoneLasley.Application.Posts;
using StoneLasley.Domain.Posts;

namespace StoneLasley.Application.Search;

/// <summary>One run of an excerpt: matched words are highlighted, the rest is plain text.</summary>
public sealed record HighlightPart(string Text, bool Match);

/// <summary>
/// One result. <paramref name="Hero"/> is the image beside it; a Watch without one is stood in for by its make and
/// reference, so <paramref name="Watch"/> carries those.
/// </summary>
public sealed record SearchHit(
    string Id, string Title, string Slug, string Area, string Date, IReadOnlyList<HighlightPart> Excerpt, string? Hero, SearchWatch? Watch);

public sealed record SearchWatch(WatchMake Make, string Reference)
{
    public static SearchWatch? Of(Watch? watch) => watch is null ? null : new SearchWatch(watch.Make, watch.Reference);
}

public sealed record SearchResult(string Query, string? Area, IReadOnlyList<SearchHit> Results, IReadOnlyDictionary<string, int> Counts);

/// <summary>
/// The keyword leg: D1 FTS5 over public, published Posts, ranked by weighted bm25.
/// </summary>
public sealed class KeywordSearch(ID1Database database, IOptions<SearchOptions> options)
{
    // Control characters FTS5 wraps around matches in snippet(); they can't occur in a Post's text.
    private const char Open = '\u0002';
    private const char Close = '\u0003';

    private const string Readable = $"p.audience = '{PostColumns.PublicAudience}' AND p.status = '{PostColumns.PublishedStatus}'";

    public async Task<SearchResult> SearchAsync(string query, ContentArea? area, CancellationToken cancellationToken)
    {
        var areaSlug = area?.Slug();
        if (SearchText.MatchExpression(query) is not { } match)
        {
            return new SearchResult(query, areaSlug, [], new Dictionary<string, int>());
        }
        var hits = await RankAsync(match, areaSlug, cancellationToken);
        var matched = await MatchedAreasAsync(match, cancellationToken);
        return new SearchResult(query, areaSlug, hits, matched.Values.GroupBy(a => a).OrderBy(g => g.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count()));
    }

    /// <summary>Hits in bm25 order, with highlighted snippets, within one Content Area or all.</summary>
    public async Task<IReadOnlyList<SearchHit>> RankAsync(string match, string? areaSlug, CancellationToken cancellationToken)
    {

        var weights = options.Value.Bm25;
        var areaFilter = areaSlug is null ? "" : "AND p.area = ?";
        object?[] filterParams = areaSlug is null ? [match] : [match, areaSlug];
        var rows = await database.QueryAsync(
            $"""
            SELECT p.id, p.title, p.slug, p.area, p.date, p.hero, p.watch,
                   snippet(posts_fts, -1, char(2), char(3), '…', 24) AS snippet
            FROM posts_fts JOIN posts p ON p.id = posts_fts.post_id
            WHERE posts_fts MATCH ? AND {Readable} {areaFilter}
            ORDER BY bm25(posts_fts, 0, ?, ?, ?, ?), p.date DESC
            LIMIT ?
            """,
            [.. filterParams, weights.Title, weights.Structured, weights.Tags, weights.Body, options.Value.Limit],
            cancellationToken);

        return [.. rows.Select(row => new SearchHit(
            Text(row, "id"), Text(row, "title"), Text(row, "slug"), Text(row, "area"), Text(row, "date"), Highlights(Text(row, "snippet")),
            PostColumns.HeroFromRow(row), SearchWatch.Of(PostColumns.WatchFromRow(row))))];
    }

    /// <summary>Every matching Post's Content Area, across all areas, for per-area counts.</summary>
    public async Task<IReadOnlyDictionary<string, string>> MatchedAreasAsync(string match, CancellationToken cancellationToken)
    {
        var rows = await database.QueryAsync(
            $"""
            SELECT p.id, p.area
            FROM posts_fts JOIN posts p ON p.id = posts_fts.post_id
            WHERE posts_fts MATCH ? AND {Readable}
            """,
            [match],
            cancellationToken);
        return rows.ToDictionary(row => Text(row, "id"), row => Text(row, "area"));
    }

    private static List<HighlightPart> Highlights(string snippet)
    {
        var parts = new List<HighlightPart>();
        var rest = snippet.AsSpan();
        while (rest.Length > 0)
        {
            var open = rest.IndexOf(Open);
            if (open < 0)
            {
                parts.Add(new HighlightPart(rest.ToString(), false));
                break;
            }
            if (open > 0)
            {
                parts.Add(new HighlightPart(rest[..open].ToString(), false));
            }
            rest = rest[(open + 1)..];
            var close = rest.IndexOf(Close);
            var end = close < 0 ? rest.Length : close;
            parts.Add(new HighlightPart(rest[..end].ToString(), true));
            rest = close < 0 ? [] : rest[(close + 1)..];
        }
        return parts;
    }

    private static string Text(JsonElement row, string column) => row.GetProperty(column).GetString()!;
}
