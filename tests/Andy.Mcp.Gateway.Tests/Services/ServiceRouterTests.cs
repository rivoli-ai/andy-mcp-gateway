using Andy.Mcp.Gateway.Models;
using Andy.Mcp.Gateway.Services;

namespace Andy.Mcp.Gateway.Tests.Services;

public sealed class ServiceRouterTests
{
    private readonly InMemoryServiceMapRegistry _registry = new();
    private readonly StubHealthMonitor _health = new();

    private IServiceRouter NewRouter() => new ServiceRouter(_registry, _health);

    [Fact]
    public async Task ResolveAsync_PrefersLocal_WhenLocalUrlSetAndHealthy()
    {
        _registry.Replace(new[]
        {
            new ServiceMapEntry("svc-a", "https://localhost:1111", "https://remote.example", true),
        });
        _health.MarkHealthy("svc-a");

        var route = await NewRouter().ResolveAsync("svc-a", CancellationToken.None);

        Assert.Equal(RouteSource.Local, route.Source);
        Assert.Equal("https://localhost:1111", route.TargetUrl);
    }

    [Fact]
    public async Task ResolveAsync_FallsBackToRemote_WhenLocalUnhealthy()
    {
        _registry.Replace(new[]
        {
            new ServiceMapEntry("svc-a", "https://localhost:1111", "https://remote.example", true),
        });
        // No MarkHealthy → defaults to unhealthy.

        var route = await NewRouter().ResolveAsync("svc-a", CancellationToken.None);

        Assert.Equal(RouteSource.Remote, route.Source);
        Assert.Equal("https://remote.example", route.TargetUrl);
    }

    [Fact]
    public async Task ResolveAsync_FallsBackToRemote_WhenLocalUrlMissing()
    {
        _registry.Replace(new[]
        {
            new ServiceMapEntry("cloud-only", null, "https://remote.example", true),
        });
        // Health monitor is irrelevant here — there's no local URL to probe.

        var route = await NewRouter().ResolveAsync("cloud-only", CancellationToken.None);

        Assert.Equal(RouteSource.Remote, route.Source);
        Assert.Equal("https://remote.example", route.TargetUrl);
    }

    [Fact]
    public async Task ResolveAsync_PreservesTenantPlaceholder()
    {
        // MG1 returns the remote pattern verbatim; MG3 will substitute
        // {tenantSlug} from the bound tenant. This test pins that contract.
        _registry.Replace(new[]
        {
            new ServiceMapEntry("svc-a", null, "https://svc-a.{tenantSlug}.rivoli.ai", true),
        });

        var route = await NewRouter().ResolveAsync("svc-a", CancellationToken.None);

        Assert.Equal(RouteSource.Remote, route.Source);
        Assert.Contains("{tenantSlug}", route.TargetUrl);
    }

    [Fact]
    public async Task ResolveAsync_Throws_WhenNeitherLocalNorRemoteAvailable()
    {
        _registry.Replace(new[]
        {
            new ServiceMapEntry("local-only", "https://localhost:9999", null, true),
        });
        // Local missing AND no remote → service unavailable.

        await Assert.ThrowsAsync<ServiceUnavailableException>(
            () => NewRouter().ResolveAsync("local-only", CancellationToken.None));
    }

    [Fact]
    public async Task ResolveAsync_Throws_WhenServiceNotInMap()
    {
        _registry.Replace(Array.Empty<ServiceMapEntry>());

        await Assert.ThrowsAsync<ServiceNotInMapException>(
            () => NewRouter().ResolveAsync("nope", CancellationToken.None));
    }

    private sealed class StubHealthMonitor : IRouteHealthMonitor
    {
        private readonly HashSet<string> _healthy = new(StringComparer.OrdinalIgnoreCase);
        public void MarkHealthy(string serviceId) => _healthy.Add(serviceId);
        public bool IsLocalHealthy(string serviceId) => _healthy.Contains(serviceId);
    }
}
