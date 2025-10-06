using McpGateway.Domain.Models;

namespace McpGateway.Domain.Interfaces;

/// <summary>
/// Repository interface for managing MCP adapter data persistence operations.
/// Provides CRUD operations and health status management for adapters.
/// </summary>
public interface IMcpAdapterRepository
{
    Task<McpAdapter?> GetByIdAsync(Guid id);
    Task<McpAdapter?> GetByNameAsync(string name);
    Task<IEnumerable<McpAdapter>> GetAllAsync();
    Task<IEnumerable<McpAdapter>> GetEnabledAsync();
    Task<McpAdapter> CreateAsync(McpAdapter adapter);
    Task<McpAdapter> UpdateAsync(McpAdapter adapter);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> ExistsByNameAsync(string name);
    Task<IEnumerable<McpAdapter>> SearchAsync(string? name = null, bool? enabled = null);
    Task UpdateHealthStatusAsync(Guid id, bool isHealthy, int? responseTimeMs = null, string? error = null);
}
