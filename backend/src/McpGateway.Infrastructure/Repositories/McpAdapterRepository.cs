using MapsterMapper;
using McpGateway.Domain.Entities;
using McpGateway.Domain.Interfaces;
using McpGateway.Domain.Models;
using McpGateway.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace McpGateway.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of <see cref="IMcpAdapterRepository"/>.
/// </summary>
public sealed class McpAdapterRepository : IMcpAdapterRepository
{
    private readonly McpGatewayDbContext _context;
    private readonly IMapper _mapper;

    public McpAdapterRepository(McpGatewayDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<McpAdapter?> GetByIdAsync(Guid id) =>
        ToDomain(await _context.McpAdapters.FindAsync(id));

    public async Task<McpAdapter?> GetByNameAsync(string name) =>
        ToDomain(await _context.McpAdapters
            .FirstOrDefaultAsync(a => a.Name.ToLower() == name.ToLower()));

    public async Task<IEnumerable<McpAdapter>> GetAllAsync() =>
        await ToDomainListAsync(_context.McpAdapters.OrderBy(a => a.Name));

    public async Task<IEnumerable<McpAdapter>> GetEnabledAsync() =>
        await ToDomainListAsync(_context.McpAdapters.Where(a => a.Enabled).OrderBy(a => a.Name));

    public async Task<IEnumerable<McpAdapter>> SearchAsync(string? name = null, bool? enabled = null)
    {
        var query = _context.McpAdapters.AsQueryable();

        if (!string.IsNullOrEmpty(name))
            query = query.Where(a => a.Name.Contains(name));

        if (enabled.HasValue)
            query = query.Where(a => a.Enabled == enabled.Value);

        return await ToDomainListAsync(query.OrderBy(a => a.Name));
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
        var entity = await _context.McpAdapters.FindAsync(adapter.Id)
            ?? throw new KeyNotFoundException($"Adapter with ID '{adapter.Id}' not found");

        _mapper.Map(adapter, entity);
        await _context.SaveChangesAsync();
        return _mapper.Map<McpAdapter>(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.McpAdapters.FindAsync(id);
        if (entity is null)
            return false;

        _context.McpAdapters.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<bool> ExistsAsync(Guid id) =>
        _context.McpAdapters.AnyAsync(a => a.Id == id);

    public Task<bool> ExistsByNameAsync(string name) =>
        _context.McpAdapters.AnyAsync(a => a.Name.ToLower() == name.ToLower());

    public async Task UpdateHealthStatusAsync(Guid id, bool isHealthy, int? responseTimeMs = null, string? error = null)
    {
        var entity = await _context.McpAdapters.FindAsync(id);
        if (entity is null)
            return;

        entity.IsHealthy = isHealthy;
        entity.LastHealthCheck = DateTime.UtcNow;
        entity.LastResponseTimeMs = responseTimeMs;
        entity.LastError = error;
        await _context.SaveChangesAsync();
    }

    private McpAdapter? ToDomain(McpAdapterEntity? entity) =>
        entity is null ? null : _mapper.Map<McpAdapter>(entity);

    private async Task<List<McpAdapter>> ToDomainListAsync(IQueryable<McpAdapterEntity> query)
    {
        var entities = await query.ToListAsync();
        return entities.Select(_mapper.Map<McpAdapter>).ToList();
    }
}
