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

    /// <summary>Personal API key scheme — checks the <c>X-MCP-Key</c> header against the api_keys table.</summary>
    public const string ApiKey = "ApiKey";
}

/// <summary>HTTP header used to pass a personal API key on MCP routes (gateway-specific, not <c>X-API-Key</c>).</summary>
public static class ApiKeyAuthenticationDefaults
{
    public const string HeaderName = "X-MCP-Key";
}
