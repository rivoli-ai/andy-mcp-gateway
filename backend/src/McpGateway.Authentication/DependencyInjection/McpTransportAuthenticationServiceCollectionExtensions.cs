using McpGateway.Authentication.Internal;

namespace McpGateway.Authentication.DependencyInjection;

public static class McpTransportAuthenticationServiceCollectionExtensions
{
    /// <summary>Registers gateway JWT, optional Entra MCP JWT bearer, MCP OAuth challenge metadata, and the MCP transport authorization policy.</summary>
    public static McpTransportAuthenticationRegistration AddMcpTransportAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var resolved = ResolvedMcpTransportAuthentication.Resolve(configuration, environment);
        McpTransportAuthenticationWebRegistrar.AddAuthenticationAndAuthorization(services, resolved);
        return new McpTransportAuthenticationRegistration(resolved);
    }
}
