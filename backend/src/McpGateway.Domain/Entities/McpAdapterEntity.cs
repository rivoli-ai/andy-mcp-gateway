using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using McpGateway.Domain.Enums;

namespace McpGateway.Domain.Entities;

/// <summary>
/// Entity representing an MCP (Model Context Protocol) adapter in the database.
/// Contains configuration and health status information for adapters that can be proxied.
/// </summary>
[Table("mcp_adapters")]
public class McpAdapterEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int TimeoutSeconds { get; set; } = 30;

    public bool Enabled { get; set; } = true;

    public AdapterType Type { get; set; } = AdapterType.StreamableHttp;

    /// <summary>
    /// Custom headers to include with requests to this adapter (stored as JSON)
    /// </summary>
    public string? Headers { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    // Health check related properties
    public DateTime? LastHealthCheck { get; set; }

    public bool IsHealthy { get; set; } = false;

    public int? LastResponseTimeMs { get; set; }

    [MaxLength(1000)]
    public string? LastError { get; set; }
}
