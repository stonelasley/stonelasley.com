using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

public sealed record ManifestResponse(string RunId, IReadOnlyList<string> Requested);

/// <summary>
/// Drives the three-step ingest protocol the way the content repo's CI does.
/// </summary>
public static class Ingest
{
    public static async Task<ManifestResponse> ManifestAsync(HttpClient client, IReadOnlyDictionary<string, string> markdownById)
    {
        var response = await client.PostAsJsonAsync(
            "/ingest/manifest",
            new { posts = markdownById.ToDictionary(p => p.Key, p => TestPosts.Hash(p.Value)) });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ManifestResponse>())!;
    }

    public static Task<HttpResponseMessage> UploadAsync(HttpClient client, string runId, string id, string markdown) =>
        client.PutAsJsonAsync($"/ingest/runs/{runId}/posts/{id}", new { markdown });

    public static Task<HttpResponseMessage> CommitAsync(HttpClient client, string runId) =>
        client.PostAsync($"/ingest/runs/{runId}/commit", null);

    /// <summary>Runs manifest, uploads whatever is requested, then commits.</summary>
    public static async Task RunAsync(HttpClient client, params string[] markdowns)
    {
        var byId = markdowns.ToDictionary(IdOf);
        var manifest = await ManifestAsync(client, byId);
        foreach (var id in manifest.Requested)
        {
            (await UploadAsync(client, manifest.RunId, id, byId[id])).EnsureSuccessStatusCode();
        }
        (await CommitAsync(client, manifest.RunId)).EnsureSuccessStatusCode();
    }

    private static string IdOf(string markdown) =>
        markdown.Split('\n').First(line => line.StartsWith("id: ", StringComparison.Ordinal))["id: ".Length..].Trim();
}
