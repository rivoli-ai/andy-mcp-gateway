using System.Net;
using System.Net.Http.Json;
using Andy.Mcp.Gateway.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Andy.Mcp.Gateway.Tests.Integration;

public class GatewayRegistryApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GatewayRegistryApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllGateways_ShouldReturnEmptyList_WhenNoGatewaysExist()
    {
        // Act
        var response = await _client.GetAsync("/api/GatewayRegistry");
        var gateways = await response.Content.ReadFromJsonAsync<List<McpGateway>>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(gateways);
    }

    [Fact]
    public async Task CreateGateway_ShouldReturnCreatedGateway()
    {
        // Arrange
        var request = new CreateGatewayRequest
        {
            Name = "Test Gateway",
            Description = "Test Description",
            Endpoint = "https://test.example.com",
            Version = "1.0.0",
            Tags = new List<string> { "test" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/GatewayRegistry", request);
        var gateway = await response.Content.ReadFromJsonAsync<McpGateway>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(gateway);
        Assert.False(string.IsNullOrEmpty(gateway.Id));
        Assert.Equal(request.Name, gateway.Name);
        Assert.Equal(request.Description, gateway.Description);
        Assert.Equal(request.Endpoint, gateway.Endpoint);
        Assert.Equal(request.Version, gateway.Version);
    }

    [Fact]
    public async Task CreateGateway_ShouldReturnBadRequest_WhenNameIsMissing()
    {
        // Arrange
        var request = new CreateGatewayRequest
        {
            Endpoint = "https://test.example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/GatewayRegistry", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetGatewayById_ShouldReturnGateway_WhenExists()
    {
        // Arrange
        var createRequest = new CreateGatewayRequest
        {
            Name = "Test Gateway",
            Endpoint = "https://test.example.com"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/GatewayRegistry", createRequest);
        var createdGateway = await createResponse.Content.ReadFromJsonAsync<McpGateway>();

        // Act
        var response = await _client.GetAsync($"/api/GatewayRegistry/{createdGateway!.Id}");
        var gateway = await response.Content.ReadFromJsonAsync<McpGateway>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(gateway);
        Assert.Equal(createdGateway.Id, gateway.Id);
        Assert.Equal(createdGateway.Name, gateway.Name);
    }

    [Fact]
    public async Task GetGatewayById_ShouldReturnNotFound_WhenNotExists()
    {
        // Act
        var response = await _client.GetAsync("/api/GatewayRegistry/nonexistent-id");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGateway_ShouldUpdateGateway_WhenExists()
    {
        // Arrange
        var createRequest = new CreateGatewayRequest
        {
            Name = "Original Name",
            Endpoint = "https://test.example.com"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/GatewayRegistry", createRequest);
        var createdGateway = await createResponse.Content.ReadFromJsonAsync<McpGateway>();

        var updateRequest = new UpdateGatewayRequest
        {
            Name = "Updated Name",
            Status = GatewayStatus.Maintenance
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/GatewayRegistry/{createdGateway!.Id}",
            updateRequest);
        var updatedGateway = await response.Content.ReadFromJsonAsync<McpGateway>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(updatedGateway);
        Assert.Equal("Updated Name", updatedGateway.Name);
        Assert.Equal(GatewayStatus.Maintenance, updatedGateway.Status);
    }

    [Fact]
    public async Task UpdateGateway_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        var updateRequest = new UpdateGatewayRequest
        {
            Name = "Updated Name"
        };

        // Act
        var response = await _client.PutAsJsonAsync(
            "/api/GatewayRegistry/nonexistent-id",
            updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGateway_ShouldDeleteGateway_WhenExists()
    {
        // Arrange
        var createRequest = new CreateGatewayRequest
        {
            Name = "Test Gateway",
            Endpoint = "https://test.example.com"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/GatewayRegistry", createRequest);
        var createdGateway = await createResponse.Content.ReadFromJsonAsync<McpGateway>();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/GatewayRegistry/{createdGateway!.Id}");
        var getResponse = await _client.GetAsync($"/api/GatewayRegistry/{createdGateway.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteGateway_ShouldReturnNotFound_WhenNotExists()
    {
        // Act
        var response = await _client.DeleteAsync("/api/GatewayRegistry/nonexistent-id");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SearchGateways_ShouldReturnFilteredResults()
    {
        // Arrange
        await _client.PostAsJsonAsync("/api/GatewayRegistry", new CreateGatewayRequest
        {
            Name = "Test Gateway",
            Description = "A test gateway",
            Endpoint = "https://test.example.com",
            Tags = new List<string> { "test" }
        });

        await _client.PostAsJsonAsync("/api/GatewayRegistry", new CreateGatewayRequest
        {
            Name = "Production Gateway",
            Description = "Production gateway",
            Endpoint = "https://prod.example.com",
            Tags = new List<string> { "production" }
        });

        var searchQuery = new GatewaySearchQuery
        {
            SearchTerm = "test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/GatewayRegistry/search", searchQuery);
        var results = await response.Content.ReadFromJsonAsync<List<McpGateway>>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(results);
        Assert.All(results, g =>
            Assert.True(g.Name.Contains("Test", StringComparison.OrdinalIgnoreCase) ||
                       g.Description.Contains("test", StringComparison.OrdinalIgnoreCase)));
    }
}
