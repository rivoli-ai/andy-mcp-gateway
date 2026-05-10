using System.Security.Claims;
using System.Text.Encodings.Web;
using McpGateway.Application.Auth;
using McpGateway.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpGateway.Authentication.Internal;

/// <summary>
/// AuthenticationHandler for the <see cref="McpTransportAuthenticationSchemes.ApiKey"/> scheme.
/// Reads the <c>X-API-Key</c> header on every request:
/// <list type="bullet">
///   <item>Header missing → <see cref="AuthenticateResult.NoResult"/> so the next scheme (Bearer) can take over.</item>
///   <item>Header present and matches an active row in <c>api_keys</c> → success with a synthetic principal.</item>
///   <item>Header present but invalid → <see cref="AuthenticateResult.Fail(string)"/> (401).</item>
/// </list>
/// The handler updates <c>LastUsedAt</c> opportunistically on a background scope: the lookup
/// uses the request-scoped DbContext (awaited), but the touch runs against a fresh
/// service scope so it doesn't race the controller for the same Npgsql connection.
/// </summary>
internal sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IApiKeyRepository _repository;
    private readonly IServiceScopeFactory _scopeFactory;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        IApiKeyRepository repository,
        IServiceScopeFactory scopeFactory)
        : base(options, loggerFactory, encoder)
    {
        _repository = repository;
        _scopeFactory = scopeFactory;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.HeaderName, out var headerValues))
            return AuthenticateResult.NoResult();

        var presented = headerValues.ToString();
        if (string.IsNullOrWhiteSpace(presented))
            return AuthenticateResult.NoResult();

        var hash = ApiKeyTokens.ComputeHash(presented.Trim());
        var apiKey = await _repository.GetActiveByHashAsync(hash, Context.RequestAborted);
        if (apiKey is null)
        {
            Logger.LogDebug("X-API-Key did not match any active key");
            return AuthenticateResult.Fail("Invalid API key");
        }

        // Update LastUsedAt on a fresh scope so it doesn't share the request's DbContext
        // (Npgsql allows only one in-flight command per connection — fire-and-forget against
        // the request-scoped context would race the controller and throw OperationInProgress).
        _ = TouchLastUsedAsync(apiKey.Id);

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, $"apikey:{apiKey.Id}"),
            new Claim(ClaimTypes.Name, apiKey.Name),
            new Claim("apikey_id", apiKey.Id.ToString()),
            new Claim("auth_method", "api_key")
        ], Scheme.Name);

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }

    private async Task TouchLastUsedAsync(Guid id)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();
            await repo.TouchLastUsedAsync(id, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to update LastUsedAt for API key {KeyId}", id);
        }
    }
}
