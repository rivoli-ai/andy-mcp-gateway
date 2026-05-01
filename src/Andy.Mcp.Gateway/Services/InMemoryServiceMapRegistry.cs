using Andy.Mcp.Gateway.Models;

namespace Andy.Mcp.Gateway.Services;

/// <summary>
/// In-process holder of the service map. The file loader (see
/// <see cref="ServiceMapFileLoader"/>) calls <see cref="Replace"/> on each
/// successful load; <see cref="Changed"/> fires for every replacement.
///
/// Reads are lock-free (the field reference is swapped atomically). Writers
/// are serialised through the registry — only the loader writes.
/// </summary>
public sealed class InMemoryServiceMapRegistry : IServiceMapRegistry
{
    private IReadOnlyList<ServiceMapEntry> _entries = Array.Empty<ServiceMapEntry>();
    private Dictionary<string, ServiceMapEntry> _byId = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<ServiceMapEntry> Entries => _entries;

    public ServiceMapEntry? Find(string serviceId)
        => _byId.TryGetValue(serviceId, out var entry) ? entry : null;

    public event EventHandler<ServiceMapChangedEventArgs>? Changed;

    /// <summary>
    /// Atomically replace the entire map. Called by the file loader on
    /// startup and on every file-change reload.
    /// </summary>
    public void Replace(IReadOnlyList<ServiceMapEntry> entries)
    {
        var index = entries.ToDictionary(e => e.ServiceId, StringComparer.OrdinalIgnoreCase);
        _entries = entries;
        _byId = index;
        Changed?.Invoke(this, new ServiceMapChangedEventArgs(entries));
    }
}
