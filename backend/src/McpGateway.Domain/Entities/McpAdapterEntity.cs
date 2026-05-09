using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using McpGateway.Domain.Enums;

namespace McpGateway.Domain.Entities;

/// <summary>
/// EF Core entity for an MCP adapter row in <c>mcp_adapters</c>. Persistence-shape mirror of
/// <see cref="Models.McpAdapter"/>; <c>Headers</c> is stored as a JSON string and projected
/// through <see cref="Mapping"/> registrations.
/// </summary>
[Table("mcp_adapters")]
public class McpAdapterEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int TimeoutSeconds { get; set; } = 30;

    public bool Enabled { get; set; } = true;

    public AdapterType Type { get; set; } = AdapterType.StreamableHttp;

    /// <summary>Custom headers to attach when proxying to this adapter, persisted as JSON.</summary>
    public string? Headers { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)] public string? CreatedBy { get; set; }
    [MaxLength(100)] public string? UpdatedBy { get; set; }

    public DateTime? LastHealthCheck { get; set; }
    public bool IsHealthy { get; set; }
    public int? LastResponseTimeMs { get; set; }

    [MaxLength(1000)] public string? LastError { get; set; }
}
