using McpGateway.Authentication.DependencyInjection;
using McpGateway.Authentication.Internal;

namespace McpGateway.Authentication;

/// <summary>Result of <see cref="McpTransportAuthenticationServiceCollectionExtensions.AddMcpTransportAuthentication"/>; maps Entra OAuth discovery endpoints after the HTTP pipeline is built.</summary>
public sealed class McpTransportAuthenticationRegistration
{
    private readonly ResolvedMcpTransportAuthentication _state;

    internal McpTransportAuthenticationRegistration(ResolvedMcpTransportAuthentication state) =>
        _state = state;

    /// <summary>Whether <see cref="MapEntraOAuthDiscoveryEndpoints"/> registers RFC8414 proxy + DCR shim routes.</summary>
    public bool MapsEntraOAuthDiscoveryEndpoints => _state.MapsEntraOAuthDiscoveryEndpoints;

    /// <summary>Maps Entra OIDC proxy, authorize/token passthrough, and DCR shim (call after <c>UseAuthorization</c>).</summary>
    public void MapEntraOAuthDiscoveryEndpoints(WebApplication app) =>
        EntraOAuthDiscoveryEndpointMapper.Map(app, _state);
}
