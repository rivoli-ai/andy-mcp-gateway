using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace McpGateway.Authentication.Internal;

/// <summary>
/// Process-scoped, TTL'd store for OAuth 2.0 authorization requests that are in flight
/// through the gateway's authorize → callback proxy.
/// </summary>
/// <remarks>
/// The MCP client (Cline, Cursor, etc.) sends its own dynamic <c>redirect_uri</c> when
/// initiating the authorization request. Entra ID, however, only accepts the single
/// <c>redirect_uri</c> registered against the app registration. To bridge the two, the
/// gateway substitutes its own callback URL when forwarding the request to Entra and
/// uses an opaque <c>state</c> token to remember the original client redirect target.
/// This store holds those pending mappings until the user completes the consent flow
/// (or the entry expires).
/// </remarks>
internal sealed class PendingMcpAuthorizationStore
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, PendingMcpAuthorization> _entries = new();
    private readonly TimeSpan _lifetime;

    public PendingMcpAuthorizationStore(TimeSpan? lifetime = null)
    {
        _lifetime = lifetime ?? DefaultLifetime;
    }

    /// <summary>
    /// Persists the original client <paramref name="clientRedirectUri"/> and
    /// <paramref name="clientState"/>, returning an opaque gateway-issued state token
    /// that should be sent to Entra in their place.
    /// </summary>
    public string Issue(string clientRedirectUri, string? clientState)
    {
        if (string.IsNullOrWhiteSpace(clientRedirectUri))
            throw new ArgumentException("Client redirect URI must be supplied.", nameof(clientRedirectUri));

        PruneExpired();

        var gatewayState = GenerateOpaqueToken();
        _entries[gatewayState] = new PendingMcpAuthorization(
            clientRedirectUri,
            string.IsNullOrEmpty(clientState) ? null : clientState,
            DateTimeOffset.UtcNow.Add(_lifetime));
        return gatewayState;
    }

    /// <summary>
    /// Atomically removes and returns the pending authorization keyed by
    /// <paramref name="gatewayState"/>. Returns <c>null</c> if the state is unknown
    /// or has expired (expired entries are removed as a side effect).
    /// </summary>
    public PendingMcpAuthorization? Redeem(string? gatewayState)
    {
        if (string.IsNullOrWhiteSpace(gatewayState))
            return null;

        if (!_entries.TryRemove(gatewayState, out var entry))
            return null;

        if (entry.ExpiresAt < DateTimeOffset.UtcNow)
            return null;

        return entry;
    }

    private void PruneExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kv in _entries)
        {
            if (kv.Value.ExpiresAt < now)
                _entries.TryRemove(kv.Key, out _);
        }
    }

    private static string GenerateOpaqueToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return WebEncoders.Base64UrlEncode(bytes);
    }
}

internal sealed record PendingMcpAuthorization(
    string ClientRedirectUri,
    string? ClientState,
    DateTimeOffset ExpiresAt);
