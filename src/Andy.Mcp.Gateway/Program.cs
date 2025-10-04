using Andy.Mcp.Gateway.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MCP Gateway Registry API",
        Version = "v1",
        Description = "API for managing MCP (Model Context Protocol) gateway registrations",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Rivoli.AI",
            Url = new Uri("https://github.com/rivoli-ai/andy-mcp-gateway")
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "Apache 2.0",
            Url = new Uri("https://www.apache.org/licenses/LICENSE-2.0")
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Register services
builder.Services.AddSingleton<IGatewayRegistryService, InMemoryGatewayRegistryService>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP Gateway Registry API v1");
    options.RoutePrefix = "swagger"; // Serve Swagger UI at /swagger
});

// Serve static files (index.html)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program { }
