using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace StoneLasley.Api;

/// <summary>Rejects any request that doesn't carry the configured token as a bearer token.</summary>
public static class RequireBearerToken
{
    public static Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>> For<TOptions>(Func<TOptions, string> token)
        where TOptions : class =>
        async (context, next) =>
        {
            var expected = token(context.HttpContext.RequestServices.GetRequiredService<IOptions<TOptions>>().Value);
            var header = context.HttpContext.Request.Headers.Authorization.ToString();
            const string prefix = "Bearer ";
            if (!header.StartsWith(prefix, StringComparison.Ordinal) || !Matches(header[prefix.Length..], expected))
            {
                return Results.Unauthorized();
            }
            return await next(context);
        };

    // Compares digests so the check takes the same time whatever the input length.
    private static bool Matches(string presented, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(presented)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
}
