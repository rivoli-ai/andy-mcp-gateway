using Andy.Mcp.Gateway.Models;
using Andy.Mcp.Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace Andy.Mcp.Gateway.Controllers;

/// <summary>
/// Read-only diagnostic surface over the gateway's service map and routing
/// decisions. Operators (and Conductor's LocalRuntime UI) can inspect what
/// the gateway thinks the world looks like.
/// </summary>
[ApiController]
[Route("api/routing")]
[Produces("application/json")]
public sealed class RoutingController : ControllerBase
{
    private readonly IServiceMapRegistry _registry;
    private readonly IServiceRouter _router;
    private readonly IRouteHealthMonitor _health;

    public RoutingController(
        IServiceMapRegistry registry,
        IServiceRouter router,
        IRouteHealthMonitor health)
    {
        _registry = registry;
        _router = router;
        _health = health;
    }

    /// <summary>
    /// List every service in the map plus its current health.
    /// </summary>
    [HttpGet("services")]
    [ProducesResponseType(typeof(IEnumerable<ServiceMapView>), StatusCodes.Status200OK)]
    public IActionResult ListServices()
    {
        var view = _registry.Entries
            .Select(e => new ServiceMapView(
                ServiceId: e.ServiceId,
                LocalUrl: e.LocalUrl,
                RemoteUrlPattern: e.RemoteUrlPattern,
                RequiresAuth: e.RequiresAuth,
                LocalHealthy: !string.IsNullOrEmpty(e.LocalUrl) && _health.IsLocalHealthy(e.ServiceId)));
        return Ok(view);
    }

    /// <summary>
    /// Resolve a single service id — same call the gateway internals make
    /// when forwarding a request. Useful for debugging routing decisions.
    /// </summary>
    [HttpGet("resolve/{serviceId}")]
    [ProducesResponseType(typeof(ResolvedRoute), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Resolve(string serviceId, CancellationToken ct)
    {
        try
        {
            var route = await _router.ResolveAsync(serviceId, ct);
            return Ok(route);
        }
        catch (ServiceNotInMapException)
        {
            return NotFound(new { error = "service_not_in_map", serviceId });
        }
        catch (ServiceUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "service_unavailable", message = ex.Message });
        }
    }
}

/// <summary>Diagnostic projection over a service map entry.</summary>
public sealed record ServiceMapView(
    string ServiceId,
    string? LocalUrl,
    string? RemoteUrlPattern,
    bool RequiresAuth,
    bool LocalHealthy);
