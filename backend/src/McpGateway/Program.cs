using Mapster;
using MapsterMapper;
using McpGateway.Application.Interfaces;
using McpGateway.Application.Mapping;
using McpGateway.Application.Proxying;
using McpGateway.Application.Services;
using McpGateway.Domain.Interfaces;
using McpGateway.Infrastructure.Data;
using McpGateway.Infrastructure.Mapping;
using McpGateway.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using McpGateway;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using McpGateway.Auth;

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

// Mapster (DTO ↔ domain ↔ entity mappings)
var mapsterConfig = TypeAdapterConfig.GlobalSettings;
new DtoMappingRegister().Register(mapsterConfig);
new EntityMappingRegister().Register(mapsterConfig);
builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();

// Add Application Services
builder.Services.AddScoped<IMcpAdapterService, McpAdapterService>();
builder.Services.AddScoped<SseEndpointRewriter>();
builder.Services.AddScoped<SseProxyStream>();
builder.Services.AddScoped<IProxyService, ProxyService>();
builder.Services.AddScoped<ExcelService>();

// Add Repositories
builder.Services.AddScoped<IMcpAdapterRepository, McpAdapterRepository>();

// Add HTTP Client
builder.Services.AddHttpClient();

builder.Services.AddAuthProviders(builder.Configuration);

// App JWT authentication (DevPilot-style): gateway issues its own JWT after validating external OIDC tokens.
var secretKey = builder.Configuration["JWT:SecretKey"];
if (string.IsNullOrWhiteSpace(secretKey))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("JWT:SecretKey must be configured.");
    secretKey = "dev-secret-key-min-32-characters-long-for-security";
}

var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"] ?? "McpGateway",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JWT:Audience"] ?? "McpGateway",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

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