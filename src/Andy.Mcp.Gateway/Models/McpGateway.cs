namespace Andy.Mcp.Gateway.Models;

/// <summary>
/// Represents an MCP (Model Context Protocol) gateway registration
/// </summary>
public class McpGateway
{
    /// <summary>
    /// Unique identifier for the gateway
    /// </summary>
    public string Id { get; set; } = string.Empty;

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
    /// Gateway status (Active, Inactive, Maintenance)
    /// </summary>
    public GatewayStatus Status { get; set; } = GatewayStatus.Active;

    /// <summary>
    /// Date and time when the gateway was registered
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when the gateway was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata as key-value pairs
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Status of an MCP gateway
/// </summary>
public enum GatewayStatus
{
    Active,
    Inactive,
    Maintenance
}
