using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace StoneLasley.Cli;

/// <summary>
/// Sends every Markdown Post under a directory through the API's manifest → upload → commit protocol.
/// The API parses and validates; this only finds each Post's id and hashes its file.
/// </summary>
internal static partial class IngestCommand
{
    public static async Task<int> RunAsync(Uri apiUrl, string token, string directory)
    {
        var posts = new Dictionary<string, string>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.md", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var markdown = await File.ReadAllTextAsync(path);
            var id = IdPattern().Match(markdown);
            if (!id.Success)
            {
                await Console.Error.WriteLineAsync($"{path}: no 'id' in the frontmatter.");
                return 1;
            }
            if (!posts.TryAdd(id.Groups["id"].Value, markdown))
            {
                await Console.Error.WriteLineAsync($"{path}: id {id.Groups["id"].Value} is used by another Post.");
                return 1;
            }
        }

        using var client = new HttpClient { BaseAddress = apiUrl };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var manifestResponse = await client.PostAsJsonAsync(
            "ingest/manifest",
            new { posts = posts.ToDictionary(p => p.Key, p => Hash(p.Value)) });
        if (!await SucceededAsync(manifestResponse, "manifest"))
        {
            return 1;
        }
        var manifest = (await manifestResponse.Content.ReadFromJsonAsync<Manifest>())!;

        foreach (var id in manifest.Requested)
        {
            var upload = await client.PutAsJsonAsync($"ingest/runs/{manifest.RunId}/posts/{id}", new { markdown = posts[id] });
            if (!await SucceededAsync(upload, $"upload {id}"))
            {
                return 1;
            }
        }

        if (!await SucceededAsync(await client.PostAsync($"ingest/runs/{manifest.RunId}/commit", null), "commit"))
        {
            return 1;
        }

        Console.WriteLine($"Ingested {manifest.Requested.Count} of {posts.Count} Posts ({posts.Count - manifest.Requested.Count} unchanged).");
        return 0;
    }

    private static string Hash(string markdown) =>
        Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(markdown)));

    private static async Task<bool> SucceededAsync(HttpResponseMessage response, string step)
    {
        if (response.IsSuccessStatusCode)
        {
            return true;
        }
        await Console.Error.WriteLineAsync($"{step} failed: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return false;
    }

    [GeneratedRegex(@"\A---\r?\n(?:.*\r?\n)*?id:\s*(?<id>\S+)\s*\r?\n")]
    private static partial Regex IdPattern();

    private sealed record Manifest(string RunId, IReadOnlyList<string> Requested);
}
