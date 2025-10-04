namespace Andy.Mcp.Gateway.Models;

/// <summary>
/// Request model for creating a new MCP gateway registration
/// </summary>
public class CreateGatewayRequest
{
    /// <summary>
    /// Display name of the gateway
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the gateway's purpose and functionality
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Base URL/endpoint for the gateway
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gateway version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Tags for categorization and search
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Additional metadata as key-value pairs
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
