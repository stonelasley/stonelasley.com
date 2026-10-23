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
    public static async Task<int> RunAsync(Uri apiUrl, string token, string directory, bool dryRun, bool includeDrafts)
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

        // The API deletes every Post missing from the manifest, so an empty one would wipe the site.
        if (posts.Count == 0)
        {
            await Console.Error.WriteLineAsync($"No Markdown Posts found under {directory}.");
            return 1;
        }

        using var client = new HttpClient { BaseAddress = apiUrl };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var manifestResponse = await client.PostAsJsonAsync(
            $"ingest/manifest?dryRun={dryRun}&includeDrafts={includeDrafts}",
            new { posts = posts.ToDictionary(p => p.Key, p => Hash(p.Value)) });
        if (!await SucceededAsync(manifestResponse, "manifest"))
        {
            return 1;
        }
        var manifest = (await manifestResponse.Content.ReadFromJsonAsync<Manifest>())!;

        // Upload everything before giving up, so one run reports every invalid Post.
        var failed = 0;
        foreach (var id in manifest.Requested)
        {
            var upload = await client.PutAsJsonAsync($"ingest/runs/{manifest.RunId}/posts/{id}", new { markdown = posts[id] });
            if (!await SucceededAsync(upload, $"upload {id}"))
            {
                failed++;
            }
        }
        if (failed > 0)
        {
            await Console.Error.WriteLineAsync($"{failed} Post(s) failed validation; nothing was committed.");
            return 1;
        }

        var commitResponse = await client.PostAsync($"ingest/runs/{manifest.RunId}/commit", null);
        if (!await SucceededAsync(commitResponse, "commit"))
        {
            return 1;
        }
        var commit = (await commitResponse.Content.ReadFromJsonAsync<Commit>())!;

        var verb = commit.DryRun ? "Would upsert" : "Upserted";
        Console.WriteLine($"{verb} {commit.Upserted.Count} and {(commit.DryRun ? "delete" : "deleted")} {commit.Deleted.Count} of {posts.Count} Posts ({posts.Count - manifest.Requested.Count} unchanged).");
        foreach (var id in commit.Deleted)
        {
            Console.WriteLine($"  - {id}");
        }
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

    private sealed record Commit(bool DryRun, IReadOnlyList<string> Upserted, IReadOnlyList<string> Deleted);
}
