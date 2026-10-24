using System.Text.Json;
using StoneLasley.Application.Common.Interfaces;

namespace StoneLasley.Application.Instagram;

/// <summary>A cached Instagram post as the site shows it: one image, linking to the post.</summary>
public sealed record FeedItem(string Id, string ImageUrl, string Permalink, string? Caption, string Timestamp);

/// <summary>
/// Keeps the long-lived token fresh and the media cached in D1's meta table. The API scales to zero and runs no
/// background jobs, so a GitHub Actions cron calls <see cref="RefreshAsync"/>.
/// </summary>
public sealed class InstagramFeed(ID1Database database, IInstagram instagram, ISiteRevalidator site)
{
    private const string TokenKey = "instagram_token";
    private const string MediaKey = "instagram_media";

    public Task SetTokenAsync(string token, CancellationToken cancellationToken) => PutAsync(TokenKey, token, cancellationToken);

    /// <summary>Renews the token, re-fetches media and revalidates the site. False when no token is stored yet.</summary>
    public async Task<bool> RefreshAsync(CancellationToken cancellationToken)
    {
        if (await GetAsync(TokenKey, cancellationToken) is not { } token)
        {
            return false;
        }
        var renewed = await instagram.RefreshAsync(token, cancellationToken);
        await PutAsync(TokenKey, renewed.AccessToken, cancellationToken);

        var items = (await instagram.MediaAsync(renewed.AccessToken, cancellationToken))
            .OrderByDescending(media => media.Timestamp, StringComparer.Ordinal)
            .Select(media => new FeedItem(
                media.Id, media.MediaType == "VIDEO" ? media.ThumbnailUrl ?? media.MediaUrl : media.MediaUrl, media.Permalink, media.Caption, media.Timestamp))
            .ToList();
        await PutAsync(MediaKey, JsonSerializer.Serialize(items), cancellationToken);
        await site.RevalidateAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<FeedItem>> MediaAsync(CancellationToken cancellationToken) =>
        await GetAsync(MediaKey, cancellationToken) is { } json ? JsonSerializer.Deserialize<List<FeedItem>>(json)! : [];

    private async Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var rows = await database.QueryAsync("SELECT value FROM meta WHERE key = ?", [key], cancellationToken);
        return rows.Count == 0 ? null : rows[0].GetProperty("value").GetString();
    }

    private Task PutAsync(string key, string value, CancellationToken cancellationToken) =>
        database.QueryAsync(
            """
            INSERT INTO meta (key, value) VALUES (?, ?)
            ON CONFLICT (key) DO UPDATE SET value = excluded.value, updated_at = strftime('%Y-%m-%dT%H:%M:%fZ', 'now')
            """,
            [key, value],
            cancellationToken);
}
