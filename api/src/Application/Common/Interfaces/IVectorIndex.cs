namespace StoneLasley.Application.Common.Interfaces;

public sealed record ChunkVector(string Id, float[] Values, string PostId, string Area);

public sealed record VectorMatch(string Id, double Score, string PostId);

/// <summary>Turns text into vectors (Workers AI bge-base-en-v1.5, 768 dimensions).</summary>
public interface IEmbedder
{
    Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken);
}

/// <summary>
/// The Vectorize index of Post chunks, one per index version (embedding model + chunker), one namespace per
/// environment (or per test run).
/// </summary>
public interface IVectorIndex
{
    Task UpsertAsync(string version, IReadOnlyList<ChunkVector> vectors, CancellationToken cancellationToken);

    Task DeleteAsync(string version, IReadOnlyList<string> ids, CancellationToken cancellationToken);

    Task<IReadOnlyList<VectorMatch>> QueryAsync(string version, float[] vector, int topK, string? area, CancellationToken cancellationToken);
}
