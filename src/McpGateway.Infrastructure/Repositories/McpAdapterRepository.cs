using MapsterMapper;
using McpGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using McpGateway.Domain.Interfaces;
using McpGateway.Domain.Models;
using McpGateway.Infrastructure.Data;

namespace McpGateway.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for MCP adapter data persistence.
/// Provides Entity Framework-based implementation of adapter repository operations.
/// </summary>
public class McpAdapterRepository : IMcpAdapterRepository
{
    private readonly McpGatewayDbContext _context;
    private readonly IMapper _mapper;

    public McpAdapterRepository(McpGatewayDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<McpAdapter?> GetByIdAsync(Guid id)
    {
        var entity = await _context.McpAdapters.FindAsync(id);
        return entity != null ? _mapper.Map<McpAdapter>(entity) : null;
    }

    public async Task<McpAdapter?> GetByNameAsync(string name)
    {
        var entity = await _context.McpAdapters
            .FirstOrDefaultAsync(a => a.Name.ToLower() == name.ToLower());
        return entity != null ? _mapper.Map<McpAdapter>(entity) : null;
    }

    public async Task<IEnumerable<McpAdapter>> GetAllAsync()
    {
        var entities = await _context.McpAdapters
            .OrderBy(a => a.Name)
            .ToListAsync();
        return entities.Select(e => _mapper.Map<McpAdapter>(e));
    }

    public async Task<IEnumerable<McpAdapter>> GetEnabledAsync()
    {
        var entities = await _context.McpAdapters
            .Where(a => a.Enabled)
            .OrderBy(a => a.Name)
            .ToListAsync();
        return entities.Select(e => _mapper.Map<McpAdapter>(e));
    }

    public async Task<McpAdapter> CreateAsync(McpAdapter adapter)
    {
        var entity = _mapper.Map<McpAdapterEntity>(adapter);
        _context.McpAdapters.Add(entity);
        await _context.SaveChangesAsync();
        return _mapper.Map<McpAdapter>(entity);
    }

    public async Task<McpAdapter> UpdateAsync(McpAdapter adapter)
    {
        var entity = await _context.McpAdapters.FindAsync(adapter.Id);
        if (entity == null)
        {
            throw new KeyNotFoundException($"Adapter with ID '{adapter.Id}' not found");
        }

        _mapper.Map(adapter, entity);
        await _context.SaveChangesAsync();
        return _mapper.Map<McpAdapter>(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.McpAdapters.FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        _context.McpAdapters.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.McpAdapters.AnyAsync(a => a.Id == id);
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.McpAdapters.AnyAsync(a => a.Name.ToLower() == name.ToLower());
    }

    public async Task<IEnumerable<McpAdapter>> SearchAsync(string? name = null, bool? enabled = null)
    {
        var query = _context.McpAdapters.AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(a => a.Name.Contains(name));
        }

        if (enabled.HasValue)
        {
            query = query.Where(a => a.Enabled == enabled.Value);
        }

        var entities = await query.OrderBy(a => a.Name).ToListAsync();
        return entities.Select(e => _mapper.Map<McpAdapter>(e));
    }

    public async Task UpdateHealthStatusAsync(Guid id, bool isHealthy, int? responseTimeMs = null, string? error = null)
    {
        var entity = await _context.McpAdapters.FindAsync(id);
        if (entity != null)
        {
            entity.IsHealthy = isHealthy;
            entity.LastHealthCheck = DateTime.UtcNow;
            entity.LastResponseTimeMs = responseTimeMs;
            entity.LastError = error;
            await _context.SaveChangesAsync();
        }
    }
}
