using System.Globalization;
using System.Text.Json;
using StoneLasley.Application.Common.Interfaces;
using StoneLasley.Application.Posts;
using StoneLasley.Domain.Posts;

namespace StoneLasley.Application.Ingest;

public sealed record Manifest(string RunId, IReadOnlyList<string> Requested);

/// <summary>
/// The ingest protocol: a manifest of every Post's content hash, an upload of each Post the API asked for,
/// then a commit that applies the uploads in one statement.
/// </summary>
public sealed class IngestService(ID1Database database)
{
    public async Task<Manifest> StartAsync(IReadOnlyDictionary<string, string> hashes, CancellationToken cancellationToken)
    {
        var stored = (await database.QueryAsync("SELECT id, content_hash FROM posts", cancellationToken: cancellationToken))
            .ToDictionary(row => row.GetProperty("id").GetString()!, row => row.GetProperty("content_hash").GetString());

        var requested = hashes
            .Where(post => !stored.TryGetValue(post.Key, out var hash) || hash != post.Value)
            .Select(post => post.Key)
            .Order(StringComparer.Ordinal)
            .ToList();

        var runId = Guid.CreateVersion7().ToString("N");
        await database.QueryAsync(
            "INSERT INTO ingest_runs (id, hashes, requested) VALUES (?, ?, ?)",
            [runId, JsonSerializer.Serialize(hashes), JsonSerializer.Serialize(requested)],
            cancellationToken);

        return new Manifest(runId, requested);
    }

    public async Task<IngestOutcome> UploadAsync(string runId, string postId, string markdown, CancellationToken cancellationToken)
    {
        var run = await FindRunAsync(runId, cancellationToken);
        if (run is null)
        {
            return new IngestOutcome.RunNotFound();
        }
        if (run.Committed)
        {
            return new IngestOutcome.RunAlreadyCommitted();
        }
        if (!run.Hashes.TryGetValue(postId, out var hash))
        {
            return new IngestOutcome.Invalid([$"Post {postId} is not in this run's manifest."]);
        }

        var parsed = MarkdownPost.Parse(markdown);
        if (parsed.Post is not { } post)
        {
            return new IngestOutcome.Invalid(parsed.Errors);
        }
        if (post.Id != postId)
        {
            return new IngestOutcome.Invalid([$"The frontmatter id {post.Id} does not match the uploaded Post {postId}."]);
        }

        await database.QueryAsync(
            """
            INSERT OR REPLACE INTO ingest_staged_posts
              (run_id, id, area, slug, title, date, tags, audience, status, body, content_hash)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """,
            [
                runId,
                post.Id,
                post.Area.Slug(),
                post.Slug,
                post.Title,
                post.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                JsonSerializer.Serialize(post.Tags),
                PostColumns.Audience(post.Audience),
                PostColumns.Status(post.Status),
                post.Body,
                hash,
            ],
            cancellationToken);

        return new IngestOutcome.Done();
    }

    public async Task<IngestOutcome> CommitAsync(string runId, CancellationToken cancellationToken)
    {
        var run = await FindRunAsync(runId, cancellationToken);
        if (run is null)
        {
            return new IngestOutcome.RunNotFound();
        }
        if (run.Committed)
        {
            return new IngestOutcome.RunAlreadyCommitted();
        }

        var staged = (await database.QueryAsync(
                "SELECT id FROM ingest_staged_posts WHERE run_id = ?",
                [runId],
                cancellationToken))
            .Select(row => row.GetProperty("id").GetString())
            .ToHashSet();
        var missing = run.Requested.Where(id => !staged.Contains(id)).ToList();
        if (missing.Count > 0)
        {
            return new IngestOutcome.Invalid([.. missing.Select(id => $"Post {id} was requested but not uploaded.")]);
        }

        // One statement, so the staged Posts land together or not at all.
        await database.QueryAsync(
            """
            INSERT INTO posts (id, area, slug, title, date, tags, audience, status, body, content_hash, updated_at)
            SELECT id, area, slug, title, date, tags, audience, status, body, content_hash,
                   strftime('%Y-%m-%dT%H:%M:%fZ', 'now')
            FROM ingest_staged_posts
            WHERE run_id = ?
            ON CONFLICT (id) DO UPDATE SET
              area = excluded.area,
              slug = excluded.slug,
              title = excluded.title,
              date = excluded.date,
              tags = excluded.tags,
              audience = excluded.audience,
              status = excluded.status,
              body = excluded.body,
              content_hash = excluded.content_hash,
              updated_at = excluded.updated_at
            """,
            [runId],
            cancellationToken);
        await database.QueryAsync(
            "UPDATE ingest_runs SET committed_at = strftime('%Y-%m-%dT%H:%M:%fZ', 'now') WHERE id = ?",
            [runId],
            cancellationToken);
        await database.QueryAsync("DELETE FROM ingest_staged_posts WHERE run_id = ?", [runId], cancellationToken);

        return new IngestOutcome.Done();
    }

    private async Task<Run?> FindRunAsync(string runId, CancellationToken cancellationToken)
    {
        var rows = await database.QueryAsync(
            "SELECT hashes, requested, committed_at FROM ingest_runs WHERE id = ?",
            [runId],
            cancellationToken);
        if (rows.Count == 0)
        {
            return null;
        }

        var row = rows[0];
        return new Run(
            JsonSerializer.Deserialize<Dictionary<string, string>>(row.GetProperty("hashes").GetString()!)!,
            JsonSerializer.Deserialize<List<string>>(row.GetProperty("requested").GetString()!)!,
            row.GetProperty("committed_at").ValueKind != JsonValueKind.Null);
    }

    private sealed record Run(IReadOnlyDictionary<string, string> Hashes, IReadOnlyList<string> Requested, bool Committed);
}
