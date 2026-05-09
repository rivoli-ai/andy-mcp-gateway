using McpGateway.Application.DTOs;
using McpGateway.Domain.Enums;

namespace McpGateway.Application.Interfaces;

/// <summary>
/// Service interface for managing MCP adapters.
/// Provides business logic operations for adapter management, health checking, and CRUD operations.
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
    Task<bool> ReloadMappingsAsync();
}
