using Andy.Mcp.Gateway.Models;
using Andy.Mcp.Gateway.Services;

namespace Andy.Mcp.Gateway.Tests.Services;

public sealed class InMemoryServiceMapRegistryTests
{
    [Fact]
    public void Find_IsCaseInsensitive()
    {
        var registry = new InMemoryServiceMapRegistry();
        registry.Replace(new[]
        {
            new ServiceMapEntry("Andy-Containers", "http://local", "http://remote", true),
        });

        Assert.NotNull(registry.Find("andy-containers"));
        Assert.NotNull(registry.Find("ANDY-CONTAINERS"));
        Assert.Null(registry.Find("missing"));
    }

    [Fact]
    public void Replace_FiresChangedEvent()
    {
        var registry = new InMemoryServiceMapRegistry();
        IReadOnlyList<ServiceMapEntry>? observed = null;
        registry.Changed += (_, e) => observed = e.Entries;

        var entries = new[]
        {
            new ServiceMapEntry("svc", "http://l", null, false),
        };
        registry.Replace(entries);

        Assert.NotNull(observed);
        Assert.Single(observed!);
    }

    [Fact]
    public void Replace_AtomicallySwapsSnapshot()
    {
        // Reads against an old snapshot must stay consistent even when a
        // writer races with a Replace. Capture two snapshots and assert
        // they're independently iterable.
        var registry = new InMemoryServiceMapRegistry();
        registry.Replace(new[] { new ServiceMapEntry("a", "http://1", null, false) });
        var snapshotA = registry.Entries;

        registry.Replace(new[] { new ServiceMapEntry("b", "http://2", null, false) });
        var snapshotB = registry.Entries;

        Assert.Equal("a", snapshotA[0].ServiceId);
        Assert.Equal("b", snapshotB[0].ServiceId);
    }
}
