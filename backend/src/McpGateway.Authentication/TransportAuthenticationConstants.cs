namespace McpGateway.Authentication;

/// <summary>Authorization policy used for MCP adapter HTTP transports (SSE / streamable HTTP).</summary>
public static class McpTransportAuthorizationPolicy
{
    public const string Name = "McpTransport";
}

/// <summary>ASP.NET Core authentication scheme names for MCP transport.</summary>
public static class McpTransportAuthenticationSchemes
{
    /// <summary>JWT bearer for Microsoft Entra access tokens on MCP adapter routes.</summary>
    public const string EntraAccessToken = "EntraAccessToken";
}
