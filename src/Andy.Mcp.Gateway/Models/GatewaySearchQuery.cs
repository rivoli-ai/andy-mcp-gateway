namespace Andy.Mcp.Gateway.Models;

/// <summary>
/// Query parameters for searching MCP gateways
/// </summary>
public class GatewaySearchQuery
{
    /// <summary>
    /// Search term to match against name and description
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by tags (any tag match)
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public GatewayStatus? Status { get; set; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 20;
}
