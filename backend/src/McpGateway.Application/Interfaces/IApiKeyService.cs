using McpGateway.Application.DTOs;

namespace McpGateway.Application.Interfaces;

/// <summary>
/// API key management for the gateway. Keys are global to the application — anyone with
/// access to the management UI can create, revoke, and reveal them. Authentication on
/// MCP routes uses these keys via the <c>X-MCP-Key</c> header.
/// </summary>
public interface IApiKeyService
{
    Task<IReadOnlyList<ApiKeyDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates a new key. The plaintext is returned exactly once via <see cref="CreatedApiKeyDto.Key"/>.</summary>
    Task<CreatedApiKeyDto> CreateAsync(CreateApiKeyDto request, string? createdBy, CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Decrypts the stored ciphertext to surface the plaintext to the caller.</summary>
    Task<RevealedApiKeyDto?> RevealAsync(Guid id, CancellationToken cancellationToken = default);
}
