# MCP Gateway Registry Examples

This directory contains example applications demonstrating how to interact with the MCP Gateway Registry API.

## Example Client Application

The example client (`Andy.Mcp.Gateway.Examples`) demonstrates all the main operations of the MCP Gateway Registry API:

- Creating gateway registrations
- Retrieving gateways by ID
- Searching for gateways
- Updating gateway information
- Deleting gateways

### Running the Example

1. **Start the MCP Gateway Registry API**:
   ```bash
   cd ../../src/Andy.Mcp.Gateway
   dotnet run
   ```

   The API will start on `https://localhost:5001` by default.

2. **Run the example client** (in a new terminal):
   ```bash
   cd examples/Andy.Mcp.Gateway.Examples
   dotnet run
   ```

   To use a custom API URL:
   ```bash
   dotnet run -- https://your-api-url.com
   ```

### Example Output

The example application will:
1. Create a new gateway registration
2. Retrieve it by ID
3. Update its properties
4. Create additional gateways
5. List all gateways
6. Search by tags and search terms
7. Delete the example gateway

You'll see output similar to:

```
=== MCP Gateway Registry Client Example ===

Using default URL: https://localhost:5001/api/GatewayRegistry

1. Creating a new gateway...
✓ Created gateway: Example Gateway (ID: 123e4567-e89b-12d3-a456-426614174000)
  Endpoint: https://example-gateway.com/api
  Status: Active
  Tags: example, demo, test

...
```

## API Endpoints

The example demonstrates the following endpoints:

- `GET /api/GatewayRegistry` - Get all gateways
- `GET /api/GatewayRegistry/{id}` - Get a specific gateway
- `POST /api/GatewayRegistry` - Create a new gateway
- `POST /api/GatewayRegistry/search` - Search gateways
- `PUT /api/GatewayRegistry/{id}` - Update a gateway
- `DELETE /api/GatewayRegistry/{id}` - Delete a gateway

## Using the API in Your Application

The example code shows how to:

```csharp
using System.Net.Http.Json;
using Andy.Mcp.Gateway.Models;

var client = new HttpClient();
var baseUrl = "https://your-api-url/api/GatewayRegistry";

// Create a gateway
var createRequest = new CreateGatewayRequest
{
    Name = "My Gateway",
    Description = "Gateway description",
    Endpoint = "https://gateway.example.com/api",
    Version = "1.0.0",
    Tags = new List<string> { "tag1", "tag2" }
};

var response = await client.PostAsJsonAsync(baseUrl, createRequest);
var gateway = await response.Content.ReadFromJsonAsync<McpGateway>();

// Search gateways
var searchQuery = new GatewaySearchQuery
{
    SearchTerm = "keyword",
    Tags = new List<string> { "tag1" },
    Status = GatewayStatus.Active
};

var searchResponse = await client.PostAsJsonAsync($"{baseUrl}/search", searchQuery);
var results = await searchResponse.Content.ReadFromJsonAsync<IEnumerable<McpGateway>>();
```
