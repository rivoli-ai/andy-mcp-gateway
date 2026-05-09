namespace McpGateway.Authentication;

/// <summary>Authorization policy that gates MCP adapter HTTP transports (SSE / streamable HTTP).</summary>
public static class McpTransportAuthorizationPolicy
{
    public const string Name = "McpTransport";
}

/// <summary>ASP.NET Core authentication scheme names used on MCP adapter routes.</summary>
public static class McpTransportAuthenticationSchemes
{
    /// <summary>JWT bearer scheme that validates Microsoft Entra access tokens.</summary>
    public const string EntraAccessToken = "EntraAccessToken";
}
