namespace McpGateway.Application.Auth;

/// <summary>OAuth 2.0 protected-resource metadata (RFC 9728) for MCP HTTP transports (Cursor, Cline, etc.).</summary>
public class McpGatewayOAuthOptions
{
    public const string SectionName = "Mcp:OAuth";

    public bool Enabled { get; set; } = true;

    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// Optional extra valid <c>aud</c> for Entra access tokens (e.g. Application ID URI <c>api://{app-guid}</c>). Added to JWT bearer validation; not written to PRM <c>resource</c> (that value is inferred per MCP URL so clients like Cursor can match the connection string).
    /// </summary>
    public string? ProtectedResourceIdentifier { get; set; }

    public string? AuthorizationServer { get; set; }

    public string[] ScopesSupported { get; set; } = ["mcp-access"];

    public string[]? ValidAudiences { get; set; }

    public bool ProxyEntraAuthorizationServerMetadata { get; set; } = true;

    public string? DynamicClientRegistrationClientId { get; set; }

    public bool StripResourceOnAuthorizeRedirect { get; set; } = true;
}
