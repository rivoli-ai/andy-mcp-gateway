using Andy.Mcp.Gateway.Models;

namespace Andy.Mcp.Gateway.Services;

/// <summary>
/// Read-only view of the gateway's currently-loaded service map. Backed by a
/// YAML file on disk; hot-reloads on file change (see
/// <see cref="ServiceMapFileLoader"/>).
/// </summary>
public interface IServiceMapRegistry
{
    /// <summary>All entries currently in the map. Snapshotted; safe to iterate.</summary>
    IReadOnlyList<ServiceMapEntry> Entries { get; }

    /// <summary>Look up a single entry by service id; returns null if absent.</summary>
    ServiceMapEntry? Find(string serviceId);

    /// <summary>
    /// Fired whenever the underlying file is reloaded. Subscribers (notably
    /// <see cref="IRouteHealthMonitor"/>) re-pick their probe targets.
    /// </summary>
    event EventHandler<ServiceMapChangedEventArgs>? Changed;
}

public sealed class ServiceMapChangedEventArgs(IReadOnlyList<ServiceMapEntry> entries) : EventArgs
{
    public IReadOnlyList<ServiceMapEntry> Entries { get; } = entries;
}
