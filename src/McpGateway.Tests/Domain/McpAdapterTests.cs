using FluentAssertions;
using McpGateway.Domain.Models;
using Xunit;

namespace McpGateway.Tests.Domain;

/// <summary>
/// Unit tests for the McpAdapter domain model.
/// </summary>
public class McpAdapterTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var adapter = new McpAdapter();

        // Assert
        adapter.Id.Should().Be(Guid.Empty);
        adapter.Name.Should().BeEmpty();
        adapter.Url.Should().BeEmpty();
        adapter.Description.Should().BeNull();
        adapter.TimeoutSeconds.Should().Be(30);
        adapter.Enabled.Should().BeTrue();
        adapter.CreatedAt.Should().Be(default(DateTime));
        adapter.UpdatedAt.Should().Be(default(DateTime));
        adapter.CreatedBy.Should().BeNull();
        adapter.UpdatedBy.Should().BeNull();
        adapter.LastHealthCheck.Should().BeNull();
        adapter.IsHealthy.Should().BeFalse();
        adapter.LastResponseTimeMs.Should().BeNull();
        adapter.LastError.Should().BeNull();
    }

    [Fact]
    public void IsReachable_WhenEnabledAndHealthy_ShouldReturnTrue()
    {
        // Arrange
        var adapter = new McpAdapter
        {
            Enabled = true,
            IsHealthy = true
        };

        // Act
        var result = adapter.IsReachable();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsReachable_WhenDisabled_ShouldReturnFalse()
    {
        // Arrange
        var adapter = new McpAdapter
        {
            Enabled = false,
            IsHealthy = true
        };

        // Act
        var result = adapter.IsReachable();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsReachable_WhenEnabledButUnhealthy_ShouldReturnFalse()
    {
        // Arrange
        var adapter = new McpAdapter
        {
            Enabled = true,
            IsHealthy = false
        };

        // Act
        var result = adapter.IsReachable();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateHealthStatus_WithHealthyStatus_ShouldUpdateProperties()
    {
        // Arrange
        var adapter = new McpAdapter();
        var responseTime = 150;
        var error = "Test error";

        // Act
        adapter.UpdateHealthStatus(true, responseTime, error);

        // Assert
        adapter.IsHealthy.Should().BeTrue();
        adapter.LastHealthCheck.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        adapter.LastResponseTimeMs.Should().Be(responseTime);
        adapter.LastError.Should().Be(error);
    }

    [Fact]
    public void UpdateHealthStatus_WithUnhealthyStatus_ShouldUpdateProperties()
    {
        // Arrange
        var adapter = new McpAdapter();
        var responseTime = 5000;
        var error = "Connection timeout";

        // Act
        adapter.UpdateHealthStatus(false, responseTime, error);

        // Assert
        adapter.IsHealthy.Should().BeFalse();
        adapter.LastHealthCheck.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        adapter.LastResponseTimeMs.Should().Be(responseTime);
        adapter.LastError.Should().Be(error);
    }

    [Fact]
    public void UpdateHealthStatus_WithoutOptionalParameters_ShouldUpdateRequiredProperties()
    {
        // Arrange
        var adapter = new McpAdapter();

        // Act
        adapter.UpdateHealthStatus(false);

        // Assert
        adapter.IsHealthy.Should().BeFalse();
        adapter.LastHealthCheck.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        adapter.LastResponseTimeMs.Should().BeNull();
        adapter.LastError.Should().BeNull();
    }

    [Fact]
    public void MarkAsUpdated_WithUpdatedBy_ShouldUpdateTimestampAndUser()
    {
        // Arrange
        var adapter = new McpAdapter();
        var updatedBy = "test-user";
        var beforeUpdate = DateTime.UtcNow;

        // Act
        adapter.MarkAsUpdated(updatedBy);

        // Assert
        adapter.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
        adapter.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        adapter.UpdatedBy.Should().Be(updatedBy);
    }

    [Fact]
    public void MarkAsUpdated_WithoutUpdatedBy_ShouldUpdateTimestampOnly()
    {
        // Arrange
        var adapter = new McpAdapter();
        var beforeUpdate = DateTime.UtcNow;

        // Act
        adapter.MarkAsUpdated();

        // Assert
        adapter.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
        adapter.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        adapter.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public void MarkAsUpdated_WithNullUpdatedBy_ShouldUpdateTimestampOnly()
    {
        // Arrange
        var adapter = new McpAdapter();
        var beforeUpdate = DateTime.UtcNow;

        // Act
        adapter.MarkAsUpdated(null);

        // Assert
        adapter.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
        adapter.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        adapter.UpdatedBy.Should().BeNull();
    }
}






