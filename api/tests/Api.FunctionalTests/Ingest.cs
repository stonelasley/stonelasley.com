using System.Net.Http.Json;

namespace StoneLasley.Api.FunctionalTests;

public sealed record ManifestResponse(string RunId, IReadOnlyList<string> Requested);

public sealed record CommitResponse(bool DryRun, IReadOnlyList<string> Upserted, IReadOnlyList<string> Deleted);

/// <summary>
/// Drives the three-step ingest protocol the way the content repo's CI does.
/// </summary>
public static class Ingest
{
    public static async Task<ManifestResponse> ManifestAsync(
        HttpClient client,
        IReadOnlyDictionary<string, string> markdownById,
        bool dryRun = false,
        bool includeDrafts = false,
        string? clients = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/ingest/manifest?dryRun={dryRun}&includeDrafts={includeDrafts}",
            new { posts = markdownById.ToDictionary(p => p.Key, p => TestPosts.Hash(p.Value)), clients });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ManifestResponse>())!;
    }

    public static Task<HttpResponseMessage> UploadAsync(HttpClient client, string runId, string id, string markdown) =>
        client.PutAsJsonAsync($"/ingest/runs/{runId}/posts/{id}", new { markdown });

    public static Task<HttpResponseMessage> UploadAsync(
        HttpClient client, string runId, string id, string markdown, IReadOnlyDictionary<string, byte[]> images) =>
        client.PutAsJsonAsync(
            $"/ingest/runs/{runId}/posts/{id}",
            new { markdown, images = images.Select(i => new { path = i.Key, content = Convert.ToBase64String(i.Value) }) });

    public static Task<HttpResponseMessage> CommitAsync(HttpClient client, string runId) =>
        client.PostAsync($"/ingest/runs/{runId}/commit", null);

    /// <summary>Runs manifest, uploads whatever is requested, then commits.</summary>
    public static async Task<CommitResponse> RunAsync(HttpClient client, params string[] markdowns) =>
        await RunAsync(client, dryRun: false, includeDrafts: false, markdowns);

    public static async Task<CommitResponse> RunAsync(HttpClient client, bool dryRun, bool includeDrafts, params string[] markdowns) =>
        await RunAsync(client, dryRun, includeDrafts, clients: null, markdowns);

    /// <summary>Runs an ingest that also syncs the clients file (YAML).</summary>
    public static async Task<CommitResponse> RunWithClientsAsync(HttpClient client, string clients, params string[] markdowns) =>
        await RunAsync(client, dryRun: false, includeDrafts: false, clients, markdowns);

    private static async Task<CommitResponse> RunAsync(HttpClient client, bool dryRun, bool includeDrafts, string? clients, string[] markdowns)
    {
        var byId = ById(markdowns);
        var manifest = await ManifestAsync(client, byId, dryRun, includeDrafts, clients);
        foreach (var id in manifest.Requested)
        {
            (await UploadAsync(client, manifest.RunId, id, byId[id])).EnsureSuccessStatusCode();
        }
        var commit = await CommitAsync(client, manifest.RunId);
        commit.EnsureSuccessStatusCode();
        return (await commit.Content.ReadFromJsonAsync<CommitResponse>())!;
    }

    public static Dictionary<string, string> ById(params string[] markdowns) => markdowns.ToDictionary(IdOf);

    private static string IdOf(string markdown) =>
        markdown.Split('\n').First(line => line.StartsWith("id: ", StringComparison.Ordinal))["id: ".Length..].Trim();
}
