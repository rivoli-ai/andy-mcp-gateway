using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McpGateway.Domain.Entities;

/// <summary>
/// EF Core entity for a row in <c>api_keys</c>. Persistence-shape mirror of <see cref="Models.ApiKey"/>.
/// Keys are global to the gateway (not user-scoped); <see cref="CreatedBy"/> is audit-only.
/// </summary>
[Table("api_keys")]
public class ApiKeyEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(320)]
    public string? CreatedBy { get; set; }

    /// <summary>SHA-256 hex digest (64 chars) of the plaintext key.</summary>
    [Required, MaxLength(64)]
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>DataProtection ciphertext (base64url) — variable length, opaque.</summary>
    [Required, MaxLength(2000)]
    public string KeyCipher { get; set; } = string.Empty;

    [Required, MaxLength(16)]
    public string KeyPrefix { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
