using StoneLasley.Application.Common.Interfaces;

namespace StoneLasley.Api.FunctionalTests;

/// <summary>
/// A test embedder at the Workers AI port that knows a few concepts: texts naming the same one point the same
/// way, whatever their words. It lets a test make the vector leg match where the keyword leg can't.
/// </summary>
public sealed class ConceptEmbedder : IEmbedder
{
    private static readonly string[][] Concepts =
    [
        ["barbell", "deadlift", "hinge"],
        ["watch", "bezel", "chronometer"],
    ];

    private static uint Fnv1a(string text)
    {
        var hash = 2166136261u;
        foreach (var c in text)
        {
            hash = (hash ^ c) * 16777619u;
        }
        return hash;
    }

    public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<float[]>>([.. texts.Select(Embed)]);

    private static float[] Embed(string text)
    {
        var vector = new float[768];
        var lower = text.ToLowerInvariant();
        for (var i = 0; i < Concepts.Length; i++)
        {
            if (Concepts[i].Any(lower.Contains))
            {
                vector[i] = 1;
            }
        }
        if (vector.All(v => v == 0))
        {
            // No known concept: a bucket of its own, so unrelated texts aren't similar to each other.
            vector[Concepts.Length + (int)(Fnv1a(lower) % (uint)(vector.Length - Concepts.Length))] = 1;
        }
        return vector;
    }
}
