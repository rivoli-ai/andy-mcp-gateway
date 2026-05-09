using System.IdentityModel.Tokens.Jwt;
using McpGateway.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace McpGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthProviderRegistry _providerRegistry;
    private readonly AuthenticationService _authenticationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AuthProviderRegistry providerRegistry,
        AuthenticationService authenticationService,
        ILogger<AuthController> logger)
    {
        _providerRegistry = providerRegistry;
        _authenticationService = authenticationService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("config")]
    public IActionResult GetAuthConfig()
    {
        var options = _providerRegistry.Options;
        var providers = new List<object>();

        foreach (var (name, config) in options.GetEnabledProviders())
        {
            var type = config.Type ?? "FrontendOidc";
            if (type.Equals("FrontendOidc", StringComparison.OrdinalIgnoreCase))
            {
                var frontendClientId = !string.IsNullOrEmpty(config.SpaClientId) ? config.SpaClientId : config.ClientId;
                providers.Add(new
                {
                    name,
                    type = "FrontendOidc",
                    authority = config.Authority,
                    clientId = frontendClientId,
                    scopes = config.Scopes,
                    tenantId = config.TenantId
                });
            }
        }

        return Ok(new { providers });
    }

    public sealed record TokenRequest(string? IdToken, string? AccessToken);

    public sealed record AuthResponse(string Token, object User);

    [AllowAnonymous]
    [HttpPost("{provider}/token")]
    public async Task<IActionResult> OidcTokenLogin(string provider, [FromBody] TokenRequest request, CancellationToken cancellationToken)
    {
        if (!_providerRegistry.TryGetProvider(provider, out var authProvider) || authProvider == null)
            return NotFound(new { message = $"Provider '{provider}' is not enabled" });

        var tokenToValidate = !string.IsNullOrWhiteSpace(request.IdToken) ? request.IdToken : request.AccessToken;
        if (string.IsNullOrWhiteSpace(tokenToValidate))
            return BadRequest(new { message = "ID token or access token is required" });

        try
        {
            var principal = await authProvider.ValidateTokenAsync(tokenToValidate!, cancellationToken);
            var jwt = principal.Identity as JwtSecurityToken;

            // Extract basic identity info
            var sub = principal.FindFirst("oid")?.Value
                      ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? Guid.NewGuid().ToString("N");

            var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                        ?? principal.FindFirst("preferred_username")?.Value
                        ?? principal.FindFirst("upn")?.Value
                        ?? principal.FindFirst("name")?.Value
                        ?? sub;

            var name = principal.FindFirst("name")?.Value ?? email;

            var appJwt = _authenticationService.GenerateToken(sub, email, name);

            return Ok(new AuthResponse(appJwt, new { email, name }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {Provider} OIDC token login", provider);
            return BadRequest(new { message = ex is ArgumentException ? ex.Message : $"Invalid or expired token: {ex.Message}" });
        }
    }
}

