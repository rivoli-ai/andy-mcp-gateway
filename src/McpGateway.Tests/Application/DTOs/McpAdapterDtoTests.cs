using FluentAssertions;
using McpGateway.Application.DTOs;
using Xunit;

namespace McpGateway.Tests.Application.DTOs;

/// <summary>
/// Unit tests for the McpAdapterDto data transfer object.
/// </summary>
public class McpAdapterDtoTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var dto = new McpAdapterDto();

        // Assert
        dto.Id.Should().Be(Guid.Empty);
        dto.Name.Should().BeEmpty();
        dto.Url.Should().BeEmpty();
        dto.Description.Should().BeNull();
        dto.TimeoutSeconds.Should().Be(0);
        dto.Enabled.Should().BeFalse();
        dto.CreatedAt.Should().Be(default(DateTime));
        dto.UpdatedAt.Should().Be(default(DateTime));
        dto.CreatedBy.Should().BeNull();
        dto.UpdatedBy.Should().BeNull();
        dto.LastHealthCheck.Should().BeNull();
        dto.IsHealthy.Should().BeFalse();
        dto.LastResponseTimeMs.Should().BeNull();
        dto.LastError.Should().BeNull();
        dto.Status.Should().Be("unknown");
    }

    [Theory]
    [InlineData("Test Adapter", "http://localhost:3000", "Test Description", 60, true, "healthy")]
    [InlineData("Another Adapter", "https://api.example.com", null, 30, false, "disabled")]
    public void Properties_ShouldAcceptValidValues(string name, string url, string? description, int timeout, bool enabled, string status)
    {
        // Arrange
        var dto = new McpAdapterDto();
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        dto.Id = id;
        dto.Name = name;
        dto.Url = url;
        dto.Description = description;
        dto.TimeoutSeconds = timeout;
        dto.Enabled = enabled;
        dto.Status = status;
        dto.CreatedAt = now;
        dto.UpdatedAt = now;
        dto.CreatedBy = "test-user";
        dto.UpdatedBy = "test-user";
        dto.LastHealthCheck = now;
        dto.IsHealthy = true;
        dto.LastResponseTimeMs = 150;
        dto.LastError = "Test error";

        // Assert
        dto.Id.Should().Be(id);
        dto.Name.Should().Be(name);
        dto.Url.Should().Be(url);
        dto.Description.Should().Be(description);
        dto.TimeoutSeconds.Should().Be(timeout);
        dto.Enabled.Should().Be(enabled);
        dto.Status.Should().Be(status);
        dto.CreatedAt.Should().Be(now);
        dto.UpdatedAt.Should().Be(now);
        dto.CreatedBy.Should().Be("test-user");
        dto.UpdatedBy.Should().Be("test-user");
        dto.LastHealthCheck.Should().Be(now);
        dto.IsHealthy.Should().BeTrue();
        dto.LastResponseTimeMs.Should().Be(150);
        dto.LastError.Should().Be("Test error");
    }
}

/// <summary>
/// Unit tests for the CreateMcpAdapterDto data transfer object.
/// </summary>
public class CreateMcpAdapterDtoTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var dto = new CreateMcpAdapterDto();

        // Assert
        dto.Name.Should().BeEmpty();
        dto.Url.Should().BeEmpty();
        dto.Description.Should().BeNull();
        dto.TimeoutSeconds.Should().Be(30);
        dto.Enabled.Should().BeTrue();
        dto.CreatedBy.Should().BeNull();
    }

    [Theory]
    [InlineData("Test Adapter", "http://localhost:3000", "Test Description", 60, true, "test-user")]
    [InlineData("Another Adapter", "https://api.example.com", null, 30, false, null)]
    public void Properties_ShouldAcceptValidValues(string name, string url, string? description, int timeout, bool enabled, string? createdBy)
    {
        // Arrange
        var dto = new CreateMcpAdapterDto();

        // Act
        dto.Name = name;
        dto.Url = url;
        dto.Description = description;
        dto.TimeoutSeconds = timeout;
        dto.Enabled = enabled;
        dto.CreatedBy = createdBy;

        // Assert
        dto.Name.Should().Be(name);
        dto.Url.Should().Be(url);
        dto.Description.Should().Be(description);
        dto.TimeoutSeconds.Should().Be(timeout);
        dto.Enabled.Should().Be(enabled);
        dto.CreatedBy.Should().Be(createdBy);
    }
}

/// <summary>
/// Unit tests for the UpdateMcpAdapterDto data transfer object.
/// </summary>
public class UpdateMcpAdapterDtoTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var dto = new UpdateMcpAdapterDto();

        // Assert
        dto.Name.Should().BeNull();
        dto.Url.Should().BeNull();
        dto.Description.Should().BeNull();
        dto.TimeoutSeconds.Should().BeNull();
        dto.Enabled.Should().BeNull();
        dto.UpdatedBy.Should().BeNull();
    }

    [Theory]
    [InlineData("Updated Adapter", "http://localhost:3001", "Updated Description", 90, true, "updated-user")]
    [InlineData(null, null, null, null, null, null)]
    public void Properties_ShouldAcceptValidValues(string? name, string? url, string? description, int? timeout, bool? enabled, string? updatedBy)
    {
        // Arrange
        var dto = new UpdateMcpAdapterDto();

        // Act
        dto.Name = name;
        dto.Url = url;
        dto.Description = description;
        dto.TimeoutSeconds = timeout;
        dto.Enabled = enabled;
        dto.UpdatedBy = updatedBy;

        // Assert
        dto.Name.Should().Be(name);
        dto.Url.Should().Be(url);
        dto.Description.Should().Be(description);
        dto.TimeoutSeconds.Should().Be(timeout);
        dto.Enabled.Should().Be(enabled);
        dto.UpdatedBy.Should().Be(updatedBy);
    }
}

/// <summary>
/// Unit tests for the AdapterHealthDto data transfer object.
/// </summary>
public class AdapterHealthDtoTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var dto = new AdapterHealthDto();

        // Assert
        dto.Id.Should().Be(Guid.Empty);
        dto.Name.Should().BeEmpty();
        dto.Url.Should().BeEmpty();
        dto.Status.Should().Be("unknown");
        dto.LastCheck.Should().BeNull();
        dto.ResponseTimeMs.Should().BeNull();
        dto.LastError.Should().BeNull();
    }

    [Theory]
    [InlineData("Test Adapter", "http://localhost:3000", "healthy", 150, "No errors")]
    [InlineData("Another Adapter", "https://api.example.com", "unhealthy", 5000, "Connection timeout")]
    public void Properties_ShouldAcceptValidValues(string name, string url, string status, int? responseTime, string? lastError)
    {
        // Arrange
        var dto = new AdapterHealthDto();
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        dto.Id = id;
        dto.Name = name;
        dto.Url = url;
        dto.Status = status;
        dto.LastCheck = now;
        dto.ResponseTimeMs = responseTime;
        dto.LastError = lastError;

        // Assert
        dto.Id.Should().Be(id);
        dto.Name.Should().Be(name);
        dto.Url.Should().Be(url);
        dto.Status.Should().Be(status);
        dto.LastCheck.Should().Be(now);
        dto.ResponseTimeMs.Should().Be(responseTime);
        dto.LastError.Should().Be(lastError);
    }
}

/// <summary>
/// Unit tests for the AdapterListDto data transfer object.
/// </summary>
public class AdapterListDtoTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var dto = new AdapterListDto();

        // Assert
        dto.Adapters.Should().NotBeNull();
        dto.Adapters.Should().BeEmpty();
        dto.Total.Should().Be(0);
        dto.Healthy.Should().Be(0);
        dto.Unhealthy.Should().Be(0);
        dto.Disabled.Should().Be(0);
    }

    [Fact]
    public void Properties_ShouldAcceptValidValues()
    {
        // Arrange
        var dto = new AdapterListDto();
        var adapters = new List<McpAdapterDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Adapter 1", IsHealthy = true, Enabled = true },
            new() { Id = Guid.NewGuid(), Name = "Adapter 2", IsHealthy = false, Enabled = true },
            new() { Id = Guid.NewGuid(), Name = "Adapter 3", IsHealthy = true, Enabled = false }
        };

        // Act
        dto.Adapters = adapters;
        dto.Total = 3;
        dto.Healthy = 1;
        dto.Unhealthy = 1;
        dto.Disabled = 1;

        // Assert
        dto.Adapters.Should().HaveCount(3);
        dto.Total.Should().Be(3);
        dto.Healthy.Should().Be(1);
        dto.Unhealthy.Should().Be(1);
        dto.Disabled.Should().Be(1);
    }
}






