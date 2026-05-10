using System.Security.Cryptography;
using System.Text;

namespace McpGateway.Application.Auth;

/// <summary>
/// Crypto helpers for API keys: secure plaintext generation, deterministic SHA-256 hex
/// digest for indexed lookup, and constant-time comparison. Centralised here so the
/// service, the auth handler, and tests share one implementation.
/// </summary>
public static class ApiKeyTokens
{
    /// <summary>Tokens look like <c>mcpg_&lt;43-char base64url&gt;</c>; the prefix lets users spot them.</summary>
    public const string Prefix = "mcpg_";

    /// <summary>Number of characters of the plaintext to surface alongside the masked display ("mcpg_abc1...").</summary>
    public const int DisplayPrefixLength = 9;

    /// <summary>Generates a fresh 32-byte random token, base64url-encoded and tagged with <see cref="Prefix"/>.</summary>
    public static string GeneratePlaintext()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Prefix + Base64UrlEncode(bytes);
    }

    /// <summary>Lowercase 64-char hex SHA-256 digest of the plaintext UTF-8 bytes.</summary>
    public static string ComputeHash(string plaintext)
    {
        ArgumentException.ThrowIfNullOrEmpty(plaintext);
        Span<byte> digest = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(plaintext), digest);
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    /// <summary>Safe display preview: first <see cref="DisplayPrefixLength"/> chars (incl. "mcpg_").</summary>
    public static string ComputePrefix(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return string.Empty;

        return plaintext.Length <= DisplayPrefixLength
            ? plaintext
            : plaintext[..DisplayPrefixLength];
    }

    /// <summary>Constant-time comparison of two hex digests (avoids timing oracles).</summary>
    public static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length)
            return false;

        var aBytes = Encoding.ASCII.GetBytes(a);
        var bBytes = Encoding.ASCII.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
