using Andy.Mcp.Gateway.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Andy.Mcp.Gateway.Services;

/// <summary>
/// Loads the service map from a YAML file at startup and watches for changes.
/// On change, reloads the file; on parse error, logs and keeps the previous
/// snapshot (better to keep stale routing than to fail closed with no map).
///
/// File format (see <c>servicemap.yaml</c> shipped with the binary):
/// <code>
/// services:
///   - serviceId: andy-containers
///     localUrl: https://localhost:7200
///     remoteUrlPattern: https://containers.{tenantSlug}.rivoli.ai
///     requiresAuth: true
///   - serviceId: andy-agents
///     localUrl: null
///     remoteUrlPattern: https://agents.{tenantSlug}.rivoli.ai
///     requiresAuth: true
/// </code>
/// </summary>
public sealed class ServiceMapFileLoader : IHostedService, IDisposable
{
    private readonly InMemoryServiceMapRegistry _registry;
    private readonly ILogger<ServiceMapFileLoader> _logger;
    private readonly string _filePath;
    private FileSystemWatcher? _watcher;
    // Debounce noisy editors (vim writes via temp file → rename → multiple events).
    private DateTimeOffset _lastReload = DateTimeOffset.MinValue;
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromMilliseconds(250);

    public ServiceMapFileLoader(
        InMemoryServiceMapRegistry registry,
        IConfiguration config,
        ILogger<ServiceMapFileLoader> logger)
    {
        _registry = registry;
        _logger = logger;
        _filePath = ResolveFilePath(config);
    }

    private static string ResolveFilePath(IConfiguration config)
    {
        var configured = config["ServiceMap:File"];
        if (!string.IsNullOrEmpty(configured))
        {
            return Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(AppContext.BaseDirectory, configured);
        }
        return Path.Combine(AppContext.BaseDirectory, "servicemap.yaml");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Reload();

        var dir = Path.GetDirectoryName(_filePath)!;
        var name = Path.GetFileName(_filePath);
        _watcher = new FileSystemWatcher(dir, name)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        };
        _watcher.Changed += OnFileEvent;
        _watcher.Created += OnFileEvent;
        _watcher.Renamed += OnFileEvent;
        _watcher.EnableRaisingEvents = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher?.Dispose();
        _watcher = null;
        return Task.CompletedTask;
    }

    public void Dispose() => _watcher?.Dispose();

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastReload < DebounceWindow) return;
        _lastReload = now;

        // Brief delay so the editor has finished swapping the file in.
        Task.Delay(50).ContinueWith(_ => Reload());
    }

    private void Reload()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogWarning(
                    "Service map file {FilePath} not found; map is empty until it appears",
                    _filePath);
                _registry.Replace(Array.Empty<ServiceMapEntry>());
                return;
            }

            var yaml = File.ReadAllText(_filePath);
            var entries = ParseYaml(yaml);
            _registry.Replace(entries);

            _logger.LogInformation(
                "Loaded {Count} service map entries from {FilePath}",
                entries.Count, _filePath);
        }
        catch (Exception ex)
        {
            // Keep the previous snapshot on parse error. Logging the path
            // gives the operator the file to fix; not failing closed avoids
            // a routing outage from an unrelated edit.
            _logger.LogError(ex,
                "Failed to reload service map from {FilePath}; keeping previous snapshot",
                _filePath);
        }
    }

    private static IReadOnlyList<ServiceMapEntry> ParseYaml(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var doc = deserializer.Deserialize<ServiceMapFile>(yaml)
            ?? throw new InvalidDataException("Service map file is empty.");

        if (doc.Services is null) return Array.Empty<ServiceMapEntry>();

        return doc.Services
            .Where(s => !string.IsNullOrWhiteSpace(s.ServiceId))
            .Select(s => new ServiceMapEntry(
                ServiceId: s.ServiceId!.Trim(),
                LocalUrl: NullIfBlank(s.LocalUrl),
                RemoteUrlPattern: NullIfBlank(s.RemoteUrlPattern),
                RequiresAuth: s.RequiresAuth))
            .ToList();
    }

    private static string? NullIfBlank(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>YAML-side mutable shape; YamlDotNet doesn't bind to records.</summary>
    private sealed class ServiceMapFile
    {
        public List<ServiceMapEntryYaml>? Services { get; set; }
    }

    private sealed class ServiceMapEntryYaml
    {
        public string? ServiceId { get; set; }
        public string? LocalUrl { get; set; }
        public string? RemoteUrlPattern { get; set; }
        public bool RequiresAuth { get; set; }
    }
}
