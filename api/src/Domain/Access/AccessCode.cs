using System.Security.Cryptography;
using System.Text;

namespace StoneLasley.Domain.Access;

/// <summary>
/// A secret Stone issues to a Client: 120 random bits, written as 24 Crockford base32 characters in groups of
/// four. Only its hash is stored. It's high-entropy, so a plain SHA-256 is enough (no slow KDF needed).
/// </summary>
public static class AccessCode
{
    private const string Crockford = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static string New()
    {
        var bytes = RandomNumberGenerator.GetBytes(15); // 120 bits
        var characters = new StringBuilder(24);
        var buffer = 0;
        var bits = 0;
        foreach (var b in bytes)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                characters.Append(Crockford[(buffer >> bits) & 31]);
            }
        }
        return string.Join('-', Enumerable.Range(0, 6).Select(i => characters.ToString(i * 4, 4)));
    }

    /// <summary>SHA-256 of the normalised code, so case, spacing, dashes and I/L/O lookalikes don't matter.</summary>
    public static string Hash(string code) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.ASCII.GetBytes(Normalise(code))));

    private static string Normalise(string code)
    {
        var normalised = new StringBuilder(code.Length);
        foreach (var c in code.ToUpperInvariant())
        {
            switch (c)
            {
                case '-' or ' ':
                    break;
                case 'I' or 'L':
                    normalised.Append('1');
                    break;
                case 'O':
                    normalised.Append('0');
                    break;
                default:
                    normalised.Append(char.IsAscii(c) ? c : '?');
                    break;
            }
        }
        return normalised.ToString();
    }
}
