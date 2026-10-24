using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using StoneLasley.Application.Common.Interfaces;

namespace StoneLasley.Infrastructure.Turnstile;

public sealed class TurnstileOptions
{
    public const string SectionName = "Turnstile";

    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Accept any non-empty token (E2E, where the widget isn't rendered). Never in a deployed environment.</summary>
    public bool Fake { get; set; }
}

/// <summary>Verifies a Turnstile token with Cloudflare's siteverify.</summary>
internal sealed class CloudflareTurnstile(HttpClient http, IOptions<TurnstileOptions> options) : ITurnstile
{
    public async Task<bool> VerifyAsync(string token, string? remoteIp, CancellationToken cancellationToken)
    {
        if (options.Value.Fake)
        {
            return true;
        }
        var form = new Dictionary<string, string> { ["secret"] = options.Value.SecretKey, ["response"] = token };
        if (remoteIp is not null)
        {
            form["remoteip"] = remoteIp;
        }
        using var response = await http.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", new FormUrlEncodedContent(form), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }
        var result = await response.Content.ReadFromJsonAsync<SiteverifyResponse>(cancellationToken);
        return result?.Success == true;
    }

    private sealed record SiteverifyResponse(bool Success);
}
