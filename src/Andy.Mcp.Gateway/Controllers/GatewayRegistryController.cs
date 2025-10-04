using Andy.Mcp.Gateway.Models;
using Andy.Mcp.Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace Andy.Mcp.Gateway.Controllers;

/// <summary>
/// API controller for managing MCP gateway registrations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class GatewayRegistryController : ControllerBase
{
    private readonly IGatewayRegistryService _registryService;
    private readonly ILogger<GatewayRegistryController> _logger;

    public GatewayRegistryController(
        IGatewayRegistryService registryService,
        ILogger<GatewayRegistryController> logger)
    {
        _registryService = registryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all gateways
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<McpGateway>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<McpGateway>>> GetAllGateways()
    {
        var gateways = await _registryService.GetAllGatewaysAsync();
        return Ok(gateways);
    }

    /// <summary>
    /// Get a gateway by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(McpGateway), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<McpGateway>> GetGatewayById(string id)
    {
        var gateway = await _registryService.GetGatewayByIdAsync(id);
        if (gateway == null)
        {
            return NotFound(new { message = $"Gateway with ID '{id}' not found" });
        }

        return Ok(gateway);
    }

    /// <summary>
    /// Search gateways with query parameters
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(IEnumerable<McpGateway>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<McpGateway>>> SearchGateways(
        [FromBody] GatewaySearchQuery query)
    {
        var gateways = await _registryService.SearchGatewaysAsync(query);
        return Ok(gateways);
    }

    /// <summary>
    /// Create a new gateway registration
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(McpGateway), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<McpGateway>> CreateGateway(
        [FromBody] CreateGatewayRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Endpoint))
        {
            return BadRequest(new { message = "Name and Endpoint are required" });
        }

        var gateway = await _registryService.CreateGatewayAsync(request);
        _logger.LogInformation("Created gateway {GatewayId} - {GatewayName}", gateway.Id, gateway.Name);

        return CreatedAtAction(
            nameof(GetGatewayById),
            new { id = gateway.Id },
            gateway);
    }

    /// <summary>
    /// Update an existing gateway registration
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(McpGateway), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<McpGateway>> UpdateGateway(
        string id,
        [FromBody] UpdateGatewayRequest request)
    {
        var gateway = await _registryService.UpdateGatewayAsync(id, request);
        if (gateway == null)
        {
            return NotFound(new { message = $"Gateway with ID '{id}' not found" });
        }

        _logger.LogInformation("Updated gateway {GatewayId}", id);
        return Ok(gateway);
    }

    /// <summary>
    /// Delete a gateway registration
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGateway(string id)
    {
        var deleted = await _registryService.DeleteGatewayAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = $"Gateway with ID '{id}' not found" });
        }

        _logger.LogInformation("Deleted gateway {GatewayId}", id);
        return NoContent();
    }
}
