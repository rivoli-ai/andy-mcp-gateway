using MapsterMapper;
using McpGateway.Application.Auth;
using McpGateway.Application.DTOs;
using McpGateway.Application.Interfaces;
using McpGateway.Domain.Interfaces;
using McpGateway.Domain.Models;

namespace McpGateway.Application.Services;

/// <summary>
/// Application service that wraps the API key repository with the crypto pipeline:
/// plaintext generation, SHA-256 hashing for lookup, and DataProtection envelope for reveal.
/// Keys are global to the gateway; <c>createdBy</c> is captured for audit only.
/// </summary>
public sealed class ApiKeyService : IApiKeyService
{
    private const int MinNameLength = 1;
    private const int MaxNameLength = 120;

    private readonly IApiKeyRepository _repository;
    private readonly IApiKeyCipher _cipher;
    private readonly IMapper _mapper;

    public ApiKeyService(IApiKeyRepository repository, IApiKeyCipher cipher, IMapper mapper)
    {
        _repository = repository;
        _cipher = cipher;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ApiKeyDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var keys = await _repository.ListAsync(cancellationToken);
        return keys.Select(_mapper.Map<ApiKeyDto>).ToList();
    }

    public async Task<CreatedApiKeyDto> CreateAsync(
        CreateApiKeyDto request,
        string? createdBy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var trimmedName = (request.Name ?? string.Empty).Trim();
        if (trimmedName.Length is < MinNameLength or > MaxNameLength)
            throw new ArgumentException($"Name must be between {MinNameLength} and {MaxNameLength} characters.", nameof(request));

        var plaintext = ApiKeyTokens.GeneratePlaintext();
        var key = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            CreatedBy = createdBy,
            KeyHash = ApiKeyTokens.ComputeHash(plaintext),
            KeyCipher = _cipher.Protect(plaintext),
            KeyPrefix = ApiKeyTokens.ComputePrefix(plaintext),
            CreatedAt = DateTime.UtcNow
        };

        var stored = await _repository.CreateAsync(key, cancellationToken);
        return new CreatedApiKeyDto
        {
            Metadata = _mapper.Map<ApiKeyDto>(stored),
            Key = plaintext
        };
    }

    public Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.RevokeAsync(id, cancellationToken);

    public async Task<RevealedApiKeyDto?> RevealAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = await _repository.GetByIdAsync(id, cancellationToken);
        if (key is null)
            return null;

        var plaintext = _cipher.Unprotect(key.KeyCipher);
        return new RevealedApiKeyDto { Id = key.Id, Key = plaintext };
    }
}
