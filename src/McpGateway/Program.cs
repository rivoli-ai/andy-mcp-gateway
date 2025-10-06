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

// Add services to the container
builder.Services.AddControllers();

// Add Entity Framework with PostgreSQL
builder.Services.AddDbContext<McpGatewayDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(DtosMapperProfile), typeof(EntityMapperProfile));

// Add Application Services
builder.Services.AddScoped<IMcpAdapterService, McpAdapterService>();
builder.Services.AddScoped<IProxyService, ProxyService>();

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

// Add logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<McpGatewayDbContext>();
    context.Database.Migrate();
}

app.UseRouting();
app.UseCors();
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

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