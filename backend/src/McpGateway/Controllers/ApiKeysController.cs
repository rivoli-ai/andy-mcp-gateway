using System.IdentityModel.Tokens.Jwt;
using McpGateway.Application.Auth;
using McpGateway.Application.DTOs;
using McpGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace McpGateway.Controllers;

/// <summary>
/// Management API for personal-style API keys used to authenticate MCP clients that
/// can't perform OAuth2/OIDC. Keys are global to the gateway; access is restricted to
/// operators carrying the <c>admin</c> role on their gateway JWT (assigned via the
/// <c>Admin:Emails</c> allow-list in configuration).
/// </summary>
[ApiController]
[Authorize(Roles = AuthenticationService.AdminRole)]
[Route("api/api-keys")]
public sealed class ApiKeysController : ControllerBase
{
    private readonly IApiKeyService _apiKeys;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(IApiKeyService apiKeys, ILogger<ApiKeysController> logger)
    {
        _apiKeys = apiKeys;
        _logger = logger;
    }

    /// <summary>List every API key (active and revoked) — never returns the plaintext.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApiKeyDto>>> List(CancellationToken cancellationToken) =>
        Ok(await _apiKeys.ListAsync(cancellationToken));

    /// <summary>Generate a new API key. The plaintext appears exactly once in the response body.</summary>
    [HttpPost]
    public async Task<ActionResult<CreatedApiKeyDto>> Create(
        [FromBody] CreateApiKeyDto request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required." });

        try
        {
            var createdBy = ResolveCallerLabel();
            var created = await _apiKeys.CreateAsync(request, createdBy, cancellationToken);
            return CreatedAtAction(nameof(List), new { id = created.Metadata.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>Revoke an API key. Idempotent — returns 204 whether or not it was already revoked.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        var revoked = await _apiKeys.RevokeAsync(id, cancellationToken);
        return revoked
            ? NoContent()
            : NotFound(new { error = $"API key '{id}' not found" });
    }

    /// <summary>Decrypt and return the plaintext for an existing API key (UI "reveal" action).</summary>
    [HttpGet("{id:guid}/reveal")]
    public async Task<ActionResult<RevealedApiKeyDto>> Reveal(Guid id, CancellationToken cancellationToken)
    {
        var revealed = await _apiKeys.RevealAsync(id, cancellationToken);
        return revealed is null
            ? NotFound(new { error = $"API key '{id}' not found" })
            : Ok(revealed);
    }

    /// <summary>Best-effort label of the management user who initiated a write — email, name, or sub claim.</summary>
    private string? ResolveCallerLabel()
    {
        var user = User;
        return user.FindFirst(JwtRegisteredClaimNames.Email)?.Value
               ?? user.FindFirst("email")?.Value
               ?? user.FindFirst("preferred_username")?.Value
               ?? user.FindFirst("name")?.Value
               ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    }
}
