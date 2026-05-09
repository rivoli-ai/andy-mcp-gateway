using MapsterMapper;
using McpGateway.Application.DTOs;
using McpGateway.Application.Interfaces;
using McpGateway.Application.Mapping;
using McpGateway.Domain.Interfaces;
using McpGateway.Domain.Models;
using Microsoft.Extensions.Logging;

namespace McpGateway.Application.Services;

/// <summary>
/// Application service for MCP adapter management: CRUD, search, and health checks.
/// </summary>
public sealed class McpAdapterService : IMcpAdapterService
{
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(5);

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

    public async Task<McpAdapterDto?> GetByIdAsync(Guid id) =>
        MapOrNull(await _repository.GetByIdAsync(id));

    public async Task<McpAdapterDto?> GetByNameAsync(string name) =>
        MapOrNull(await _repository.GetByNameAsync(name));

    public async Task<AdapterListDto> GetAllAsync() =>
        BuildList(await _repository.GetAllAsync());

    public async Task<AdapterListDto> GetEnabledAsync() =>
        BuildList(await _repository.GetEnabledAsync());

    public async Task<AdapterListDto> SearchAsync(string? name = null, bool? enabled = null) =>
        BuildList(await _repository.SearchAsync(name, enabled));

    public async Task<McpAdapterDto> CreateAsync(CreateMcpAdapterDto dto)
    {
        if (await _repository.ExistsByNameAsync(dto.Name))
            throw new InvalidOperationException($"Adapter with name '{dto.Name}' already exists");

        var adapter = _mapper.Map<McpAdapter>(dto);
        adapter.CreatedAt = DateTime.UtcNow;
        adapter.UpdatedAt = DateTime.UtcNow;

        var created = await _repository.CreateAsync(adapter);
        _logger.LogInformation("Created MCP adapter: {Name} -> {Url}", created.Name, created.Url);

        return MapToDto(created);
    }

    public async Task<McpAdapterDto> UpdateAsync(Guid id, UpdateMcpAdapterDto dto)
    {
        var existing = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Adapter with ID '{id}' not found");

        if (!string.IsNullOrEmpty(dto.Name)
            && !string.Equals(dto.Name, existing.Name, StringComparison.Ordinal)
            && await _repository.ExistsByNameAsync(dto.Name))
        {
            throw new InvalidOperationException($"Adapter with name '{dto.Name}' already exists");
        }

        McpAdapterPartialUpdate.Apply(dto, existing);
        existing.MarkAsUpdated(dto.UpdatedBy);

        var updated = await _repository.UpdateAsync(existing);
        _logger.LogInformation("Updated MCP adapter: {Name} -> {Url}", updated.Name, updated.Url);

        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (deleted)
            _logger.LogInformation("Deleted MCP adapter with ID: {Id}", id);
        return deleted;
    }

    public async Task<AdapterHealthDto> CheckHealthAsync(Guid id)
    {
        var adapter = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Adapter with ID '{id}' not found");

        return await ProbeAndPersistHealthAsync(adapter);
    }

    public async Task<IEnumerable<AdapterHealthDto>> CheckAllHealthAsync()
    {
        var adapters = await _repository.GetEnabledAsync();
        var results = new List<AdapterHealthDto>();

        foreach (var adapter in adapters)
        {
            try
            {
                results.Add(await ProbeAndPersistHealthAsync(adapter));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health for adapter {Name}", adapter.Name);
                results.Add(new AdapterHealthDto
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

        return results;
    }

    private async Task<AdapterHealthDto> ProbeAndPersistHealthAsync(McpAdapter adapter)
    {
        var probe = await ProbeAsync(adapter).ConfigureAwait(false);

        await _repository.UpdateHealthStatusAsync(adapter.Id, probe.IsHealthy, probe.ResponseTimeMs, probe.Error)
            .ConfigureAwait(false);

        return new AdapterHealthDto
        {
            Id = adapter.Id,
            Name = adapter.Name,
            Url = adapter.Url,
            Status = probe.IsHealthy ? "healthy" : "unhealthy",
            LastCheck = DateTime.UtcNow,
            ResponseTimeMs = probe.ResponseTimeMs,
            LastError = probe.Error
        };
    }

    private static async Task<HealthCheckResult> ProbeAsync(McpAdapter adapter)
    {
        var start = DateTime.UtcNow;
        try
        {
            using var client = new HttpClient { Timeout = HealthCheckTimeout };
            var response = await client.GetAsync($"{adapter.Url.TrimEnd('/')}/health").ConfigureAwait(false);
            return new HealthCheckResult(response.IsSuccessStatusCode, ElapsedMs(start), null);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(false, ElapsedMs(start), ex.Message);
        }
    }

    private AdapterListDto BuildList(IEnumerable<McpAdapter> adapters)
    {
        var items = adapters.Select(MapToDto).ToList();
        return new AdapterListDto
        {
            Adapters = items,
            Total = items.Count,
            Healthy = items.Count(a => a.IsHealthy && a.Enabled),
            Unhealthy = items.Count(a => !a.IsHealthy && a.Enabled),
            Disabled = items.Count(a => !a.Enabled)
        };
    }

    private McpAdapterDto MapToDto(McpAdapter adapter) => _mapper.Map<McpAdapterDto>(adapter);

    private McpAdapterDto? MapOrNull(McpAdapter? adapter) => adapter is null ? null : MapToDto(adapter);

    private static int ElapsedMs(DateTime start) => (int)(DateTime.UtcNow - start).TotalMilliseconds;

    private readonly record struct HealthCheckResult(bool IsHealthy, int? ResponseTimeMs, string? Error);
}
