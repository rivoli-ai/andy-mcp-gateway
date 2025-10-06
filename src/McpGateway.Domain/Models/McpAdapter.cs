using McpGateway.Domain.Enums;

namespace McpGateway.Domain.Models;

/// <summary>
/// Domain model representing an MCP (Model Context Protocol) adapter.
/// Contains business logic for adapter health management and status tracking.
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? LastHealthCheck { get; set; }
    public bool IsHealthy { get; set; } = false;
    public int? LastResponseTimeMs { get; set; }
    public string? LastError { get; set; }

    // Business logic methods
    public bool IsReachable()
    {
        return Enabled && IsHealthy;
    }

    public void UpdateHealthStatus(bool isHealthy, int? responseTimeMs = null, string? error = null)
    {
        IsHealthy = isHealthy;
        LastHealthCheck = DateTime.UtcNow;
        LastResponseTimeMs = responseTimeMs;
        LastError = error;
    }

    public void MarkAsUpdated(string? updatedBy = null)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}
