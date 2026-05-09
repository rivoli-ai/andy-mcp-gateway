using Mapster;
using MapsterMapper;
using McpGateway.Application.Interfaces;
using McpGateway.Application.Mapping;
using McpGateway.Application.Proxying;
using McpGateway.Application.Services;
using McpGateway.Authentication;
using McpGateway.Domain.Interfaces;
using McpGateway.Infrastructure;
using McpGateway.Infrastructure.Data;
using McpGateway.Infrastructure.Mapping;
using McpGateway.Infrastructure.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace McpGateway;

/// <summary>
/// Service registration and HTTP pipeline (CORS, auth, Swagger, endpoints). Called from <see cref="Program"/>.
/// </summary>
public sealed class Startup
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private McpTransportAuthenticationRegistration? _mcpTransportAuth;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    /// <summary>Kestrel limits for long-lived MCP streams; invoke from <c>Program</c> before <c>Build</c>.</summary>
    public static void ConfigureWebHost(IWebHostBuilder webHost)
    {
        webHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
            serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
            serverOptions.Limits.MinRequestBodyDataRate = null;
            serverOptions.Limits.MinResponseDataRate = null;
        });
        Console.WriteLine("[KESTREL] Configured with 10 minute timeouts for streaming");
    }

    public void ConfigureServices(IServiceCollection services)
    {
        AddControllersAndApiExploration(services);
        AddPersistence(services);
        AddMapster(services);
        AddApplicationServices(services);
        AddAuth(services);
        AddLogging(services);
        AddCors(services);
        AddSwagger(services);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (app is not WebApplication webApp)
            throw new InvalidOperationException("Application must be WebApplication (see Program.cs host setup).");

        if (env.IsDevelopment())
        {
            webApp.UseDeveloperExceptionPage();
            webApp.UseSwagger();
            webApp.UseSwaggerUI();
            Console.WriteLine("[ENV] Running in Development mode");
        }

        ApplyDatabaseMigrations(webApp);

        webApp.UseRouting();
        webApp.UseCors();
        webApp.UseAuthentication();
        webApp.UseAuthorization();

        _mcpTransportAuth?.MapEntraOAuthDiscoveryEndpoints(webApp);

        webApp.MapControllers();
        webApp.MapGet("/health", () =>
        {
            Console.WriteLine("[HEALTH] Health check called");
            return new { status = "healthy", timestamp = DateTime.UtcNow };
        });

        Console.WriteLine("[STARTUP] Middleware configured");
        Console.WriteLine("[STARTUP] All services registered and configured");
        Console.WriteLine("[STARTUP] Application starting on http://localhost:5080");
    }

    private static void AddControllersAndApiExploration(IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
    }

    private void AddPersistence(IServiceCollection services)
    {
        services.AddDbContext<McpGatewayDbContext>(options =>
            options.UseNpgsql(_configuration.GetConnectionString("DefaultConnection")));

        Console.WriteLine($"[CONFIG] Database connection configured: {_configuration.GetConnectionString("DefaultConnection")}");
    }

    private static void AddMapster(IServiceCollection services)
    {
        var mapsterConfig = TypeAdapterConfig.GlobalSettings;
        new DtoMappingRegister().Register(mapsterConfig);
        new EntityMappingRegister().Register(mapsterConfig);
        services.AddSingleton(mapsterConfig);
        services.AddScoped<IMapper, ServiceMapper>();
    }

    private static void AddApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IMcpAdapterService, McpAdapterService>();
        services.AddScoped<SseEndpointRewriter>();
        services.AddScoped<SseProxyStream>();
        services.AddScoped<IProxyService, ProxyService>();
        services.AddScoped<ExcelService>();
        services.AddScoped<IMcpAdapterRepository, McpAdapterRepository>();
    }

    private void AddAuth(IServiceCollection services)
    {
        services.AddAuthProviders(_configuration);
        _mcpTransportAuth = services.AddMcpTransportAuthentication(_configuration, _environment);
    }

    private static void AddLogging(IServiceCollection services)
    {
        services.AddLogging(config =>
        {
            config.ClearProviders();
            config.AddConsole();
            config.AddDebug();
            config.SetMinimumLevel(LogLevel.Information);
        });
        Console.WriteLine("[LOGGING] Console and Debug logging configured");
    }

    private static void AddCors(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
    }

    private static void AddSwagger(IServiceCollection services) =>
        services.AddSwaggerGen();

    private static void ApplyDatabaseMigrations(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<McpGatewayDbContext>();
        context.Database.Migrate();
        Console.WriteLine("[DB] Database migrations applied");
    }
}
