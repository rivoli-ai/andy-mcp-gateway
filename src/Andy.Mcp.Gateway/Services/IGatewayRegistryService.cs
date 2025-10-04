using Andy.Mcp.Gateway.Models;

namespace Andy.Mcp.Gateway.Services;

/// <summary>
/// Service interface for managing MCP gateway registrations
/// </summary>
public interface IGatewayRegistryService
{
    /// <summary>
    /// Get all gateways
    /// </summary>
    Task<IEnumerable<McpGateway>> GetAllGatewaysAsync();

    /// <summary>
    /// Get a gateway by ID
    /// </summary>
    Task<McpGateway?> GetGatewayByIdAsync(string id);

    /// <summary>
    /// Search gateways with query parameters
    /// </summary>
    Task<IEnumerable<McpGateway>> SearchGatewaysAsync(GatewaySearchQuery query);

    /// <summary>
    /// Create a new gateway registration
    /// </summary>
    Task<McpGateway> CreateGatewayAsync(CreateGatewayRequest request);

    /// <summary>
    /// Update an existing gateway registration
    /// </summary>
    Task<McpGateway?> UpdateGatewayAsync(string id, UpdateGatewayRequest request);

    /// <summary>
    /// Delete a gateway registration
    /// </summary>
    Task<bool> DeleteGatewayAsync(string id);
}
