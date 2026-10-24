using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StoneLasley.Application.Common.Interfaces;

namespace StoneLasley.Infrastructure.Site;

/// <summary>
/// Calls the site's <c>/api/revalidate</c> route. A failure is logged, not thrown: the ingest has already
/// committed, and the next successful ingest revalidates again.
/// </summary>
internal sealed partial class WebhookSiteRevalidator(
    HttpClient http,
    IOptions<RevalidationOptions> options,
    ILogger<WebhookSiteRevalidator> logger) : ISiteRevalidator
{
    public async Task RevalidateAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (settings.Url is null)
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, settings.Url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.Secret);
        if (!string.IsNullOrEmpty(settings.ProtectionBypass))
        {
            request.Headers.Add("x-vercel-protection-bypass", settings.ProtectionBypass);
        }

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogFailed(logger, settings.Url, (int)response.StatusCode);
            }
        }
        catch (HttpRequestException exception)
        {
            LogUnreachable(logger, settings.Url, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Revalidating {Url} answered {StatusCode}.")]
    private static partial void LogFailed(ILogger logger, Uri url, int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Revalidating {Url} failed.")]
    private static partial void LogUnreachable(ILogger logger, Uri url, Exception exception);
}
