using Andy.Mcp.Gateway.Models;

namespace Andy.Mcp.Gateway.Services;

/// <inheritdoc cref="IServiceRouter"/>
public sealed class ServiceRouter : IServiceRouter
{
    private readonly IServiceMapRegistry _registry;
    private readonly IRouteHealthMonitor _health;

    public ServiceRouter(IServiceMapRegistry registry, IRouteHealthMonitor health)
    {
        _registry = registry;
        _health = health;
    }

    public Task<ResolvedRoute> ResolveAsync(string serviceId, CancellationToken ct)
    {
        var entry = _registry.Find(serviceId)
            ?? throw new ServiceNotInMapException(serviceId);

        // Local takes precedence — only when both URL is present AND health
        // monitor reports the listener responding.
        if (!string.IsNullOrEmpty(entry.LocalUrl) && _health.IsLocalHealthy(entry.ServiceId))
        {
            return Task.FromResult(new ResolvedRoute(entry.LocalUrl, RouteSource.Local));
        }

        if (!string.IsNullOrEmpty(entry.RemoteUrlPattern))
        {
            return Task.FromResult(new ResolvedRoute(entry.RemoteUrlPattern, RouteSource.Remote));
        }

        throw new ServiceUnavailableException(
            $"Service '{serviceId}' has no healthy local route and no remote URL pattern.");
    }
}
