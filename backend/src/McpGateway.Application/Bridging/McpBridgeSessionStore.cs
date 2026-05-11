using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace McpGateway.Application.Bridging;

/// <summary>Entry tracking a single live bridge session.</summary>
public sealed class BridgeSessionEntry
{
    public required string SessionId { get; init; }
    public required string AdapterName { get; init; }
    public required IMcpBridgeSession Session { get; init; }
    public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Process-local registry of active bridge sessions. Keyed by the gateway-issued
/// session id that we send back to clients (either via the <c>Mcp-Session-Id</c>
/// header for streamable HTTP clients, or via the messages URL query string for SSE
/// clients). Entries are evicted after <see cref="IdleTimeout"/> of inactivity.
/// </summary>
public sealed class McpBridgeSessionStore : IAsyncDisposable
{
    public TimeSpan IdleTimeout { get; init; } = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<string, BridgeSessionEntry> _entries = new(StringComparer.Ordinal);

    public string CreateId()
    {
        Span<byte> bytes = stackalloc byte[18];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public void Register(BridgeSessionEntry entry) => _entries[entry.SessionId] = entry;

    public BridgeSessionEntry? Touch(string sessionId)
    {
        if (!_entries.TryGetValue(sessionId, out var entry))
            return null;
        entry.LastActivityUtc = DateTime.UtcNow;
        return entry;
    }

    public bool TryGet(string sessionId, out BridgeSessionEntry? entry) =>
        _entries.TryGetValue(sessionId, out entry);

    public async Task<bool> RemoveAsync(string sessionId)
    {
        if (!_entries.TryRemove(sessionId, out var entry))
            return false;
        await entry.Session.DisposeAsync().ConfigureAwait(false);
        return true;
    }

    /// <summary>Drop sessions that haven't seen traffic for <see cref="IdleTimeout"/>.</summary>
    public async Task SweepIdleAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow - IdleTimeout;
        foreach (var (id, entry) in _entries)
        {
            if (entry.LastActivityUtc > cutoff) continue;
            if (_entries.TryRemove(id, out var removed))
            {
                try { await removed.Session.DisposeAsync().ConfigureAwait(false); }
                catch { /* swallow */ }
            }
            if (cancellationToken.IsCancellationRequested) break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var entry in _entries.Values)
        {
            try { await entry.Session.DisposeAsync().ConfigureAwait(false); }
            catch { /* swallow */ }
        }
        _entries.Clear();
    }
}
