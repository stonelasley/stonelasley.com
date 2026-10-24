using Microsoft.Extensions.Logging;
using StoneLasley.Application.Common.Interfaces;
using StoneLasley.Domain.Posts;

namespace StoneLasley.Application.Ingest;

public sealed record PostChunks(string PostId, string Area, IReadOnlyList<string> Chunks);

/// <summary>
/// Keeps Vectorize in step with committed Posts. Runs after the D1 commit: a failure here leaves the Post's
/// vector_chunks NULL, so the next manifest asks for it again instead of the ingest failing half-applied.
/// </summary>
public sealed partial class VectorIndexer(ID1Database database, IEmbedder embedder, IVectorIndex index, ILogger<VectorIndexer> logger)
{
    private const int EmbedBatch = 50;

    /// <summary>The chunks of each Post the index should hold.</summary>
    public static PostChunks Chunk(string postId, string area, string title, string body) => new(postId, area, Chunker.Split(title, body));

    public async Task ApplyAsync(
        IReadOnlyDictionary<string, int> previousChunkCounts, IReadOnlyList<PostChunks> posts, CancellationToken cancellationToken)
    {
        try
        {
            var stale = previousChunkCounts.SelectMany(post => Enumerable.Range(0, post.Value).Select(i => VectorId(post.Key, i))).ToList();
            if (stale.Count > 0)
            {
                await index.DeleteAsync(stale, cancellationToken);
            }

            foreach (var post in posts)
            {
                var vectors = new List<ChunkVector>();
                foreach (var batch in post.Chunks.Select((text, i) => (text, i)).Chunk(EmbedBatch))
                {
                    var embedded = await embedder.EmbedAsync([.. batch.Select(chunk => chunk.text)], cancellationToken);
                    vectors.AddRange(batch.Select((chunk, j) => new ChunkVector(VectorId(post.PostId, chunk.i), embedded[j], post.PostId, post.Area)));
                }
                await index.UpsertAsync(vectors, cancellationToken);
                await database.QueryAsync("UPDATE posts SET vector_chunks = ? WHERE id = ?", [post.Chunks.Count, post.PostId], cancellationToken);
            }
        }
        // The Posts are already committed, so any failure here (a timeout included) is logged rather than thrown;
        // only the caller cancelling stops the ingest.
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogFailed(logger, exception);
        }
    }

    public static string VectorId(string postId, int chunk) => $"{postId}:{chunk}";

    [LoggerMessage(Level = LogLevel.Warning, Message = "Updating Post vectors failed; the next ingest retries the affected Posts.")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
