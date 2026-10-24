using StoneLasley.Application.Common.Interfaces;

namespace StoneLasley.Api.FunctionalTests;

/// <summary>Stands in for the Instagram API: refreshes return a new token; media is whatever the test sets.</summary>
public sealed class FakeInstagram : IInstagram
{
    public List<string> RefreshedTokens { get; } = [];

    public IReadOnlyList<InstagramMedia> Media { get; set; } =
    [
        new("1", "IMAGE", "https://cdn.test/1.jpg", null, "https://instagram.com/p/1", "Leg day", "2026-10-20T10:00:00Z"),
        new("2", "VIDEO", "https://cdn.test/2.mp4", "https://cdn.test/2.jpg", "https://instagram.com/p/2", null, "2026-10-21T10:00:00Z"),
    ];

    public Task<InstagramToken> RefreshAsync(string token, CancellationToken cancellationToken)
    {
        RefreshedTokens.Add(token);
        return Task.FromResult(new InstagramToken($"{token}-refreshed", TimeSpan.FromDays(60)));
    }

    public Task<IReadOnlyList<InstagramMedia>> MediaAsync(string token, CancellationToken cancellationToken) =>
        Task.FromResult(Media);
}
