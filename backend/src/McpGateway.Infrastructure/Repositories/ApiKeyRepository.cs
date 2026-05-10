using MapsterMapper;
using McpGateway.Domain.Entities;
using McpGateway.Domain.Interfaces;
using McpGateway.Domain.Models;
using McpGateway.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace McpGateway.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IApiKeyRepository"/>. All write paths persist
/// timestamps in UTC; the table is keyed for fast hash lookup during authentication.
/// </summary>
public sealed class ApiKeyRepository : IApiKeyRepository
{
    private readonly McpGatewayDbContext _context;
    private readonly IMapper _mapper;

    public ApiKeyRepository(McpGatewayDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ApiKey>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.ApiKeys
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(_mapper.Map<ApiKey>).ToList();
    }

    public async Task<ApiKey?> GetActiveByHashAsync(string keyHash, CancellationToken cancellationToken = default)
    {
        var row = await _context.ApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.RevokedAt == null, cancellationToken);

        return row is null ? null : _mapper.Map<ApiKey>(row);
    }

    public async Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await _context.ApiKeys.FindAsync([id], cancellationToken);
        return row is null ? null : _mapper.Map<ApiKey>(row);
    }

    public async Task<ApiKey> CreateAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<ApiKeyEntity>(apiKey);
        _context.ApiKeys.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return _mapper.Map<ApiKey>(entity);
    }

    public async Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ApiKeys.FindAsync([id], cancellationToken);
        if (entity is null)
            return false;

        if (entity.RevokedAt is not null)
            return true; // idempotent

        entity.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task TouchLastUsedAsync(Guid id, DateTime usedAtUtc, CancellationToken cancellationToken = default)
    {
        // Single UPDATE without loading the row — keeps the auth-hot-path cheap.
        await _context.ApiKeys
            .Where(k => k.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(k => k.LastUsedAt, usedAtUtc), cancellationToken);
    }
}
