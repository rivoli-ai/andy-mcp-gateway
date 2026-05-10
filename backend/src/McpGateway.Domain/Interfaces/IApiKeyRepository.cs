using McpGateway.Domain.Models;

namespace McpGateway.Domain.Interfaces;

/// <summary>
/// Persistence contract for application-level API keys. Lookups during authentication go
/// through <see cref="GetActiveByHashAsync"/> (indexed); the UI sees the full list via
/// <see cref="ListAsync"/>.
/// </summary>
public interface IApiKeyRepository
{
    Task<IReadOnlyList<ApiKey>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Look up an active (non-revoked) key by its SHA-256 hex hash. Returns null when missing or revoked.</summary>
    Task<ApiKey?> GetActiveByHashAsync(string keyHash, CancellationToken cancellationToken = default);

    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ApiKey> CreateAsync(ApiKey apiKey, CancellationToken cancellationToken = default);

    /// <summary>Idempotent revoke: stamps <c>RevokedAt</c> if currently active. Returns false when not found.</summary>
    Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default);

    Task TouchLastUsedAsync(Guid id, DateTime usedAtUtc, CancellationToken cancellationToken = default);
}
