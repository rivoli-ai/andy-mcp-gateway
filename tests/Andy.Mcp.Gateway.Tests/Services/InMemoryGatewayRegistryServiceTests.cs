using Andy.Mcp.Gateway.Models;
using Andy.Mcp.Gateway.Services;

namespace Andy.Mcp.Gateway.Tests.Services;

public class InMemoryGatewayRegistryServiceTests
{
    private readonly InMemoryGatewayRegistryService _service;

    public InMemoryGatewayRegistryServiceTests()
    {
        _service = new InMemoryGatewayRegistryService();
    }

    [Fact]
    public async Task CreateGatewayAsync_ShouldCreateGatewayWithGeneratedId()
    {
        // Arrange
        var request = new CreateGatewayRequest
        {
            Name = "Test Gateway",
            Description = "Test Description",
            Endpoint = "https://test.example.com",
            Version = "1.0.0",
            Tags = new List<string> { "test", "example" }
        };

        // Act
        var gateway = await _service.CreateGatewayAsync(request);

        // Assert
        Assert.NotNull(gateway);
        Assert.False(string.IsNullOrEmpty(gateway.Id));
        Assert.Equal(request.Name, gateway.Name);
        Assert.Equal(request.Description, gateway.Description);
        Assert.Equal(request.Endpoint, gateway.Endpoint);
        Assert.Equal(request.Version, gateway.Version);
        Assert.Equal(GatewayStatus.Active, gateway.Status);
        Assert.Equal(2, gateway.Tags.Count);
    }

    [Fact]
    public async Task GetGatewayByIdAsync_ShouldReturnGateway_WhenExists()
    {
        // Arrange
        var request = new CreateGatewayRequest
        {
            Name = "Test Gateway",
            Endpoint = "https://test.example.com"
        };
        var created = await _service.CreateGatewayAsync(request);

        // Act
        var retrieved = await _service.GetGatewayByIdAsync(created.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal(created.Name, retrieved.Name);
    }

    [Fact]
    public async Task GetGatewayByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var retrieved = await _service.GetGatewayByIdAsync("nonexistent-id");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetAllGatewaysAsync_ShouldReturnAllGateways()
    {
        // Arrange
        await _service.CreateGatewayAsync(new CreateGatewayRequest
        {
            Name = "Gateway 1",
            Endpoint = "https://gateway1.example.com"
        });
        await _service.CreateGatewayAsync(new CreateGatewayRequest
        {
            Name = "Gateway 2",
            Endpoint = "https://gateway2.example.com"
        });

        // Act
        var gateways = await _service.GetAllGatewaysAsync();

        // Assert
        Assert.NotNull(gateways);
        Assert.Equal(2, gateways.Count());
    }

    [Fact]
    public async Task UpdateGatewayAsync_ShouldUpdateGateway_WhenExists()
    {
        // Arrange
        var created = await _service.CreateGatewayAsync(new CreateGatewayRequest
        {
            Name = "Original Name",
            Description = "Original Description",
            Endpoint = "https://original.example.com"
        });

        var updateRequest = new UpdateGatewayRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
            Status = GatewayStatus.Maintenance
        };

        // Act
        var updated = await _service.UpdateGatewayAsync(created.Id, updateRequest);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("Updated Description", updated.Description);
        Assert.Equal(GatewayStatus.Maintenance, updated.Status);
        Assert.Equal(created.Endpoint, updated.Endpoint); // Should remain unchanged
    }

    [Fact]
    public async Task UpdateGatewayAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var updateRequest = new UpdateGatewayRequest
        {
            Name = "Updated Name"
        };

        // Act
        var updated = await _service.UpdateGatewayAsync("nonexistent-id", updateRequest);

        // Assert
        Assert.Null(updated);
    }

    [Fact]
    public async Task DeleteGatewayAsync_ShouldDeleteGateway_WhenExists()
    {
        // Arrange
        var created = await _service.CreateGatewayAsync(new CreateGatewayRequest
        {
            Name = "Test Gateway",
            Endpoint = "https://test.example.com"
        });

        // Act
        var deleted = await _service.DeleteGatewayAsync(created.Id);
        var retrieved = await _service.GetGatewayByIdAsync(created.Id);

        // Assert
        Assert.True(deleted);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteGatewayAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Act
        var deleted = await _service.DeleteGatewayAsync("nonexistent-id");

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public async Task SearchGatewaysAsync_ShouldFilterBySearchTerm()
    {
        // Arrange
        await _service.CreateGatewayAsync(new CreateGatewayRequest
        {
            Name = "Test Gateway",
            Description = "A test gateway",
            Endpoint = "https://test.example.com"
        });
        await _service.CreateGatewayAsync(new CreateGatewayRequest
        {
            Name = "Production Gateway",
            Description = "Production gateway",
            Endpoint = "https://prod.example.com"
        });

        // Act
        var query = new GatewaySearchQuery { SearchTerm = "test" };
        var results = await _service.SearchGatewaysAsync(query);

        // Assert
        Assert.Single(results);
        Assert.Contains("Test", results.First().Name);
    }

    [Fact]
    public async Task SearchGatewaysAsync_ShouldFilterByTags()
    {
        // Arrange
        await _service.CreateGatewayAsync(new CreateGatewayRequest
        {
            Name = "Gateway 1",
            Endpoint = "https://gateway1.example.com",
            Tags = new List<string> { "tag1", "tag2" }
        });
        await _service.CreateGatewayAsync(new CreateGatewayRequest
        {
            Name = "Gateway 2",
            Endpoint = "https://gateway2.example.com",
            Tags = new List<string> { "tag3" }
        });

        // Act
        var query = new GatewaySearchQuery { Tags = new List<string> { "tag1" } };
        var results = await _service.SearchGatewaysAsync(query);

        // Assert
        Assert.Single(results);
        Assert.Contains("Gateway 1", results.First().Name);
    }

    [Fact]
    public async Task SearchGatewaysAsync_ShouldFilterByStatus()
    {
        // Arrange
        var gateway1 = await _service.CreateGatewayAsync(new CreateGatewayRequest
        {
            Name = "Active Gateway",
            Endpoint = "https://active.example.com"
        });

        var gateway2 = await _service.CreateGatewayAsync(new CreateGatewayRequest
        {
            Name = "Inactive Gateway",
            Endpoint = "https://inactive.example.com"
        });

        await _service.UpdateGatewayAsync(gateway2.Id, new UpdateGatewayRequest
        {
            Status = GatewayStatus.Inactive
        });

        // Act
        var query = new GatewaySearchQuery { Status = GatewayStatus.Active };
        var results = await _service.SearchGatewaysAsync(query);

        // Assert
        Assert.Single(results);
        Assert.Equal(GatewayStatus.Active, results.First().Status);
    }

    [Fact]
    public async Task SearchGatewaysAsync_ShouldApplyPagination()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            await _service.CreateGatewayAsync(new CreateGatewayRequest
            {
                Name = $"Gateway {i}",
                Endpoint = $"https://gateway{i}.example.com"
            });
        }

        // Act
        var query = new GatewaySearchQuery { Page = 1, PageSize = 2 };
        var results = await _service.SearchGatewaysAsync(query);

        // Assert
        Assert.Equal(2, results.Count());
    }
}
