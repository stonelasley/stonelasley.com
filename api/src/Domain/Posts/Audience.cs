namespace StoneLasley.Domain.Posts;

/// <summary>
/// Who may read a Post: Public (anyone), Clients (every Client), or one Client by ULID. Anything but Public
/// is a Private Post.
/// </summary>
public sealed record Audience
{
    private const string ClientPrefix = "client:";

    private Audience(string stored) => Stored = stored;

    public static Audience Public { get; } = new("public");

    public static Audience Clients { get; } = new("clients");

    public static Audience ForClient(string clientId) => new(ClientPrefix + clientId);

    /// <summary>How the Audience is written to D1: <c>public</c>, <c>clients</c> or <c>client:{ULID}</c>.</summary>
    public string Stored { get; }

    public bool IsPublic => this == Public;

    /// <summary>The Client this Post belongs to, when it's for one Client.</summary>
    public string? ClientId => Stored.StartsWith(ClientPrefix, StringComparison.Ordinal) ? Stored[ClientPrefix.Length..] : null;

    /// <summary>Whether the Client with this ULID may read the Post.</summary>
    public bool Admits(string clientId) => this == Clients || ClientId == clientId;

    public static Audience FromStored(string stored) => stored switch
    {
        "public" => Public,
        "clients" => Clients,
        _ when stored.StartsWith(ClientPrefix, StringComparison.Ordinal) => new Audience(stored),
        _ => throw new ArgumentOutOfRangeException(nameof(stored), stored, "Not a stored Audience."),
    };

    public override string ToString() => Stored;
}
