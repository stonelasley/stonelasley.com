using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using StoneLasley.Application.Common.Interfaces;
using StoneLasley.Domain.Access;

namespace StoneLasley.Application.Access;

public sealed record IssuedCode(string ClientId, string Code);

public sealed record Exchanged(string Session, string ClientName);

/// <summary>A Client unlocked by a live grant.</summary>
public sealed record AccessSession(string ClientId, string ClientName);

/// <summary>
/// Issues, revokes and exchanges Access Codes. An exchange returns a signed session value naming the grant; every
/// protected read resolves it against the grant row again, so revocation takes effect on the next request.
/// </summary>
public sealed class AccessService(ID1Database database, IOptions<AccessOptions> options)
{
    private const string Now = "strftime('%Y-%m-%dT%H:%M:%fZ', 'now')";

    /// <summary>A new code for the Client, revoking any earlier one; null if there's no such Client.</summary>
    public async Task<IssuedCode?> IssueAsync(string clientId, CancellationToken cancellationToken)
    {
        if ((await database.QueryAsync("SELECT id FROM clients WHERE id = ?", [clientId], cancellationToken)).Count == 0)
        {
            return null;
        }
        var code = AccessCode.New();
        await database.BatchAsync(
        [
            new D1Statement($"UPDATE access_grants SET revoked_at = {Now} WHERE client_id = ? AND revoked_at IS NULL", [clientId]),
            new D1Statement(
                "INSERT INTO access_grants (id, client_id, code_hash) VALUES (?, ?, ?)",
                [Guid.CreateVersion7().ToString("N"), clientId, AccessCode.Hash(code)]),
        ], cancellationToken);
        return new IssuedCode(clientId, code);
    }

    public Task RevokeAsync(string clientId, CancellationToken cancellationToken) =>
        database.QueryAsync($"UPDATE access_grants SET revoked_at = {Now} WHERE client_id = ? AND revoked_at IS NULL", [clientId], cancellationToken);

    /// <summary>A session for a live code, or null for anything else (unknown, revoked, or a removed Client).</summary>
    public async Task<Exchanged?> ExchangeAsync(string code, CancellationToken cancellationToken)
    {
        var rows = await database.QueryAsync(
            """
            SELECT g.id, c.name FROM access_grants g JOIN clients c ON c.id = g.client_id
            WHERE g.code_hash = ? AND g.revoked_at IS NULL
            """,
            [AccessCode.Hash(code)],
            cancellationToken);
        return rows.Count == 0
            ? null
            : new Exchanged(Sign(rows[0].GetProperty("id").GetString()!), rows[0].GetProperty("name").GetString()!);
    }

    /// <summary>The Client a session value unlocks, checked against its grant now; null if forged or revoked.</summary>
    public async Task<AccessSession?> ResolveAsync(string? session, CancellationToken cancellationToken)
    {
        if (Verify(session) is not { } grantId)
        {
            return null;
        }
        var rows = await database.QueryAsync(
            """
            SELECT c.id, c.name FROM access_grants g JOIN clients c ON c.id = g.client_id
            WHERE g.id = ? AND g.revoked_at IS NULL
            """,
            [grantId],
            cancellationToken);
        return rows.Count == 0 ? null : new AccessSession(rows[0].GetProperty("id").GetString()!, rows[0].GetProperty("name").GetString()!);
    }

    // "{grantId}.{HMAC-SHA256(grantId)}", base64url.
    private string Sign(string grantId) => $"{grantId}.{Mac(grantId)}";

    private string? Verify(string? session)
    {
        var dot = session?.IndexOf('.') ?? -1;
        if (session is null || dot <= 0)
        {
            return null;
        }
        var grantId = session[..dot];
        var expected = Encoding.ASCII.GetBytes(Mac(grantId));
        var presented = Encoding.ASCII.GetBytes(session[(dot + 1)..]);
        return CryptographicOperations.FixedTimeEquals(expected, presented) ? grantId : null;
    }

    private string Mac(string grantId) =>
        Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.Value.SessionKey), Encoding.UTF8.GetBytes(grantId)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
