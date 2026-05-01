using System.Collections.Concurrent;
using Andy.Mcp.Gateway.Models;

namespace Andy.Mcp.Gateway.Services;

/// <summary>
/// Probes every <see cref="ServiceMapEntry.LocalUrl"/> on a fixed interval
/// (default 5s) and exposes the latest result. Re-subscribes on registry
/// changes so newly-added entries pick up probing on the next cycle.
///
/// Probe shape: HEAD on the URL with a 2 s timeout. Treat any 2xx-3xx as
/// healthy. Network errors / timeouts mark unhealthy.
/// </summary>
public sealed class RouteHealthMonitor : BackgroundService, IRouteHealthMonitor
{
    private static readonly TimeSpan ProbeInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);

    private readonly IServiceMapRegistry _registry;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RouteHealthMonitor> _logger;
    private readonly ConcurrentDictionary<string, bool> _healthy = new(StringComparer.OrdinalIgnoreCase);

    public RouteHealthMonitor(
        IServiceMapRegistry registry,
        IHttpClientFactory httpClientFactory,
        ILogger<RouteHealthMonitor> logger)
    {
        _registry = registry;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public bool IsLocalHealthy(string serviceId)
        => _healthy.TryGetValue(serviceId, out var ok) && ok;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial pass before the first sleep so callers don't see a
        // window of "everything is unhealthy" right after startup.
        await ProbeAllAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(ProbeInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await ProbeAllAsync(stoppingToken);
        }
    }

    private async Task ProbeAllAsync(CancellationToken ct)
    {
        var entries = _registry.Entries;
        // Probe in parallel; per-probe timeout caps total cycle time.
        var tasks = entries
            .Where(e => !string.IsNullOrEmpty(e.LocalUrl))
            .Select(e => ProbeOneAsync(e, ct));
        await Task.WhenAll(tasks);
    }

    private async Task ProbeOneAsync(ServiceMapEntry entry, CancellationToken ct)
    {
        var localUrl = entry.LocalUrl!;
        var previous = _healthy.TryGetValue(entry.ServiceId, out var p) ? p : (bool?)null;

        bool nowHealthy;
        try
        {
            using var client = _httpClientFactory.CreateClient("route-probe");
            client.Timeout = ProbeTimeout;
            using var req = new HttpRequestMessage(HttpMethod.Head, localUrl);
            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            // Any 2xx/3xx is fine — many services don't expose HEAD on their
            // root, so a 405 still tells us the listener is up. Treat
            // anything < 500 as healthy; 5xx means the service is broken.
            nowHealthy = (int)resp.StatusCode < 500;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            nowHealthy = false;
        }

        _healthy[entry.ServiceId] = nowHealthy;

        if (previous != nowHealthy)
        {
            // Structured log on every transition so on-call sees the flip
            // without trawling for it. Log level chosen by direction:
            // healthy → unhealthy is a warning (something just went down);
            // unhealthy → healthy is informational (recovery).
            if (nowHealthy)
            {
                _logger.LogInformation(
                    "Local route healthy for {ServiceId} at {LocalUrl}",
                    entry.ServiceId, localUrl);
            }
            else
            {
                _logger.LogWarning(
                    "Local route unhealthy for {ServiceId} at {LocalUrl}; falling back to remote",
                    entry.ServiceId, localUrl);
            }
        }
    }
}
