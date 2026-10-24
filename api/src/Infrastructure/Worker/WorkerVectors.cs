using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using StoneLasley.Application.Common.Interfaces;
using StoneLasley.Application.Search;

namespace StoneLasley.Infrastructure.Worker;

/// <summary>Workers AI embeddings through the Worker's <c>/ai/embed</c>.</summary>
internal sealed class WorkerEmbedder(HttpClient http) : IEmbedder
{
    public async Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync("ai/embed", new { texts }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken)
            ?? throw new InvalidOperationException("The Worker returned no embeddings.");
        return body.Vectors;
    }

    private sealed record EmbedResponse(IReadOnlyList<float[]> Vectors);
}

/// <summary>Vectorize through the Worker's <c>/vectors/*</c>, in this environment's namespace.</summary>
internal sealed class WorkerVectorIndex(HttpClient http, IOptions<SearchOptions> options) : IVectorIndex
{
    private const int DeleteBatch = 1000;

    // Preview and production use the index's default namespace: leave "namespace" out rather than send null.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private string? Namespace => options.Value.Vector.Namespace;

    public async Task UpsertAsync(string version, IReadOnlyList<ChunkVector> vectors, CancellationToken cancellationToken)
    {
        if (vectors.Count == 0)
        {
            return;
        }
        var payload = vectors.Select(v => new
        {
            id = v.Id,
            values = v.Values,
            @namespace = Namespace,
            metadata = new { postId = v.PostId, area = v.Area },
        });
        using var response = await http.PostAsJsonAsync("vectors/upsert", new { version, vectors = payload }, Json, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string version, IReadOnlyList<string> ids, CancellationToken cancellationToken)
    {
        foreach (var batch in ids.Chunk(DeleteBatch))
        {
            using var response = await http.PostAsJsonAsync("vectors/delete", new { version, ids = batch }, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<IReadOnlyList<VectorMatch>> QueryAsync(string version, float[] vector, int topK, string? area, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(
            "vectors/query",
            new { version, vector, topK, @namespace = Namespace, filter = area is null ? null : new { area } },
            Json,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<QueryResponse>(cancellationToken)
            ?? throw new InvalidOperationException("The Worker returned no matches.");
        return [.. body.Matches.Select(m => new VectorMatch(m.Id, m.Score, m.Metadata?.PostId ?? m.Id.Split(':')[0]))];
    }

    private sealed record QueryResponse(IReadOnlyList<Match> Matches);

    private sealed record Match(string Id, double Score, MatchMetadata? Metadata);

    private sealed record MatchMetadata(string? PostId);
}
