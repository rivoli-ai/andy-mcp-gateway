namespace McpGateway.Application.DTOs;

/// <summary>
/// Public projection of an API key — never carries the plaintext or its ciphertext.
/// </summary>
public sealed class ApiKeyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Body of a create-API-key request.</summary>
public sealed class CreateApiKeyDto
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Returned exactly once at creation. Includes the plaintext <see cref="Key"/>.
/// </summary>
public sealed class CreatedApiKeyDto
{
    public ApiKeyDto Metadata { get; set; } = new();
    public string Key { get; set; } = string.Empty;
}

/// <summary>Wraps the decrypted plaintext returned by the reveal endpoint.</summary>
public sealed class RevealedApiKeyDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
}
