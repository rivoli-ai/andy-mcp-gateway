using McpGateway.Application.Interfaces;
using McpGateway.Application.Services;
using McpGateway.Application.Mapping;
using McpGateway.Domain.Interfaces;
using McpGateway.Infrastructure.Data;
using McpGateway.Infrastructure.Repositories;
using McpGateway.Infrastructure.Mapping;
using Microsoft.EntityFrameworkCore;
using McpGateway;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using ModelContextProtocol.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for streaming requests
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    serverOptions.Limits.MinRequestBodyDataRate = null;
    serverOptions.Limits.MinResponseDataRate = null;
    
    Console.WriteLine("[KESTREL] Configured with 10 minute timeouts for streaming");
});

// Add services to the container
builder.Services.AddControllers();

// Add Entity Framework with PostgreSQL
builder.Services.AddDbContext<McpGatewayDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

Console.WriteLine($"[CONFIG] Database connection configured: {builder.Configuration.GetConnectionString("DefaultConnection")}");

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(DtosMapperProfile), typeof(EntityMapperProfile));

// Add Application Services
builder.Services.AddScoped<IMcpAdapterService, McpAdapterService>();
builder.Services.AddScoped<IProxyService, ProxyService>();
builder.Services.AddScoped<ExcelService>();

// Add Repositories
builder.Services.AddScoped<IMcpAdapterRepository, McpAdapterRepository>();

// Add HTTP Client
builder.Services.AddHttpClient();

var azureAdConfig = builder.Configuration.GetSection("AzureAd");
builder.Services.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddScheme<McpAuthenticationOptions, McpSubPathAwareAuthenticationHandler>(
    McpAuthenticationDefaults.AuthenticationScheme,
    McpAuthenticationDefaults.DisplayName,
    options =>
    {
        options.ResourceMetadata = new()
        {
            Resource = new Uri(builder.Configuration.GetValue<string>("PublicOrigin")!),
            AuthorizationServers = { new Uri($"https://login.microsoftonline.com/{azureAdConfig["TenantId"]}/v2.0") },
            ScopesSupported = ["api://andy-back/Api.Access"]
        };
    })
.AddMicrosoftIdentityWebApi(azureAdConfig);

// Add logging with console output
builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.AddDebug();
    config.SetMinimumLevel(LogLevel.Information);
});

Console.WriteLine("[LOGGING] Console and Debug logging configured");

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

Console.WriteLine("[APP] Application built successfully");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
    Console.WriteLine("[ENV] Running in Development mode");
}

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<McpGatewayDbContext>();
    context.Database.Migrate();
    Console.WriteLine("[DB] Database migrations applied");
}

app.UseRouting();
app.UseCors();
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();

Console.WriteLine("[STARTUP] Middleware configured");

// Health check endpoint
app.MapGet("/health", () => 
{
    Console.WriteLine("[HEALTH] Health check called");
    return new { status = "healthy", timestamp = DateTime.UtcNow };
});

Console.WriteLine("[STARTUP] All services registered and configured");
Console.WriteLine("[STARTUP] Application starting on http://localhost:5080");

app.Run();

public class McpServerMappingConfiguration
{
    public const string SectionName = "McpServerMappings";

    public Dictionary<string, string> Servers { get; set; } = new();

    public void AddMapping(string name, string url)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Name and URL cannot be empty");

        Servers[name.ToLowerInvariant()] = url;
    }

    public bool TryGetUrl(string name, out string? url)
    {
        return Servers.TryGetValue(name.ToLowerInvariant(), out url);
    }

    public IEnumerable<string> GetServerNames() => Servers.Keys;
}