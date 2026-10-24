using System.Text.Json;
using Microsoft.Extensions.Options;
using StoneLasley.Application.Common.Interfaces;
using StoneLasley.Application.Posts;
using StoneLasley.Domain.Posts;

namespace StoneLasley.Application.Search;

/// <summary>
/// The vector leg: embed the query, fetch the nearest chunks, drop those under the cosine floor, and score each
/// Post by its best chunk. The excerpt is the opening of that chunk.
/// </summary>
public sealed class VectorSearch(ID1Database database, IEmbedder embedder, IVectorIndex index, IOptions<SearchOptions> options)
{
    private const string Readable = $"audience = '{PostColumns.PublicAudience}' AND status = '{PostColumns.PublishedStatus}'";

    public async Task<SearchResult> SearchAsync(string query, ContentArea? area, CancellationToken cancellationToken)
    {
        var areaSlug = area?.Slug();
        if (SearchText.MatchExpression(query) is null)
        {
            return new SearchResult(query, areaSlug, [], new Dictionary<string, int>());
        }

        var vector = (await embedder.EmbedAsync([query], cancellationToken))[0];
        var ranked = await RankAsync(vector, areaSlug, cancellationToken);
        var everywhere = area is null ? ranked : await RankAsync(vector, null, cancellationToken);

        var counts = everywhere.GroupBy(post => post.Area).ToDictionary(group => group.Key, group => group.Count());
        return new SearchResult(
            query,
            areaSlug,
            [.. ranked.Select(post => new SearchHit(post.Id, post.Title, post.Slug, post.Area, post.Date, [new HighlightPart(post.Excerpt, false)]))],
            counts);
    }

    /// <summary>Posts ranked by their best chunk's similarity, for fusion (#42) as well as the vector-only mode.</summary>
    public async Task<IReadOnlyList<RankedPost>> RankAsync(float[] vector, string? area, CancellationToken cancellationToken)
    {
        var settings = options.Value.Vector;
        var best = (await index.QueryAsync(vector, settings.TopK, area, cancellationToken))
            .Where(match => match.Score >= settings.CosineFloor)
            .GroupBy(match => match.PostId)
            .Select(group => group.MaxBy(match => match.Score)!)
            .ToList();
        if (best.Count == 0)
        {
            return [];
        }

        var rows = await database.QueryAsync(
            $"""
            SELECT p.id, p.title, p.slug, p.area, p.date, c.text
            FROM posts p JOIN post_chunks c ON c.post_id = p.id
            WHERE {Readable.Replace("audience", "p.audience").Replace("status", "p.status")}
              AND (p.id || ':' || c.idx) IN (SELECT value FROM json_each(?))
            """,
            [JsonSerializer.Serialize(best.Select(match => match.Id))],
            cancellationToken);
        var posts = rows.ToDictionary(row => Text(row, "id"));

        return
        [
            .. best
                .Where(match => posts.ContainsKey(match.PostId))
                .OrderByDescending(match => match.Score)
                .Select(match =>
                {
                    var row = posts[match.PostId];
                    return new RankedPost(
                        match.PostId, Text(row, "title"), Text(row, "slug"), Text(row, "area"), Text(row, "date"), match.Score, Opening(Text(row, "text")));
                }),
        ];
    }

    // A chunk starts with its "Title — Heading" line; the excerpt is the prose after it.
    private static string Opening(string chunk)
    {
        var start = chunk.IndexOf("\n\n", StringComparison.Ordinal);
        return Excerpt.From(start < 0 ? chunk : chunk[(start + 2)..]);
    }

    private static string Text(JsonElement row, string column) => row.GetProperty(column).GetString()!;
}

public sealed record RankedPost(string Id, string Title, string Slug, string Area, string Date, double Score, string Excerpt);
