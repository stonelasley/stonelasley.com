namespace StoneLasley.Application.Common.Interfaces;

public sealed record InstagramToken(string AccessToken, TimeSpan ExpiresIn);

/// <summary>One post from the Instagram API. Videos carry a thumbnail.</summary>
public sealed record InstagramMedia(
    string Id, string MediaType, string MediaUrl, string? ThumbnailUrl, string Permalink, string? Caption, string Timestamp);

/// <summary>The Instagram API with Instagram Login, for Stone's Creator account.</summary>
public interface IInstagram
{
    /// <summary>Renews a 60-day long-lived token before it expires.</summary>
    Task<InstagramToken> RefreshAsync(string token, CancellationToken cancellationToken);

    Task<IReadOnlyList<InstagramMedia>> MediaAsync(string token, CancellationToken cancellationToken);
}
