using System.Net.Http.Json;
using System.Text.Json.Serialization;
using StoneLasley.Application.Common.Interfaces;

namespace StoneLasley.Infrastructure.Instagram;

/// <summary>graph.instagram.com: token refresh and the account's media.</summary>
internal sealed class InstagramGraph(HttpClient http) : IInstagram
{
    public async Task<InstagramToken> RefreshAsync(string token, CancellationToken cancellationToken)
    {
        var response = await http.GetFromJsonAsync<RefreshResponse>(
            $"refresh_access_token?grant_type=ig_refresh_token&access_token={Uri.EscapeDataString(token)}", cancellationToken)
            ?? throw new InvalidOperationException("Instagram returned no token.");
        return new InstagramToken(response.AccessToken, TimeSpan.FromSeconds(response.ExpiresIn));
    }

    public async Task<IReadOnlyList<InstagramMedia>> MediaAsync(string token, CancellationToken cancellationToken)
    {
        var response = await http.GetFromJsonAsync<MediaPage>(
            $"me/media?fields=id,caption,media_type,media_url,thumbnail_url,permalink,timestamp&limit=12&access_token={Uri.EscapeDataString(token)}",
            cancellationToken);
        return [.. (response?.Data ?? []).Select(m => new InstagramMedia(m.Id, m.MediaType, m.MediaUrl, m.ThumbnailUrl, m.Permalink, m.Caption, m.Timestamp))];
    }

    private sealed record RefreshResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] long ExpiresIn);

    private sealed record MediaPage(IReadOnlyList<Media> Data);

    private sealed record Media(
        string Id,
        [property: JsonPropertyName("media_type")] string MediaType,
        [property: JsonPropertyName("media_url")] string MediaUrl,
        [property: JsonPropertyName("thumbnail_url")] string? ThumbnailUrl,
        string Permalink,
        string? Caption,
        string Timestamp);
}
