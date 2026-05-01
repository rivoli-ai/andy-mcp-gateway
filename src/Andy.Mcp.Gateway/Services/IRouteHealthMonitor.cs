namespace Andy.Mcp.Gateway.Services;

/// <summary>
/// Tracks the health of every entry's <c>LocalUrl</c> via periodic HTTP
/// probes. The router consults <see cref="IsLocalHealthy"/> when picking
/// local-or-remote.
///
/// Remote URLs are not probed by this monitor — remote services are assumed
/// reachable, and failures bubble back to callers as 5xx from the proxy.
/// </summary>
public interface IRouteHealthMonitor
{
    /// <summary>
    /// True if the most recent probe of the service's local URL succeeded.
    /// False if the URL is missing, the probe is failing, or no probe has
    /// completed yet.
    /// </summary>
    bool IsLocalHealthy(string serviceId);
}
