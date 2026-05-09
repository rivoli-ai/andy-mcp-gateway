namespace McpGateway.Domain.Enums;

/// <summary>
/// Enumeration representing the possible health status values for an MCP adapter.
/// </summary>
public enum AdapterStatus
{
    Unknown = 0,
    Healthy = 1,
    Unhealthy = 2,
    Disabled = 3
}
