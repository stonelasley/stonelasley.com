using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace StoneLasley.Api.Ingest;

/// <summary>
/// Rejects any request that doesn't carry the ingest token as a bearer token.
/// </summary>
internal sealed class RequireIngestToken(IOptions<IngestOptions> options) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var header = context.HttpContext.Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (!header.StartsWith(prefix, StringComparison.Ordinal) || !Matches(header[prefix.Length..], options.Value.Token))
        {
            return Results.Unauthorized();
        }
        return await next(context);
    }

    // Compares digests so the check takes the same time whatever the input length.
    private static bool Matches(string presented, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(presented)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
}
