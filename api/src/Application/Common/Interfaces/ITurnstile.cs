namespace StoneLasley.Application.Common.Interfaces;

/// <summary>Cloudflare Turnstile's siteverify: was this token solved by a person, just now?</summary>
public interface ITurnstile
{
    Task<bool> VerifyAsync(string token, string? remoteIp, CancellationToken cancellationToken);
}
