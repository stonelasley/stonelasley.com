using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using StoneLasley.Domain.Posts;

namespace StoneLasley.Cli;

/// <summary>
/// Sends every Markdown Post under a directory through the API's manifest → upload → commit protocol.
/// The API parses and validates; this only finds each Post's id and hashes its file.
/// <c>_areas/{area}.md</c> holds a Content Area's intro; nothing under a <c>_</c> folder is a Post.
/// </summary>
internal static partial class IngestCommand
{
    public static async Task<int> RunAsync(Uri apiUrl, string token, string directory, bool dryRun, bool includeDrafts)
    {
        var posts = new Dictionary<string, string>();
        var postFiles = new Dictionary<string, string>();
        var markdownFiles = Directory.EnumerateFiles(directory, "*.md", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(directory, path).Split(Path.DirectorySeparatorChar).Any(part => part.StartsWith('_')))
            .Order(StringComparer.Ordinal);
        foreach (var path in markdownFiles)
        {
            var markdown = await File.ReadAllTextAsync(path);
            var id = IdPattern().Match(markdown);
            if (!id.Success)
            {
                await Console.Error.WriteLineAsync($"{path}: no 'id' in the frontmatter.");
                return 1;
            }
            postFiles[id.Groups["id"].Value] = path;
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

        var introDirectory = Path.Combine(directory, "_areas");
        var intros = Directory.Exists(introDirectory)
            ? Directory.EnumerateFiles(introDirectory, "*.md").ToDictionary(path => Path.GetFileNameWithoutExtension(path), File.ReadAllText)
            : [];

        using var client = new HttpClient { BaseAddress = apiUrl };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var manifestResponse = await client.PostAsJsonAsync(
            $"ingest/manifest?dryRun={dryRun}&includeDrafts={includeDrafts}",
            new { posts = posts.ToDictionary(p => p.Key, p => Hash(p.Value, ImagesOf(postFiles[p.Key], p.Value))), intros });
        if (!await SucceededAsync(manifestResponse, "manifest"))
        {
            return 1;
        }
        var manifest = (await manifestResponse.Content.ReadFromJsonAsync<Manifest>())!;

        // Upload everything before giving up, so one run reports every invalid Post.
        var failed = 0;
        foreach (var id in manifest.Requested)
        {
            var images = ImagesOf(postFiles[id], posts[id]).Select(i => new { path = i.Path, content = Convert.ToBase64String(i.Content) });
            var upload = await client.PutAsJsonAsync($"ingest/runs/{manifest.RunId}/posts/{id}", new { markdown = posts[id], images });
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

    // The Post's hash covers its images, so changing only an image still re-ingests the Post.
    private static string Hash(string markdown, IEnumerable<(string Path, byte[] Content)> images)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(System.Text.Encoding.UTF8.GetBytes(markdown));
        foreach (var (path, content) in images.OrderBy(i => i.Path, StringComparer.Ordinal))
        {
            hash.AppendData(System.Text.Encoding.UTF8.GetBytes(path));
            hash.AppendData(SHA256.HashData(content));
        }
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    // Images next to the Post that its Markdown references. A missing file is left for the API to report.
    private static IEnumerable<(string Path, byte[] Content)> ImagesOf(string postFile, string markdown)
    {
        var directory = Path.GetDirectoryName(postFile)!;
        foreach (var reference in PostImages.References(markdown))
        {
            var file = Path.Combine(directory, reference);
            if (File.Exists(file))
            {
                yield return (reference, File.ReadAllBytes(file));
            }
        }
    }

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
