using System.Globalization;
using System.Text.Json;
using StoneLasley.Application.Common.Interfaces;
using StoneLasley.Application.Posts;
using StoneLasley.Domain.Posts;

namespace StoneLasley.Application.Ingest;

public sealed record Manifest(string RunId, IReadOnlyList<string> Requested);

/// <summary>An image uploaded with its Post, keyed by the path the Markdown references it by.</summary>
public sealed record UploadedImage(string Path, byte[] Content);

/// <summary>
/// The ingest protocol: a manifest of every Post's content hash, an upload of each Post the API asked for,
/// then a commit that validates the whole set and applies it in one transaction. Posts missing from the
/// manifest are deleted, because the content repo is the source of truth (ADR 0002).
/// </summary>
public sealed class IngestService(ID1Database database, IImageStore images, VectorIndexer vectors, ISiteRevalidator site)
{
    private const string Now = "strftime('%Y-%m-%dT%H:%M:%fZ', 'now')";

    public async Task<Manifest> StartAsync(
        IReadOnlyDictionary<string, string> hashes,
        IReadOnlyDictionary<ContentArea, string>? intros,
        bool dryRun,
        bool includeDrafts,
        CancellationToken cancellationToken)
    {
        // Staged uploads of runs that never committed are dead weight after a day.
        await database.QueryAsync(
            "DELETE FROM ingest_staged_posts WHERE run_id IN (SELECT id FROM ingest_runs WHERE committed_at IS NULL AND created_at < strftime('%Y-%m-%dT%H:%M:%fZ', 'now', '-1 day'))",
            cancellationToken: cancellationToken);

        // A public, published Post missing from the keyword or vector index counts as changed, so the next full ingest
        // indexes Posts stored before the index existed (or left out by a failure).
        var storedPosts = $"""
            SELECT id, content_hash FROM posts
            WHERE NOT (audience = '{PostColumns.PublicAudience}' AND status = '{PostColumns.PublishedStatus}')
               OR (id IN (SELECT post_id FROM posts_fts) AND vector_chunks IS NOT NULL)
            """;
        var known = includeDrafts
            ? storedPosts
            : $"{storedPosts} UNION ALL SELECT id, content_hash FROM skipped_drafts";
        var stored = (await database.QueryAsync(known, cancellationToken: cancellationToken))
            .ToLookup(row => Text(row, "id"), row => Text(row, "content_hash"));

        var requested = hashes
            .Where(post => !stored[post.Key].Contains(post.Value))
            .Select(post => post.Key)
            .Order(StringComparer.Ordinal)
            .ToList();

        var runId = Guid.CreateVersion7().ToString("N");
        await database.QueryAsync(
            "INSERT INTO ingest_runs (id, hashes, requested, intros, dry_run, include_drafts) VALUES (?, ?, ?, ?, ?, ?)",
            [
                runId,
                JsonSerializer.Serialize(hashes),
                JsonSerializer.Serialize(requested),
                intros is null ? null : JsonSerializer.Serialize(intros.ToDictionary(intro => intro.Key.Slug(), intro => intro.Value)),
                dryRun ? 1 : 0,
                includeDrafts ? 1 : 0,
            ],
            cancellationToken);

        return new Manifest(runId, requested);
    }

    public async Task<IngestOutcome> UploadAsync(
        string runId, string postId, string markdown, IReadOnlyList<UploadedImage> uploadedImages, CancellationToken cancellationToken)
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

        var stored = await StoreImagesAsync(post.Body, uploadedImages, cancellationToken);
        if (stored.Errors.Count > 0)
        {
            return new IngestOutcome.Invalid(stored.Errors);
        }
        post = post with { Body = PostImages.Rewrite(post.Body, stored.Urls) };

        await database.QueryAsync(
            """
            INSERT OR REPLACE INTO ingest_staged_posts
              (run_id, id, area, slug, title, date, tags, audience, status, body, excerpt, recipe, watch,
               search_structured, search_body, content_hash)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
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
                post.Excerpt,
                PostColumns.RecipeJson(post.Recipe),
                PostColumns.WatchJson(post.Watch),
                SearchText.Structured(post),
                SearchText.Body(post.Body),
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

        bool Keeps(StoredAddress post) => run.IncludeDrafts || post.Status == PostColumns.PublishedStatus;
        var kept = staged.Where(Keeps).ToList();
        var stored = await AddressesAsync("SELECT id, area, slug, status FROM posts", [], cancellationToken);
        var untouched = stored
            .Where(post => run.Hashes.ContainsKey(post.Address.Id) && !stagedIds.Contains(post.Address.Id) && Keeps(post))
            .ToList();

        var conflicts = SlugConflicts.Find(untouched.Concat(kept).Select(post => post.Address));
        if (conflicts.Count > 0)
        {
            return new IngestOutcome.Invalid(conflicts);
        }

        // Whatever survives is either untouched or freshly staged; everything else stored goes, including
        // Posts gone from the manifest and Drafts while Drafts are excluded.
        var keptIds = kept.Concat(untouched).Select(post => post.Address.Id).ToHashSet();
        var deleted = stored
            .Select(post => post.Address.Id)
            .Where(id => !keptIds.Contains(id))
            .Order(StringComparer.Ordinal)
            .ToList();
        var upserted = kept.Select(post => post.Address.Id).Order(StringComparer.Ordinal).ToList();

        // Every manifest Post that isn't kept is a Draft left out by this run. Remember it by hash, so the next
        // manifest doesn't ask for it again.
        var skippedDrafts = run.Hashes
            .Where(post => !keptIds.Contains(post.Key))
            .ToDictionary(post => post.Key, post => post.Value);

        List<D1Statement> statements = [];
        var previousChunks = new Dictionary<string, int>();
        var chunks = new List<PostChunks>();
        if (!run.DryRun)
        {
            // Vectors of every rewritten or removed Post go, by id, once the commit lands.
            previousChunks = (await database.QueryAsync(
                    "SELECT id, vector_chunks FROM posts WHERE vector_chunks IS NOT NULL AND id IN (SELECT value FROM json_each(?))",
                    [JsonSerializer.Serialize(deleted.Concat(upserted))],
                    cancellationToken))
                .ToDictionary(row => Text(row, "id"), row => row.GetProperty("vector_chunks").GetInt32());
            chunks = [.. (await database.QueryAsync(
                    $"""
                    SELECT id, area, title, body FROM ingest_staged_posts
                    WHERE run_id = ? AND id IN (SELECT value FROM json_each(?))
                      AND status = '{PostColumns.PublishedStatus}' AND audience = '{PostColumns.PublicAudience}'
                    """,
                    [runId, JsonSerializer.Serialize(upserted)],
                    cancellationToken))
                .Select(row => VectorIndexer.Chunk(Text(row, "id"), Text(row, "area"), Text(row, "title"), Text(row, "body")))];

            statements.Add(new D1Statement(
                "DELETE FROM posts WHERE id IN (SELECT value FROM json_each(?))", [JsonSerializer.Serialize(deleted)]));
            // Park the slugs of Posts being rewritten, so two Posts can swap slugs without tripping UNIQUE (area, slug)
            // part-way through the upsert. Validation has already proved the final set is conflict-free.
            statements.Add(new D1Statement(
                "UPDATE posts SET slug = '~' || id WHERE id IN (SELECT value FROM json_each(?))", [JsonSerializer.Serialize(upserted)]));
            statements.Add(new D1Statement(
                $"""
                INSERT INTO posts (id, area, slug, title, date, tags, audience, status, body, excerpt, recipe, watch, content_hash, updated_at)
                SELECT id, area, slug, title, date, tags, audience, status, body, excerpt, recipe, watch, content_hash, {Now}
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
                  excerpt = excluded.excerpt,
                  recipe = excluded.recipe,
                  watch = excluded.watch,
                  content_hash = excluded.content_hash,
                  updated_at = excluded.updated_at,
                  vector_chunks = NULL
                """,
                [runId, JsonSerializer.Serialize(upserted)]));
            statements.Add(new D1Statement(
                "DELETE FROM post_chunks WHERE post_id IN (SELECT value FROM json_each(?))",
                [JsonSerializer.Serialize(deleted.Concat(upserted))]));
            // One statement per Post keeps each bound value about the size of one body, well under D1's limit.
            statements.AddRange(chunks.Select(post => new D1Statement(
                """
                INSERT INTO post_chunks (post_id, idx, text)
                SELECT ?, key, value FROM json_each(?)
                """,
                [post.PostId, JsonSerializer.Serialize(post.Chunks)])));
            // The keyword index follows the Posts table in the same transaction: public, published Posts only.
            statements.Add(new D1Statement(
                "DELETE FROM posts_fts WHERE post_id IN (SELECT value FROM json_each(?))",
                [JsonSerializer.Serialize(deleted.Concat(upserted))]));
            statements.Add(new D1Statement(
                $"""
                INSERT INTO posts_fts (post_id, title, structured, tags, body)
                SELECT id, title, search_structured, (SELECT group_concat(value, ' ') FROM json_each(tags)), search_body
                FROM ingest_staged_posts
                WHERE run_id = ? AND id IN (SELECT value FROM json_each(?))
                  AND status = '{PostColumns.PublishedStatus}' AND audience = '{PostColumns.PublicAudience}'
                """,
                [runId, JsonSerializer.Serialize(upserted)]));
            statements.Add(new D1Statement(
                "DELETE FROM skipped_drafts WHERE id NOT IN (SELECT key FROM json_each(?))", [JsonSerializer.Serialize(skippedDrafts)]));
            statements.Add(new D1Statement(
                """
                INSERT INTO skipped_drafts (id, content_hash)
                SELECT key, value FROM json_each(?) WHERE true
                ON CONFLICT (id) DO UPDATE SET content_hash = excluded.content_hash
                """,
                [JsonSerializer.Serialize(skippedDrafts)]));
            // A manifest without intros leaves them as they are; one with intros replaces them all.
            if (run.Intros is not null)
            {
                statements.Add(new D1Statement("DELETE FROM area_intros", []));
                statements.Add(new D1Statement(
                    "INSERT INTO area_intros (area, intro) SELECT key, value FROM json_each(?)", [run.Intros]));
            }
        }
        statements.Add(new D1Statement($"UPDATE ingest_runs SET committed_at = {Now} WHERE id = ?", [runId]));
        statements.Add(new D1Statement("DELETE FROM ingest_staged_posts WHERE run_id = ?", [runId]));
        await database.BatchAsync(statements, cancellationToken);
        if (!run.DryRun)
        {
            await vectors.ApplyAsync(previousChunks, chunks, cancellationToken);
        }

        if (!run.DryRun && (upserted.Count > 0 || deleted.Count > 0 || run.Intros is not null))
        {
            await site.RevalidateAsync(cancellationToken);
        }

        return new IngestOutcome.Committed(new CommitSummary(run.DryRun, upserted, deleted));
    }

    /// <summary>
    /// Every image the body references must be uploaded with it. Each goes to the image store under its content
    /// hash, skipped when already there, so re-ingesting an unchanged image costs one existence check.
    /// Images go up before commit; an abandoned run leaves only harmless, content-addressed objects.
    /// </summary>
    private async Task<(IReadOnlyDictionary<string, string> Urls, IReadOnlyList<string> Errors)> StoreImagesAsync(
        string body, IReadOnlyList<UploadedImage> uploadedImages, CancellationToken cancellationToken)
    {
        // Two spellings of one path ("./a.png", "a.png") are one image.
        var byPath = uploadedImages
            .Select(image => (Path: PostImages.Normalise(image.Path), image.Content))
            .Where(image => image.Path is not null)
            .GroupBy(image => image.Path!)
            .ToDictionary(group => group.Key, group => group.First().Content);
        var references = PostImages.References(body);

        var errors = new List<string>();
        foreach (var label in PostImages.ReferenceStyle(body))
        {
            errors.Add($"Image [{label}] is reference-style; write it inline as ![alt](path) so ingest can store it.");
        }
        foreach (var path in references)
        {
            if (PostImages.Normalise(path) is null)
            {
                errors.Add($"Image '{path}' must be inside the Post's folder.");
            }
            else if (!byPath.TryGetValue(path, out var content))
            {
                errors.Add($"Image '{path}' is referenced but was not uploaded.");
            }
            else if (PostImages.ContentType(path) is null)
            {
                errors.Add($"'{path}' is not a supported image type (jpg, png, gif, webp, avif).");
            }
            else if (content.Length > PostImages.MaxBytes)
            {
                errors.Add($"Image '{path}' is over the {PostImages.MaxBytes / (1024 * 1024)} MB limit.");
            }
        }
        if (errors.Count > 0)
        {
            return (new Dictionary<string, string>(), errors);
        }

        var urls = new Dictionary<string, string>();
        foreach (var path in references)
        {
            var content = byPath[path];
            var key = PostImages.Key(path, content);
            if (!await images.ExistsAsync(key, cancellationToken))
            {
                await images.PutAsync(key, content, PostImages.ContentType(path)!, cancellationToken);
            }
            urls[path] = images.PublicUrl(key).ToString();
        }
        return (urls, []);
    }

    private async Task<(Run? Run, IngestOutcome? Refusal)> OpenRunAsync(string runId, CancellationToken cancellationToken)
    {
        var rows = await database.QueryAsync(
            "SELECT hashes, requested, intros, dry_run, include_drafts, committed_at FROM ingest_runs WHERE id = ?",
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
            JsonSerializer.Deserialize<List<string>>(Text(row, "requested"))!.ToHashSet(),
            row.GetProperty("intros").GetString(),
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

    private sealed record Run(
        IReadOnlyDictionary<string, string> Hashes, IReadOnlySet<string> Requested, string? Intros, bool DryRun, bool IncludeDrafts);

    private sealed record StoredAddress(PostAddress Address, string Status);
}
