using Andy.Mcp.Gateway.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Andy.Mcp.Gateway.Tests.Services;

/// <summary>
/// Loader tests use a temporary file per case so they don't conflict in
/// parallel runs. Hot-reload assertion uses Task.Delay to wait out the
/// debounce window — short and bounded.
/// </summary>
public sealed class ServiceMapFileLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public ServiceMapFileLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "mg1-loader-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    private (ServiceMapFileLoader loader, InMemoryServiceMapRegistry registry, string filePath)
        BuildLoader(string yamlContent)
    {
        var filePath = Path.Combine(_tempDir, "servicemap.yaml");
        File.WriteAllText(filePath, yamlContent);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceMap:File"] = filePath,
            })
            .Build();

        var registry = new InMemoryServiceMapRegistry();
        var loader = new ServiceMapFileLoader(registry, config, NullLogger<ServiceMapFileLoader>.Instance);
        return (loader, registry, filePath);
    }

    [Fact]
    public async Task StartAsync_LoadsValidYaml()
    {
        var (loader, registry, _) = BuildLoader(
            """
            services:
              - serviceId: svc-a
                localUrl: https://localhost:1111
                remoteUrlPattern: https://svc-a.{tenantSlug}.rivoli.ai
                requiresAuth: true
              - serviceId: cloud-only
                localUrl: null
                remoteUrlPattern: https://cloud.example.com
                requiresAuth: false
            """);

        await loader.StartAsync(CancellationToken.None);

        Assert.Equal(2, registry.Entries.Count);
        var svcA = registry.Find("svc-a")!;
        Assert.Equal("https://localhost:1111", svcA.LocalUrl);
        Assert.True(svcA.RequiresAuth);
        var cloudOnly = registry.Find("cloud-only")!;
        Assert.Null(cloudOnly.LocalUrl);
        Assert.False(cloudOnly.RequiresAuth);

        await loader.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_TreatsBlankLocalUrlAsNull()
    {
        var (loader, registry, _) = BuildLoader(
            """
            services:
              - serviceId: svc
                localUrl: "   "
                remoteUrlPattern: https://x
                requiresAuth: false
            """);

        await loader.StartAsync(CancellationToken.None);

        Assert.Null(registry.Find("svc")!.LocalUrl);

        await loader.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_KeepsPreviousSnapshot_OnInvalidYaml()
    {
        var (loader, registry, filePath) = BuildLoader(
            """
            services:
              - serviceId: good
                localUrl: http://l
                remoteUrlPattern: http://r
                requiresAuth: true
            """);

        await loader.StartAsync(CancellationToken.None);
        Assert.Single(registry.Entries);

        // Overwrite with invalid YAML and trigger a reload by waiting for
        // the watcher. The loader should log + skip; previous snapshot stays.
        File.WriteAllText(filePath, "this: is: not: valid: yaml: [");
        await Task.Delay(500); // debounce + watcher event

        // Either the registry kept the original or got cleared. Loader's
        // contract is "keep previous on parse error", so we expect the
        // original entry to still be there.
        Assert.Single(registry.Entries);
        Assert.Equal("good", registry.Entries[0].ServiceId);

        await loader.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_EmptyMapWhenFileMissing()
    {
        var filePath = Path.Combine(_tempDir, "missing.yaml");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceMap:File"] = filePath,
            })
            .Build();
        var registry = new InMemoryServiceMapRegistry();
        var loader = new ServiceMapFileLoader(registry, config, NullLogger<ServiceMapFileLoader>.Instance);

        await loader.StartAsync(CancellationToken.None);

        Assert.Empty(registry.Entries);

        await loader.StopAsync(CancellationToken.None);
    }
}
