namespace McpGateway.Domain.Models;

/// <summary>
/// Domain model for an application-level API key. Issued via the gateway management UI
/// as an alternative authentication path for MCP clients that cannot perform OAuth2/OIDC.
/// Keys are global to the gateway (not scoped to a user) — they only authenticate on MCP
/// transport routes, never on management endpoints.
/// The plaintext is never persisted: only a SHA-256 lookup hash plus an encrypted ciphertext
/// (so the UI can re-display it on demand). Never expires; the only kill switch is <see cref="RevokedAt"/>.
/// </summary>
public sealed class ApiKey
{
    public Guid Id { get; set; }

    /// <summary>Human-readable label set at creation time.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Audit-only: the management user who created this key (subject id or email).</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Constant-length SHA-256 hex digest of the plaintext key, used for O(1) lookup.</summary>
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>Encrypted ciphertext of the plaintext key (DataProtection); allows the UI to reveal it.</summary>
    public string KeyCipher { get; set; } = string.Empty;

    /// <summary>First few characters of the plaintext, e.g. "mcpg_abc1" — safe to surface alongside a masked display.</summary>
    public string KeyPrefix { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsActive => RevokedAt is null;
}
