namespace Andy.Mcp.Gateway.Models;

/// <summary>
/// Request model for updating an existing MCP gateway registration
/// </summary>
public class UpdateGatewayRequest
{
    /// <summary>
    /// Display name of the gateway
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Description of the gateway's purpose and functionality
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Base URL/endpoint for the gateway
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gateway version
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Tags for categorization and search
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Gateway status
    /// </summary>
    public GatewayStatus? Status { get; set; }

    /// <summary>
    /// Additional metadata as key-value pairs
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
