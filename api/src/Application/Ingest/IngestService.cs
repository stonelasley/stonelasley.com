using System.Globalization;
using System.Text.Json;
using StoneLasley.Application.Common.Interfaces;
using StoneLasley.Application.Posts;
using StoneLasley.Domain.Posts;

namespace StoneLasley.Application.Ingest;

public sealed record Manifest(string RunId, IReadOnlyList<string> Requested);

/// <summary>
/// The ingest protocol: a manifest of every Post's content hash, an upload of each Post the API asked for,
/// then a commit that validates the whole set and applies it in one transaction. Posts missing from the
/// manifest are deleted, because the content repo is the source of truth (ADR 0002).
/// </summary>
public sealed class IngestService(ID1Database database)
{
    private const string Now = "strftime('%Y-%m-%dT%H:%M:%fZ', 'now')";

    public async Task<Manifest> StartAsync(
        IReadOnlyDictionary<string, string> hashes,
        bool dryRun,
        bool includeDrafts,
        CancellationToken cancellationToken)
    {
        var stored = (await database.QueryAsync("SELECT id, content_hash FROM posts", cancellationToken: cancellationToken))
            .ToDictionary(row => Text(row, "id"), row => Text(row, "content_hash"));

        var requested = hashes
            .Where(post => !stored.TryGetValue(post.Key, out var hash) || hash != post.Value)
            .Select(post => post.Key)
            .Order(StringComparer.Ordinal)
            .ToList();

        var runId = Guid.CreateVersion7().ToString("N");
        await database.QueryAsync(
            "INSERT INTO ingest_runs (id, hashes, requested, dry_run, include_drafts) VALUES (?, ?, ?, ?, ?)",
            [runId, JsonSerializer.Serialize(hashes), JsonSerializer.Serialize(requested), dryRun ? 1 : 0, includeDrafts ? 1 : 0],
            cancellationToken);

        return new Manifest(runId, requested);
    }

    public async Task<IngestOutcome> UploadAsync(string runId, string postId, string markdown, CancellationToken cancellationToken)
    {
        var (run, refusal) = await OpenRunAsync(runId, cancellationToken);
        if (run is null)
        {
            return refusal!;
        }
        if (!run.Requested.Contains(postId))
        {
            return new IngestOutcome.Invalid([$"Post {postId} was not requested by this run's manifest."]);
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
                run.Hashes[postId],
            ],
            cancellationToken);

        return new IngestOutcome.Uploaded();
    }

    public async Task<IngestOutcome> CommitAsync(string runId, CancellationToken cancellationToken)
    {
        var (run, refusal) = await OpenRunAsync(runId, cancellationToken);
        if (run is null)
        {
            return refusal!;
        }

        var staged = await AddressesAsync(
            "SELECT id, area, slug, status FROM ingest_staged_posts WHERE run_id = ?", [runId], cancellationToken);
        var stagedIds = staged.Select(post => post.Address.Id).ToHashSet();

        var missing = run.Requested.Where(id => !stagedIds.Contains(id)).ToList();
        if (missing.Count > 0)
        {
            return new IngestOutcome.Invalid([.. missing.Select(id => $"Post {id} was requested but not uploaded.")]);
        }

        var kept = staged.Where(post => run.IncludeDrafts || post.Status == PostColumns.PublishedStatus).ToList();
        var stored = await AddressesAsync("SELECT id, area, slug, status FROM posts", [], cancellationToken);
        var untouched = stored.Where(post => run.Hashes.ContainsKey(post.Address.Id) && !stagedIds.Contains(post.Address.Id));

        var conflicts = SlugConflicts.Find(untouched.Concat(kept).Select(post => post.Address));
        if (conflicts.Count > 0)
        {
            return new IngestOutcome.Invalid(conflicts);
        }

        // Gone from the manifest, or turned into a Draft while Drafts are excluded.
        var keptIds = kept.Select(post => post.Address.Id).ToHashSet();
        var deleted = stored
            .Select(post => post.Address.Id)
            .Where(id => !run.Hashes.ContainsKey(id) || (stagedIds.Contains(id) && !keptIds.Contains(id)))
            .Order(StringComparer.Ordinal)
            .ToList();
        var upserted = keptIds.Order(StringComparer.Ordinal).ToList();

        List<D1Statement> statements = [];
        if (!run.DryRun)
        {
            statements.Add(new D1Statement(
                "DELETE FROM posts WHERE id IN (SELECT value FROM json_each(?))", [JsonSerializer.Serialize(deleted)]));
            statements.Add(new D1Statement(
                $"""
                INSERT INTO posts (id, area, slug, title, date, tags, audience, status, body, content_hash, updated_at)
                SELECT id, area, slug, title, date, tags, audience, status, body, content_hash, {Now}
                FROM ingest_staged_posts
                WHERE run_id = ? AND id IN (SELECT value FROM json_each(?))
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
                [runId, JsonSerializer.Serialize(upserted)]));
        }
        statements.Add(new D1Statement($"UPDATE ingest_runs SET committed_at = {Now} WHERE id = ?", [runId]));
        statements.Add(new D1Statement("DELETE FROM ingest_staged_posts WHERE run_id = ?", [runId]));
        await database.BatchAsync(statements, cancellationToken);

        return new IngestOutcome.Committed(new CommitSummary(run.DryRun, upserted, deleted));
    }

    private async Task<(Run? Run, IngestOutcome? Refusal)> OpenRunAsync(string runId, CancellationToken cancellationToken)
    {
        var rows = await database.QueryAsync(
            "SELECT hashes, requested, dry_run, include_drafts, committed_at FROM ingest_runs WHERE id = ?",
            [runId],
            cancellationToken);
        if (rows.Count == 0)
        {
            return (null, new IngestOutcome.RunNotFound());
        }

        var row = rows[0];
        if (row.GetProperty("committed_at").ValueKind != JsonValueKind.Null)
        {
            return (null, new IngestOutcome.RunAlreadyCommitted());
        }

        var run = new Run(
            JsonSerializer.Deserialize<Dictionary<string, string>>(Text(row, "hashes"))!,
            [.. JsonSerializer.Deserialize<List<string>>(Text(row, "requested"))!],
            row.GetProperty("dry_run").GetInt32() == 1,
            row.GetProperty("include_drafts").GetInt32() == 1);
        return (run, null);
    }

    private async Task<List<StoredAddress>> AddressesAsync(string sql, IReadOnlyList<object?> parameters, CancellationToken cancellationToken)
    {
        var rows = await database.QueryAsync(sql, parameters, cancellationToken);
        return
        [
            .. rows.Select(row =>
            {
                ContentAreas.TryParse(Text(row, "area"), out var area);
                return new StoredAddress(new PostAddress(Text(row, "id"), area, Text(row, "slug")), Text(row, "status"));
            }),
        ];
    }

    private static string Text(JsonElement row, string column) => row.GetProperty(column).GetString()!;

    private sealed record Run(IReadOnlyDictionary<string, string> Hashes, HashSet<string> Requested, bool DryRun, bool IncludeDrafts);

    private sealed record StoredAddress(PostAddress Address, string Status);
}
