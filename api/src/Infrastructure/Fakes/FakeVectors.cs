using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using StoneLasley.Application.Common.Interfaces;

namespace StoneLasley.Infrastructure.Fakes;

/// <summary>
/// Stands in for Workers AI where there's no Cloudflare access (fork PRs, E2E): a hashed bag of words, so texts
/// sharing words are near each other. Not semantic; enough to exercise the pipeline deterministically.
/// </summary>
internal sealed partial class FakeEmbedder : IEmbedder
{
    public const int Dimensions = 768;

    public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<float[]>>([.. texts.Select(Embed)]);

    private static float[] Embed(string text)
    {
        var vector = new float[Dimensions];
        foreach (Match word in Word().Matches(text.ToLowerInvariant()))
        {
            vector[(int)((uint)string.GetHashCode(word.Value, StringComparison.Ordinal) % Dimensions)] += 1;
        }
        var length = MathF.Sqrt(vector.Sum(v => v * v));
        return length == 0 ? vector : [.. vector.Select(v => v / length)];
    }

    [GeneratedRegex(@"[\p{L}\p{N}]{3,}")]
    private static partial Regex Word();
}

/// <summary>An in-process cosine index, shared for the life of the API.</summary>
internal sealed class InMemoryVectorIndex : IVectorIndex
{
    private readonly ConcurrentDictionary<string, ChunkVector> _vectors = new();

    public Task UpsertAsync(IReadOnlyList<ChunkVector> vectors, CancellationToken cancellationToken)
    {
        foreach (var vector in vectors)
        {
            _vectors[vector.Id] = vector;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken)
    {
        foreach (var id in ids)
        {
            _vectors.TryRemove(id, out _);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorMatch>> QueryAsync(float[] vector, int topK, string? area, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<VectorMatch>>(
        [
            .. _vectors.Values
                .Where(v => area is null || v.Area == area)
                .Select(v => new VectorMatch(v.Id, Cosine(vector, v.Values), v.PostId))
                .OrderByDescending(m => m.Score)
                .Take(topK),
        ]);

    private static double Cosine(float[] a, float[] b)
    {
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        return na == 0 || nb == 0 ? 0 : dot / Math.Sqrt(na * nb);
    }
}
