namespace McpGateway.Domain.Enums;

/// <summary>
/// Enum representing the type of MCP adapter communication protocol.
/// </summary>
public enum AdapterType
{
    /// <summary>
    /// Streamable HTTP adapter - supports HTTP streaming communication
    /// </summary>
    StreamableHttp = 1,
    
    /// <summary>
    /// Server-Sent Events (SSE) adapter - supports real-time streaming via SSE
    /// </summary>
    Sse = 2
}
