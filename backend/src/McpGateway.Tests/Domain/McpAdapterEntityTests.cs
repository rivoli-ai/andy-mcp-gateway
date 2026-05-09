using FluentAssertions;
using McpGateway.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Xunit;

namespace McpGateway.Tests.Domain;

/// <summary>
/// Unit tests for the McpAdapterEntity domain entity.
/// </summary>
public class McpAdapterEntityTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var entity = new McpAdapterEntity();

        // Assert
        entity.Id.Should().Be(Guid.Empty);
        entity.Name.Should().BeEmpty();
        entity.Url.Should().BeEmpty();
        entity.Description.Should().BeNull();
        entity.TimeoutSeconds.Should().Be(30);
        entity.Enabled.Should().BeTrue();
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.CreatedBy.Should().BeNull();
        entity.UpdatedBy.Should().BeNull();
        entity.LastHealthCheck.Should().BeNull();
        entity.IsHealthy.Should().BeFalse();
        entity.LastResponseTimeMs.Should().BeNull();
        entity.LastError.Should().BeNull();
    }

    [Fact]
    public void Id_ShouldHaveKeyAttribute()
    {
        // Arrange
        var property = typeof(McpAdapterEntity).GetProperty(nameof(McpAdapterEntity.Id));

        // Act & Assert
        property.Should().NotBeNull();
        property!.GetCustomAttributes(typeof(KeyAttribute), false).Should().HaveCount(1);
    }

    [Fact]
    public void Name_ShouldHaveRequiredAndMaxLengthAttributes()
    {
        // Arrange
        var property = typeof(McpAdapterEntity).GetProperty(nameof(McpAdapterEntity.Name));

        // Act & Assert
        property.Should().NotBeNull();
        property!.GetCustomAttributes(typeof(RequiredAttribute), false).Should().HaveCount(1);
        property.GetCustomAttributes(typeof(MaxLengthAttribute), false).Should().HaveCount(1);
        
        var maxLengthAttr = property.GetCustomAttributes(typeof(MaxLengthAttribute), false).First() as MaxLengthAttribute;
        maxLengthAttr!.Length.Should().Be(100);
    }

    [Fact]
    public void Url_ShouldHaveRequiredAndMaxLengthAttributes()
    {
        // Arrange
        var property = typeof(McpAdapterEntity).GetProperty(nameof(McpAdapterEntity.Url));

        // Act & Assert
        property.Should().NotBeNull();
        property!.GetCustomAttributes(typeof(RequiredAttribute), false).Should().HaveCount(1);
        property.GetCustomAttributes(typeof(MaxLengthAttribute), false).Should().HaveCount(1);
        
        var maxLengthAttr = property.GetCustomAttributes(typeof(MaxLengthAttribute), false).First() as MaxLengthAttribute;
        maxLengthAttr!.Length.Should().Be(500);
    }

    [Fact]
    public void Description_ShouldHaveMaxLengthAttribute()
    {
        // Arrange
        var property = typeof(McpAdapterEntity).GetProperty(nameof(McpAdapterEntity.Description));

        // Act & Assert
        property.Should().NotBeNull();
        property!.GetCustomAttributes(typeof(MaxLengthAttribute), false).Should().HaveCount(1);
        
        var maxLengthAttr = property.GetCustomAttributes(typeof(MaxLengthAttribute), false).First() as MaxLengthAttribute;
        maxLengthAttr!.Length.Should().Be(1000);
    }

    [Fact]
    public void CreatedBy_ShouldHaveMaxLengthAttribute()
    {
        // Arrange
        var property = typeof(McpAdapterEntity).GetProperty(nameof(McpAdapterEntity.CreatedBy));

        // Act & Assert
        property.Should().NotBeNull();
        property!.GetCustomAttributes(typeof(MaxLengthAttribute), false).Should().HaveCount(1);
        
        var maxLengthAttr = property.GetCustomAttributes(typeof(MaxLengthAttribute), false).First() as MaxLengthAttribute;
        maxLengthAttr!.Length.Should().Be(100);
    }

    [Fact]
    public void UpdatedBy_ShouldHaveMaxLengthAttribute()
    {
        // Arrange
        var property = typeof(McpAdapterEntity).GetProperty(nameof(McpAdapterEntity.UpdatedBy));

        // Act & Assert
        property.Should().NotBeNull();
        property!.GetCustomAttributes(typeof(MaxLengthAttribute), false).Should().HaveCount(1);
        
        var maxLengthAttr = property.GetCustomAttributes(typeof(MaxLengthAttribute), false).First() as MaxLengthAttribute;
        maxLengthAttr!.Length.Should().Be(100);
    }

    [Fact]
    public void LastError_ShouldHaveMaxLengthAttribute()
    {
        // Arrange
        var property = typeof(McpAdapterEntity).GetProperty(nameof(McpAdapterEntity.LastError));

        // Act & Assert
        property.Should().NotBeNull();
        property!.GetCustomAttributes(typeof(MaxLengthAttribute), false).Should().HaveCount(1);
        
        var maxLengthAttr = property.GetCustomAttributes(typeof(MaxLengthAttribute), false).First() as MaxLengthAttribute;
        maxLengthAttr!.Length.Should().Be(1000);
    }

    [Fact]
    public void Class_ShouldHaveTableAttribute()
    {
        // Arrange
        var type = typeof(McpAdapterEntity);

        // Act & Assert
        type.GetCustomAttributes(typeof(TableAttribute), false).Should().HaveCount(1);
        
        var tableAttr = type.GetCustomAttributes(typeof(TableAttribute), false).First() as TableAttribute;
        tableAttr!.Name.Should().Be("mcp_adapters");
    }

    [Theory]
    [InlineData("Test Adapter", "http://localhost:3000", "Test Description", 60, true)]
    [InlineData("Another Adapter", "https://api.example.com", null, 30, false)]
    public void Properties_ShouldAcceptValidValues(string name, string url, string? description, int timeout, bool enabled)
    {
        // Arrange
        var entity = new McpAdapterEntity();

        // Act
        entity.Name = name;
        entity.Url = url;
        entity.Description = description;
        entity.TimeoutSeconds = timeout;
        entity.Enabled = enabled;

        // Assert
        entity.Name.Should().Be(name);
        entity.Url.Should().Be(url);
        entity.Description.Should().Be(description);
        entity.TimeoutSeconds.Should().Be(timeout);
        entity.Enabled.Should().Be(enabled);
    }
}
