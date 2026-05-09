using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ModelContextProtocol.AspNetCore.Authentication;

namespace McpGateway.Authentication;

/// <summary>
/// When the MCP transport policy lists multiple JWT schemes, each JwtBearer may challenge without RFC 9728
/// <c>resource_metadata</c>. MCP clients require the MCP authentication scheme challenge on adapter routes.
/// </summary>
internal static class JwtBearerEntraMcpChallengeExtensions
{
    public static void AttachMcpResourceMetadataChallenge(JwtBearerOptions options)
    {
        options.Events ??= new JwtBearerEvents();
        var priorChallenge = options.Events.OnChallenge;
        options.Events.OnChallenge = async context =>
        {
            if (context.Response.HasStarted)
                return;

            if (!context.Request.Path.StartsWithSegments("/adapters"))
            {
                if (priorChallenge is not null)
                    await priorChallenge(context).ConfigureAwait(false);
                return;
            }

            context.HandleResponse();
            await context.HttpContext
                .ChallengeAsync(McpAuthenticationDefaults.AuthenticationScheme, context.Properties)
                .ConfigureAwait(false);
        };
    }
}
