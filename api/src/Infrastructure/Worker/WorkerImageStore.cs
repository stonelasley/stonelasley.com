using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using StoneLasley.Application.Common.Interfaces;

namespace StoneLasley.Infrastructure.Worker;

/// <summary>
/// Stores images in R2 through the Worker's <c>/r2/objects/{key}</c> passthrough.
/// </summary>
internal sealed class WorkerImageStore(HttpClient http, IOptions<ImageOptions> options) : IImageStore
{
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        using var response = await http.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"r2/objects/{key}"), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task PutAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        using var body = new ByteArrayContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        using var response = await http.PutAsync($"r2/objects/{key}", body, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public Uri PublicUrl(string key) => new(options.Value.BaseUrl!, key);
}
