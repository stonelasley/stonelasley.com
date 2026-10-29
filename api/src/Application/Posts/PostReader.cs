using System.Text.Json;
using Microsoft.Extensions.Options;
using StoneLasley.Application.Common.Interfaces;
using StoneLasley.Domain.Posts;

namespace StoneLasley.Application.Posts;

public sealed record PostSummary(
    string Id, string Title, string Slug, string Area, string Date, IReadOnlyList<string> Tags, string Excerpt, string? Hero,
    WatchSummary? Watch, bool Draft);

/// <summary>Site:ShowDrafts is on for preview only, where Drafts are reviewed in place with a badge.</summary>
public sealed class SiteOptions
{
    public const string SectionName = "Site";

    public bool ShowDrafts { get; set; }
}

/// <summary>What the Watches index shows and filters on.</summary>
public sealed record WatchSummary(WatchMake Make, string Reference, string ModelName, IReadOnlyList<int> Decades)
{
    public static WatchSummary? Of(Watch? watch, int currentYear) =>
        watch is null ? null : new WatchSummary(watch.Make, watch.Reference, watch.ModelName, watch.Decades(currentYear));
}

public sealed record WatchFacets(IReadOnlyList<string> Makes, IReadOnlyList<string> ModelNames, IReadOnlyList<int> Decades);

public sealed record PostDetail(
    string Id, string Title, string Slug, string Area, string Date, IReadOnlyList<string> Tags, string Excerpt, string Body, Recipe? Recipe, Watch? Watch,
    bool AudiencePublic,
    bool Draft);

public sealed record AreaDetail(string Slug, string? Intro);

/// <summary>
/// Public reads: published Posts whose Audience is Public.
/// </summary>
public sealed class PostReader(ID1Database database, TimeProvider clock, IOptions<SiteOptions> site)
{
    // Published Posts, and Drafts too where the site shows them (preview).
    private string Visible => site.Value.ShowDrafts
        ? $"status IN ('{PostColumns.PublishedStatus}', '{PostColumns.DraftStatus}')"
        : $"status = '{PostColumns.PublishedStatus}'";

    private string PublicAndPublished => $"audience = '{PostColumns.PublicAudience}' AND {Visible}";

    public Task<IReadOnlyList<PostSummary>> ListAsync(ContentArea area, CancellationToken cancellationToken) =>
        SummariesAsync($"WHERE area = ? AND {PublicAndPublished} ORDER BY date DESC, id DESC", [area.Slug()], cancellationToken);

    /// <summary>A Client's Private Posts: the shared Clients library and their own, newest first. No Public filler.</summary>
    public Task<IReadOnlyList<PostSummary>> PrivateForClientAsync(string clientId, CancellationToken cancellationToken) =>
        SummariesAsync(
            $"WHERE {Visible} AND audience IN (?, ?) ORDER BY date DESC, id DESC",
            [Audience.Clients.Stored, Audience.ForClient(clientId).Stored],
            cancellationToken);

    /// <summary>The newest public, published Posts, across every area or within one. Never Drafts, even on preview.</summary>
    public Task<IReadOnlyList<PostSummary>> LatestAsync(ContentArea? area, int limit, CancellationToken cancellationToken)
    {
        const string publicPublished = $"audience = '{PostColumns.PublicAudience}' AND status = '{PostColumns.PublishedStatus}'";
        return area is { } only
            ? SummariesAsync($"WHERE area = ? AND {publicPublished} ORDER BY date DESC, id DESC LIMIT ?", [only.Slug(), limit], cancellationToken)
            : SummariesAsync($"WHERE {publicPublished} ORDER BY date DESC, id DESC LIMIT ?", [limit], cancellationToken);
    }

    private async Task<IReadOnlyList<PostSummary>> SummariesAsync(string where, IReadOnlyList<object?> parameters, CancellationToken cancellationToken)
    {
        var rows = await database.QueryAsync(
            $"SELECT id, title, slug, area, date, tags, excerpt, hero, watch, status FROM posts {where}",
            parameters,
            cancellationToken);
        return [.. rows.Select(row => new PostSummary(
            Text(row, "id"), Text(row, "title"), Text(row, "slug"), Text(row, "area"), Text(row, "date"), Tags(row), Text(row, "excerpt"), PostColumns.HeroFromRow(row),
            WatchSummary.Of(PostColumns.WatchFromRow(row), clock.GetUtcNow().Year),
            Text(row, "status") == PostColumns.DraftStatus))];
    }

    /// <summary>
    /// A published Post the reader may see: Public, or, for an unlocked Client, the Clients library and that
    /// Client's own Posts.
    /// </summary>
    public async Task<PostDetail?> FindAsync(ContentArea area, string slug, string? clientId, CancellationToken cancellationToken)
    {
        var rows = await database.QueryAsync(
            $"""
            SELECT id, title, slug, area, date, tags, excerpt, body, recipe, watch, audience, status FROM posts
            WHERE area = ? AND slug = ? AND {Visible}
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
            Text(row, "audience") == Audience.Public.Stored,
            Text(row, "status") == PostColumns.DraftStatus);
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
