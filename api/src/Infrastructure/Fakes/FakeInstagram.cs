using StoneLasley.Application.Common.Interfaces;

namespace StoneLasley.Infrastructure.Fakes;

/// <summary>
/// Stands in for the Instagram API where there's no Creator account token (E2E, local dev): three fixture posts
/// with inline images.
/// </summary>
internal sealed class FakeInstagram : IInstagram
{
    private const string Pixel = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";

    public Task<InstagramToken> RefreshAsync(string token, CancellationToken cancellationToken) =>
        Task.FromResult(new InstagramToken(token, TimeSpan.FromDays(60)));

    public Task<IReadOnlyList<InstagramMedia>> MediaAsync(string token, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<InstagramMedia>>(
        [
            .. Enumerable.Range(1, 3).Select(i => new InstagramMedia(
                $"fixture-{i}", "IMAGE", Pixel, null, $"https://www.instagram.com/p/fixture{i}/", $"Fixture photo {i}", $"2026-10-0{i}T12:00:00Z")),
        ]);
}
