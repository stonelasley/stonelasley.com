using Microsoft.Extensions.Options;
using StoneLasley.Domain.Posts;
using StoneLasley.Domain.Search;

namespace StoneLasley.Application.Search;

/// <summary>
/// Answers a search with the configured leg, or with both fused by Reciprocal Rank Fusion.
/// </summary>
public sealed class Searcher(KeywordSearch keyword, VectorSearch vector, IOptions<SearchOptions> options)
{
    public Task<SearchResult> SearchAsync(string query, ContentArea? area, SearchMode? mode, CancellationToken cancellationToken) =>
        (mode ?? options.Value.Mode) switch
        {
            SearchMode.Keyword => keyword.SearchAsync(query, area, cancellationToken),
            SearchMode.Vector => vector.SearchAsync(query, area, cancellationToken),
            _ => HybridAsync(query, area, cancellationToken),
        };

    private async Task<SearchResult> HybridAsync(string query, ContentArea? area, CancellationToken cancellationToken)
    {
        var areaSlug = area?.Slug();
        if (SearchText.MatchExpression(query) is not { } match)
        {
            return new SearchResult(query, areaSlug, [], new Dictionary<string, int>());
        }

        var embedding = await vector.EmbedQueryAsync(query, cancellationToken);
        var keywordHits = await keyword.RankAsync(match, areaSlug, cancellationToken);
        var vectorHits = await vector.RankAsync(embedding, areaSlug, cancellationToken);

        var rrf = options.Value.Rrf;
        var fused = Fusion.ReciprocalRank(
            [
                new RankedList([.. keywordHits.Select(hit => hit.Id)], rrf.KeywordWeight),
                new RankedList([.. vectorHits.Select(hit => hit.Id)], rrf.VectorWeight),
            ],
            rrf.K);

        // The excerpt is the FTS5 snippet when the keyword leg matched, otherwise the opening of the best chunk.
        var byKeyword = keywordHits.ToDictionary(hit => hit.Id);
        var byVector = vectorHits.ToDictionary(hit => hit.Id);
        var results = fused
            .Take(options.Value.Limit)
            .Select(result => byKeyword.TryGetValue(result.Id, out var hit)
                ? hit
                : byVector[result.Id].ToHit())
            .ToList();

        // Counts cover every area, whatever the filter: any Post either leg matched.
        var matched = new Dictionary<string, string>(await keyword.MatchedAreasAsync(match, cancellationToken));
        var everywhere = areaSlug is null ? vectorHits : await vector.RankAsync(embedding, null, cancellationToken);
        foreach (var hit in everywhere)
        {
            matched.TryAdd(hit.Id, hit.Area);
        }
        var counts = matched.Values.GroupBy(a => a).OrderBy(group => group.Key, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count());

        return new SearchResult(query, areaSlug, results, counts);
    }
}
