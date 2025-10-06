using System.Text.Json.Serialization;
using McpGateway.Domain.Enums;

namespace McpGateway.Application.DTOs;

/// <summary>
/// Data Transfer Object representing an MCP adapter for API responses.
/// Contains all adapter information including health status and metadata.
/// </summary>
public class McpAdapterDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TimeoutSeconds { get; set; }
    public bool Enabled { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AdapterType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? LastHealthCheck { get; set; }
    public bool IsHealthy { get; set; }
    public int? LastResponseTimeMs { get; set; }
    public string? LastError { get; set; }
    public string Status { get; set; } = "unknown";
}

/// <summary>
/// Data Transfer Object for creating a new MCP adapter.
/// Contains the required information to register a new adapter.
/// </summary>
public class CreateMcpAdapterDto
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public bool Enabled { get; set; } = true;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AdapterType Type { get; set; } = AdapterType.StreamableHttp;
    public string? CreatedBy { get; set; }
}

/// <summary>
/// Data Transfer Object for updating an existing MCP adapter.
/// All properties are optional to support partial updates.
/// </summary>
public class UpdateMcpAdapterDto
{
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? Description { get; set; }
    public int? TimeoutSeconds { get; set; }
    public bool? Enabled { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AdapterType? Type { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Data Transfer Object representing the health status of an MCP adapter.
/// Contains health check results and performance metrics.
/// </summary>
public class AdapterHealthDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Status { get; set; } = "unknown";
    public DateTime? LastCheck { get; set; }
    public int? ResponseTimeMs { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Data Transfer Object representing a collection of MCP adapters with summary statistics.
/// Includes counts of healthy, unhealthy, and disabled adapters.
/// </summary>
public class AdapterListDto
{
    public IEnumerable<McpAdapterDto> Adapters { get; set; } = Enumerable.Empty<McpAdapterDto>();
    public int Total { get; set; }
    public int Healthy { get; set; }
    public int Unhealthy { get; set; }
    public int Disabled { get; set; }
}
