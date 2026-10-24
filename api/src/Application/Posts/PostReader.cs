using System.Text.Json;
using StoneLasley.Application.Common.Interfaces;
using StoneLasley.Domain.Posts;

namespace StoneLasley.Application.Posts;

public sealed record PostSummary(
    string Id, string Title, string Slug, string Area, string Date, IReadOnlyList<string> Tags, string Excerpt, WatchSummary? Watch);

/// <summary>What the Watches index shows and filters on.</summary>
public sealed record WatchSummary(WatchMake Make, string Reference, string ModelName, IReadOnlyList<int> Decades)
{
    public static WatchSummary? Of(Watch? watch, int currentYear) =>
        watch is null ? null : new WatchSummary(watch.Make, watch.Reference, watch.ModelName, watch.Decades(currentYear));
}

public sealed record WatchFacets(IReadOnlyList<string> Makes, IReadOnlyList<string> ModelNames, IReadOnlyList<int> Decades);

public sealed record PostDetail(
    string Id, string Title, string Slug, string Area, string Date, IReadOnlyList<string> Tags, string Excerpt, string Body, Recipe? Recipe, Watch? Watch,
    bool AudiencePublic);

public sealed record AreaDetail(string Slug, string? Intro);

/// <summary>
/// Public reads: published Posts whose Audience is Public.
/// </summary>
public sealed class PostReader(ID1Database database, TimeProvider clock)
{
    private const string PublicAndPublished = $"audience = '{PostColumns.PublicAudience}' AND status = '{PostColumns.PublishedStatus}'";

    public Task<IReadOnlyList<PostSummary>> ListAsync(ContentArea area, CancellationToken cancellationToken) =>
        SummariesAsync($"WHERE area = ? AND {PublicAndPublished} ORDER BY date DESC, id DESC", [area.Slug()], cancellationToken);

    /// <summary>A Client's Private Posts: the shared Clients library and their own, newest first. No Public filler.</summary>
    public Task<IReadOnlyList<PostSummary>> PrivateForClientAsync(string clientId, CancellationToken cancellationToken) =>
        SummariesAsync(
            $"WHERE status = '{PostColumns.PublishedStatus}' AND audience IN (?, ?) ORDER BY date DESC, id DESC",
            [Audience.Clients.Stored, Audience.ForClient(clientId).Stored],
            cancellationToken);

    /// <summary>The newest public, published Posts, across every area or within one.</summary>
    public Task<IReadOnlyList<PostSummary>> LatestAsync(ContentArea? area, int limit, CancellationToken cancellationToken) =>
        area is { } only
            ? SummariesAsync($"WHERE area = ? AND {PublicAndPublished} ORDER BY date DESC, id DESC LIMIT ?", [only.Slug(), limit], cancellationToken)
            : SummariesAsync($"WHERE {PublicAndPublished} ORDER BY date DESC, id DESC LIMIT ?", [limit], cancellationToken);

    private async Task<IReadOnlyList<PostSummary>> SummariesAsync(string where, IReadOnlyList<object?> parameters, CancellationToken cancellationToken)
    {
        var rows = await database.QueryAsync(
            $"SELECT id, title, slug, area, date, tags, excerpt, watch FROM posts {where}",
            parameters,
            cancellationToken);
        return [.. rows.Select(row => new PostSummary(
            Text(row, "id"), Text(row, "title"), Text(row, "slug"), Text(row, "area"), Text(row, "date"), Tags(row), Text(row, "excerpt"), WatchSummary.Of(PostColumns.WatchFromRow(row), clock.GetUtcNow().Year)))];
    }

    /// <summary>
    /// A published Post the reader may see: Public, or, for an unlocked Client, the Clients library and that
    /// Client's own Posts.
    /// </summary>
    public async Task<PostDetail?> FindAsync(ContentArea area, string slug, string? clientId, CancellationToken cancellationToken)
    {
        var rows = await database.QueryAsync(
            $"""
            SELECT id, title, slug, area, date, tags, excerpt, body, recipe, watch, audience FROM posts
            WHERE area = ? AND slug = ? AND status = '{PostColumns.PublishedStatus}'
              AND audience IN (?, ?, ?)
            """,
            [
                area.Slug(),
                slug,
                Audience.Public.Stored,
                clientId is null ? Audience.Public.Stored : Audience.Clients.Stored,
                clientId is null ? Audience.Public.Stored : Audience.ForClient(clientId).Stored,
            ],
            cancellationToken);
        if (rows.Count == 0)
        {
            return null;
        }

        var row = rows[0];
        return new PostDetail(
            Text(row, "id"), Text(row, "title"), Text(row, "slug"), Text(row, "area"), Text(row, "date"), Tags(row), Text(row, "excerpt"), Text(row, "body"), PostColumns.RecipeFromRow(row), PostColumns.WatchFromRow(row),
            Text(row, "audience") == Audience.Public.Stored);
    }

    /// <summary>The Watches index filters: every make, model name and production decade in use.</summary>
    public async Task<WatchFacets> WatchFacetsAsync(CancellationToken cancellationToken)
    {
        var watches = (await ListAsync(ContentArea.Watches, cancellationToken)).Select(post => post.Watch).OfType<WatchSummary>().ToList();
        return new WatchFacets(
            [.. watches.Select(w => w.Make.Name()).Distinct().Order(StringComparer.Ordinal)],
            [.. watches.Select(w => w.ModelName).Distinct().Order(StringComparer.Ordinal)],
            [.. watches.SelectMany(w => w.Decades).Distinct().Order()]);
    }

    public async Task<AreaDetail> AreaAsync(ContentArea area, CancellationToken cancellationToken)
    {
        var rows = await database.QueryAsync("SELECT intro FROM area_intros WHERE area = ?", [area.Slug()], cancellationToken);
        return new AreaDetail(area.Slug(), rows.Count == 0 ? null : Text(rows[0], "intro"));
    }

    private static string Text(JsonElement row, string column) => row.GetProperty(column).GetString()!;

    private static List<string> Tags(JsonElement row) => JsonSerializer.Deserialize<List<string>>(Text(row, "tags"))!;
}
