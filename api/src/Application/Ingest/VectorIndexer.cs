using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StoneLasley.Application.Common.Interfaces;
using StoneLasley.Application.Posts;
using StoneLasley.Application.Search;
using StoneLasley.Domain.Posts;

namespace StoneLasley.Application.Ingest;

public sealed record PostChunks(string PostId, string Area, IReadOnlyList<string> Chunks);

/// <summary>
/// Keeps Vectorize in step with committed Posts. Runs after the D1 commit: a failure here leaves the Post's
/// vector_chunks NULL, so the next manifest asks for it again instead of the ingest failing half-applied.
/// </summary>
public sealed partial class VectorIndexer(
    ID1Database database, IEmbedder embedder, IVectorIndex index, IOptions<SearchOptions> options, ILogger<VectorIndexer> logger)
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
                await index.DeleteAsync(Active, stale, cancellationToken);
            }

            foreach (var post in posts)
            {
                await index.UpsertAsync(Active, await EmbedAsync(post, cancellationToken), cancellationToken);
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

    private string Active => options.Value.Vector.IndexVersion;

    private async Task<List<ChunkVector>> EmbedAsync(PostChunks post, CancellationToken cancellationToken)
    {
        var vectors = new List<ChunkVector>();
        foreach (var batch in post.Chunks.Select((text, i) => (text, i)).Chunk(EmbedBatch))
        {
            var embedded = await embedder.EmbedAsync([.. batch.Select(chunk => chunk.text)], cancellationToken);
            vectors.AddRange(batch.Select((chunk, j) => new ChunkVector(VectorId(post.PostId, chunk.i), embedded[j], post.PostId, post.Area)));
        }
        return vectors;
    }

    /// <summary>
    /// Embeds every public, published Post into the index for <paramref name="version"/> while search keeps
    /// reading the active one, recording each Post's chunk count for promotion. Returns (posts, vectors).
    /// </summary>
    public async Task<(int Posts, int Vectors)> RebuildAsync(string version, CancellationToken cancellationToken)
    {
        var rows = await database.QueryAsync(
            $"SELECT id, area, title, body FROM posts WHERE audience = '{PostColumns.PublicAudience}' AND status = '{PostColumns.PublishedStatus}'",
            cancellationToken: cancellationToken);
        await database.QueryAsync("DELETE FROM post_vector_builds WHERE version = ?", [version], cancellationToken);
        var vectorCount = 0;
        foreach (var row in rows)
        {
            var post = Chunk(Text(row, "id"), Text(row, "area"), Text(row, "title"), Text(row, "body"));
            var vectors = await EmbedAsync(post, cancellationToken);
            await index.UpsertAsync(version, vectors, cancellationToken);
            await database.QueryAsync(
                "INSERT INTO post_vector_builds (version, post_id, chunks) VALUES (?, ?, ?)", [version, post.PostId, post.Chunks.Count], cancellationToken);
            vectorCount += vectors.Count;
        }
        return (rows.Count, vectorCount);
    }

    /// <summary>
    /// Adopts a finished build once the API runs on its version: chunk counts come from the build, and a Post the
    /// build missed (changed while it ran) gets NULL, so the next ingest indexes it.
    /// </summary>
    public async Task<bool> PromoteAsync(string version, CancellationToken cancellationToken)
    {
        // No build for this version (already promoted, or never built): nothing to adopt, and copying from an
        // empty build would mark every Post unindexed.
        if ((await database.QueryAsync("SELECT 1 FROM post_vector_builds WHERE version = ? LIMIT 1", [version], cancellationToken)).Count == 0)
        {
            return false;
        }
        await database.BatchAsync(
        [
            new D1Statement(
                "UPDATE posts SET vector_chunks = (SELECT chunks FROM post_vector_builds b WHERE b.version = ? AND b.post_id = posts.id)",
                [version]),
            new D1Statement("DELETE FROM post_vector_builds WHERE version = ?", [version]),
        ], cancellationToken);
        return true;
    }

    private static string Text(System.Text.Json.JsonElement row, string column) => row.GetProperty(column).GetString()!;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Updating Post vectors failed; the next ingest retries the affected Posts.")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
