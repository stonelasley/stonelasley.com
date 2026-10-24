using StoneLasley.Application.Common.Interfaces;

namespace StoneLasley.Api.FunctionalTests;

/// <summary>Accepts any token except "bad", and records the IPs it was asked about.</summary>
public sealed class FakeTurnstile : ITurnstile
{
    public const string BadToken = "bad";

    public Task<bool> VerifyAsync(string token, string? remoteIp, CancellationToken cancellationToken) =>
        Task.FromResult(token != BadToken);
}
