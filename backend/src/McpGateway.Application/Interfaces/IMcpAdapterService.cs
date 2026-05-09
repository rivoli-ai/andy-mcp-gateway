using McpGateway.Application.DTOs;

namespace McpGateway.Application.Interfaces;

/// <summary>
/// Application service for MCP adapter management: CRUD, health checks, search.
/// </summary>
public interface IMcpAdapterService
{
    Task<McpAdapterDto?> GetByIdAsync(Guid id);
    Task<McpAdapterDto?> GetByNameAsync(string name);
    Task<AdapterListDto> GetAllAsync();
    Task<AdapterListDto> GetEnabledAsync();
    Task<McpAdapterDto> CreateAsync(CreateMcpAdapterDto dto);
    Task<McpAdapterDto> UpdateAsync(Guid id, UpdateMcpAdapterDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<AdapterHealthDto> CheckHealthAsync(Guid id);
    Task<IEnumerable<AdapterHealthDto>> CheckAllHealthAsync();
    Task<AdapterListDto> SearchAsync(string? name = null, bool? enabled = null);
}
