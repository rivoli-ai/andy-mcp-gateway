using FluentAssertions;
using McpGateway.Domain.Models;
using Xunit;

namespace McpGateway.Tests.Domain;

public class McpAdapterTests
{
    [Fact]
    public void Constructor_initialises_default_values()
    {
        var adapter = new McpAdapter();

        adapter.Id.Should().Be(Guid.Empty);
        adapter.Name.Should().BeEmpty();
        adapter.Url.Should().BeEmpty();
        adapter.Description.Should().BeNull();
        adapter.TimeoutSeconds.Should().Be(30);
        adapter.Enabled.Should().BeTrue();
        adapter.CreatedAt.Should().Be(default);
        adapter.UpdatedAt.Should().Be(default);
        adapter.CreatedBy.Should().BeNull();
        adapter.UpdatedBy.Should().BeNull();
        adapter.LastHealthCheck.Should().BeNull();
        adapter.IsHealthy.Should().BeFalse();
        adapter.LastResponseTimeMs.Should().BeNull();
        adapter.LastError.Should().BeNull();
    }

    [Fact]
    public void MarkAsUpdated_stamps_timestamp_and_user()
    {
        var adapter = new McpAdapter();
        var before = DateTime.UtcNow;

        adapter.MarkAsUpdated("test-user");

        adapter.UpdatedAt.Should().BeOnOrAfter(before);
        adapter.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        adapter.UpdatedBy.Should().Be("test-user");
    }

    [Fact]
    public void MarkAsUpdated_without_user_leaves_user_null()
    {
        var adapter = new McpAdapter();
        var before = DateTime.UtcNow;

        adapter.MarkAsUpdated();

        adapter.UpdatedAt.Should().BeOnOrAfter(before);
        adapter.UpdatedBy.Should().BeNull();
    }
}
