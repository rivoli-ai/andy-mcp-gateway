namespace Andy.Mcp.Gateway.Models;

/// <summary>
/// Result of <see cref="Services.IServiceRouter.ResolveAsync"/>. Carries the
/// chosen URL plus a tag identifying whether the routing decision picked the
/// local or remote endpoint — useful for diagnostics and for MG4's auth
/// bridge, which only injects a tenant JWT when <see cref="Source"/> is
/// <see cref="RouteSource.Remote"/>.
/// </summary>
public sealed record ResolvedRoute(string TargetUrl, RouteSource Source);

public enum RouteSource
{
    /// <summary>Local MCP server (Conductor LocalRuntime / Mac).</summary>
    Local,
    /// <summary>Remote tenant in Rivoli AI Cloud.</summary>
    Remote,
}
