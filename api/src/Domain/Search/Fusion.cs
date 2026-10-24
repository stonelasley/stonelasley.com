namespace StoneLasley.Domain.Search;

/// <summary>One leg's Post ids, best first, and how much that leg counts in fusion.</summary>
public sealed record RankedList(IReadOnlyList<string> Ids, double Weight);

public sealed record FusedResult(string Id, double Score);

public static class Fusion
{
    /// <summary>
    /// Reciprocal Rank Fusion: a Post scores Σ weight / (k + rank) over the lists it appears in (rank from 1).
    /// It needs no score calibration between legs, which is why it suits bm25 and cosine together.
    /// </summary>
    public static IReadOnlyList<FusedResult> ReciprocalRank(IReadOnlyList<RankedList> lists, int k)
    {
        var scores = new Dictionary<string, double>();
        var firstSeen = new Dictionary<string, int>();
        foreach (var list in lists)
        {
            for (var rank = 0; rank < list.Ids.Count; rank++)
            {
                var id = list.Ids[rank];
                scores[id] = scores.GetValueOrDefault(id) + (list.Weight / (k + rank + 1));
                firstSeen.TryAdd(id, firstSeen.Count);
            }
        }
        return [.. scores.OrderByDescending(s => s.Value).ThenBy(s => firstSeen[s.Key]).Select(s => new FusedResult(s.Key, s.Value))];
    }
}

/// <summary>A vector match: one chunk of one Post and its cosine similarity to the query.</summary>
public sealed record ChunkScore(string PostId, int Chunk, double Score);

public static class ChunkRollup
{
    /// <summary>Each Post's best chunk at or above the cosine floor, best Post first.</summary>
    public static IReadOnlyList<ChunkScore> BestPerPost(IEnumerable<ChunkScore> chunks, double floor) =>
    [
        .. chunks
            .Where(chunk => chunk.Score >= floor)
            .GroupBy(chunk => chunk.PostId)
            .Select(group => group.MaxBy(chunk => chunk.Score)!)
            .OrderByDescending(chunk => chunk.Score),
    ];
}
