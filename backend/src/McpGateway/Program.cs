using McpGateway;

var builder = WebApplication.CreateBuilder(args);

Startup.ConfigureWebHost(builder.WebHost);

var startup = new Startup(builder.Configuration, builder.Environment);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
Console.WriteLine("[APP] Application built successfully");

startup.Configure(app, app.Environment);
app.Run();
