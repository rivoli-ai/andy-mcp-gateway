using McpGateway.Domain.Enums;

namespace McpGateway.Domain.Models;

/// <summary>
/// Domain model for an MCP (Model Context Protocol) adapter — the configuration and
/// last-known health state of an upstream server the gateway can proxy to.
/// </summary>
public class McpAdapter
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public bool Enabled { get; set; } = true;
    public AdapterType Type { get; set; } = AdapterType.StreamableHttp;
    public Dictionary<string, string>? Headers { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? LastHealthCheck { get; set; }
    public bool IsHealthy { get; set; }
    public int? LastResponseTimeMs { get; set; }
    public string? LastError { get; set; }

    /// <summary>Stamps <see cref="UpdatedAt"/> with the current UTC time and records who triggered the update.</summary>
    public void MarkAsUpdated(string? updatedBy = null)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}
