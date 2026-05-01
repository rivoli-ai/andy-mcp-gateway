using Andy.Mcp.Gateway.Models;

namespace Andy.Mcp.Gateway.Services;

/// <summary>
/// Resolves a service id to the URL the gateway should call.
///
/// Policy: prefer-local-fallback-remote. If the service has a
/// <c>LocalUrl</c> and <see cref="IRouteHealthMonitor"/> reports it healthy,
/// the local URL wins. Otherwise the <c>RemoteUrlPattern</c> is returned (with
/// any tenant placeholders intact — substitution lands in MG3). If neither is
/// available, the call fails with <see cref="ServiceUnavailableException"/>.
/// </summary>
public interface IServiceRouter
{
    Task<ResolvedRoute> ResolveAsync(string serviceId, CancellationToken ct);
}

public sealed class ServiceUnavailableException : Exception
{
    public ServiceUnavailableException(string message) : base(message) { }
}

public sealed class ServiceNotInMapException : Exception
{
    public ServiceNotInMapException(string serviceId)
        : base($"Service '{serviceId}' is not in the service map.") { }
}
