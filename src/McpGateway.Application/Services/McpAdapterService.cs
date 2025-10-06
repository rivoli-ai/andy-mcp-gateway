using AutoMapper;
using McpGateway.Application.DTOs;
using McpGateway.Application.Interfaces;
using McpGateway.Domain.Interfaces;
using McpGateway.Domain.Models;
using Microsoft.Extensions.Logging;

namespace McpGateway.Application.Services;

/// <summary>
/// Service implementation for managing MCP adapters.
/// Provides business logic for adapter CRUD operations, health checking, and status management.
/// </summary>
public class McpAdapterService : IMcpAdapterService
{
    private readonly IMcpAdapterRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<McpAdapterService> _logger;

    public McpAdapterService(
        IMcpAdapterRepository repository,
        IMapper mapper,
        ILogger<McpAdapterService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<McpAdapterDto?> GetByIdAsync(Guid id)
    {
        var adapter = await _repository.GetByIdAsync(id);
        return adapter != null ? MapToDto(adapter) : null;
    }

    public async Task<McpAdapterDto?> GetByNameAsync(string name)
    {
        var adapter = await _repository.GetByNameAsync(name);
        return adapter != null ? MapToDto(adapter) : null;
    }

    public async Task<AdapterListDto> GetAllAsync()
    {
        var adapters = await _repository.GetAllAsync();
        var adapterDtos = adapters.Select(MapToDto).ToList();
        
        return new AdapterListDto
        {
            Adapters = adapterDtos,
            Total = adapterDtos.Count,
            Healthy = adapterDtos.Count(a => a.IsHealthy && a.Enabled),
            Unhealthy = adapterDtos.Count(a => !a.IsHealthy && a.Enabled),
            Disabled = adapterDtos.Count(a => !a.Enabled)
        };
    }

    public async Task<AdapterListDto> GetEnabledAsync()
    {
        var adapters = await _repository.GetEnabledAsync();
        var adapterDtos = adapters.Select(MapToDto).ToList();
        
        return new AdapterListDto
        {
            Adapters = adapterDtos,
            Total = adapterDtos.Count,
            Healthy = adapterDtos.Count(a => a.IsHealthy),
            Unhealthy = adapterDtos.Count(a => !a.IsHealthy),
            Disabled = 0
        };
    }

    public async Task<McpAdapterDto> CreateAsync(CreateMcpAdapterDto dto)
    {
        // Check if adapter with same name already exists
        if (await _repository.ExistsByNameAsync(dto.Name))
        {
            throw new InvalidOperationException($"Adapter with name '{dto.Name}' already exists");
        }

        var adapter = _mapper.Map<McpAdapter>(dto);
        adapter.CreatedAt = DateTime.UtcNow;
        adapter.UpdatedAt = DateTime.UtcNow;

        var createdAdapter = await _repository.CreateAsync(adapter);
        _logger.LogInformation("Created MCP adapter: {Name} -> {Url}", createdAdapter.Name, createdAdapter.Url);
        
        return MapToDto(createdAdapter);
    }

    public async Task<McpAdapterDto> UpdateAsync(Guid id, UpdateMcpAdapterDto dto)
    {
        var existingAdapter = await _repository.GetByIdAsync(id);
        if (existingAdapter == null)
        {
            throw new KeyNotFoundException($"Adapter with ID '{id}' not found");
        }

        // Check if new name conflicts with existing adapter
        if (!string.IsNullOrEmpty(dto.Name) && dto.Name != existingAdapter.Name)
        {
            if (await _repository.ExistsByNameAsync(dto.Name))
            {
                throw new InvalidOperationException($"Adapter with name '{dto.Name}' already exists");
            }
        }

        _mapper.Map(dto, existingAdapter);
        existingAdapter.MarkAsUpdated(dto.UpdatedBy);

        var updatedAdapter = await _repository.UpdateAsync(existingAdapter);
        _logger.LogInformation("Updated MCP adapter: {Name} -> {Url}", updatedAdapter.Name, updatedAdapter.Url);
        
        return MapToDto(updatedAdapter);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var result = await _repository.DeleteAsync(id);
        if (result)
        {
            _logger.LogInformation("Deleted MCP adapter with ID: {Id}", id);
        }
        return result;
    }

    public async Task<AdapterHealthDto> CheckHealthAsync(Guid id)
    {
        var adapter = await _repository.GetByIdAsync(id);
        if (adapter == null)
        {
            throw new KeyNotFoundException($"Adapter with ID '{id}' not found");
        }

        var healthStatus = await CheckAdapterHealth(adapter);
        await _repository.UpdateHealthStatusAsync(id, healthStatus.isHealthy, healthStatus.responseTimeMs, healthStatus.error);

        return new AdapterHealthDto
        {
            Id = adapter.Id,
            Name = adapter.Name,
            Url = adapter.Url,
            Status = healthStatus.isHealthy ? "healthy" : "unhealthy",
            LastCheck = DateTime.UtcNow,
            ResponseTimeMs = healthStatus.responseTimeMs,
            LastError = healthStatus.error
        };
    }

    public async Task<IEnumerable<AdapterHealthDto>> CheckAllHealthAsync()
    {
        var adapters = await _repository.GetEnabledAsync();
        var healthChecks = new List<AdapterHealthDto>();

        foreach (var adapter in adapters)
        {
            try
            {
                var healthStatus = await CheckAdapterHealth(adapter);
                await _repository.UpdateHealthStatusAsync(adapter.Id, healthStatus.isHealthy, healthStatus.responseTimeMs, healthStatus.error);

                healthChecks.Add(new AdapterHealthDto
                {
                    Id = adapter.Id,
                    Name = adapter.Name,
                    Url = adapter.Url,
                    Status = healthStatus.isHealthy ? "healthy" : "unhealthy",
                    LastCheck = DateTime.UtcNow,
                    ResponseTimeMs = healthStatus.responseTimeMs,
                    LastError = healthStatus.error
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health for adapter {Name}", adapter.Name);
                healthChecks.Add(new AdapterHealthDto
                {
                    Id = adapter.Id,
                    Name = adapter.Name,
                    Url = adapter.Url,
                    Status = "error",
                    LastCheck = DateTime.UtcNow,
                    LastError = ex.Message
                });
            }
        }

        return healthChecks;
    }

    public async Task<AdapterListDto> SearchAsync(string? name = null, bool? enabled = null)
    {
        var adapters = await _repository.SearchAsync(name, enabled);
        var adapterDtos = adapters.Select(MapToDto).ToList();
        
        return new AdapterListDto
        {
            Adapters = adapterDtos,
            Total = adapterDtos.Count,
            Healthy = adapterDtos.Count(a => a.IsHealthy && a.Enabled),
            Unhealthy = adapterDtos.Count(a => !a.IsHealthy && a.Enabled),
            Disabled = adapterDtos.Count(a => !a.Enabled)
        };
    }

    public Task<bool> ReloadMappingsAsync()
    {
        // This method can be used to reload adapter mappings from configuration
        // For now, we'll just return true as the database is the source of truth
        _logger.LogInformation("Reloading adapter mappings from database");
        return Task.FromResult(true);
    }


    private McpAdapterDto MapToDto(McpAdapter adapter)
    {
        var dto = _mapper.Map<McpAdapterDto>(adapter);
        dto.Status = GetAdapterStatus(adapter);
        return dto;
    }

    private string GetAdapterStatus(McpAdapter adapter)
    {
        if (!adapter.Enabled) return "disabled";
        if (!adapter.IsHealthy) return "unhealthy";
        return "healthy";
    }

    private async Task<(bool isHealthy, int? responseTimeMs, string? error)> CheckAdapterHealth(McpAdapter adapter)
    {
        var start = DateTime.UtcNow;
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.GetAsync($"{adapter.Url.TrimEnd('/')}/health");
            var isHealthy = response.IsSuccessStatusCode;
            var elapsed = (int)(DateTime.UtcNow - start).TotalMilliseconds;
            return (isHealthy, elapsed, null);
        }
        catch (Exception ex)
        {
            var elapsed = (int)(DateTime.UtcNow - start).TotalMilliseconds;
            return (false, elapsed, ex.Message);
        }
    }

    private async Task<(bool IsOnline, bool IsHealthy, int? ResponseTimeMs, string? Error)> CheckServiceConnectivity(string url)
    {
        var start = DateTime.UtcNow;
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            
            // First, try to reach the base URL to check if service is online
            var baseResponse = await client.GetAsync(url.TrimEnd('/'));
            var isOnline = baseResponse.IsSuccessStatusCode;
            var elapsed = (int)(DateTime.UtcNow - start).TotalMilliseconds;
            
            if (!isOnline)
            {
                return (false, false, elapsed, $"Service at {url} returned status {baseResponse.StatusCode}");
            }

            // If online, try to check health endpoint
            try
            {
                var healthResponse = await client.GetAsync($"{url.TrimEnd('/')}/health");
                var isHealthy = healthResponse.IsSuccessStatusCode;
                return (true, isHealthy, elapsed, isHealthy ? null : $"Health check failed with status {healthResponse.StatusCode}");
            }
            catch (Exception healthEx)
            {
                // Service is online but health check failed
                return (true, false, elapsed, $"Health check failed: {healthEx.Message}");
            }
        }
        catch (Exception ex)
        {
            var elapsed = (int)(DateTime.UtcNow - start).TotalMilliseconds;
            return (false, false, elapsed, ex.Message);
        }
    }
}
