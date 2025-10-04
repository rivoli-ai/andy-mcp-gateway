using System.Net.Http.Json;
using Andy.Mcp.Gateway.Models;

namespace Andy.Mcp.Gateway.Examples;

/// <summary>
/// Example client demonstrating how to interact with the MCP Gateway Registry API
/// </summary>
class Program
{
    private static readonly HttpClient client = new HttpClient();
    private static string baseUrl = "https://localhost:5001/api/GatewayRegistry";

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== MCP Gateway Registry Client Example ===\n");

        // Allow user to specify a custom URL
        if (args.Length > 0)
        {
            baseUrl = args[0].TrimEnd('/') + "/api/GatewayRegistry";
            Console.WriteLine($"Using custom URL: {baseUrl}\n");
        }
        else
        {
            Console.WriteLine($"Using default URL: {baseUrl}");
            Console.WriteLine("(Pass a custom URL as a command line argument to override)\n");
        }

        // Disable SSL certificate validation for local development
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        using var httpClient = new HttpClient(handler);

        try
        {
            // 1. Create a new gateway
            Console.WriteLine("1. Creating a new gateway...");
            var newGateway = await CreateGatewayAsync(httpClient, new CreateGatewayRequest
            {
                Name = "Example Gateway",
                Description = "An example MCP gateway for demonstration",
                Endpoint = "https://example-gateway.com/api",
                Version = "1.0.0",
                Tags = new List<string> { "example", "demo", "test" },
                Metadata = new Dictionary<string, string>
                {
                    { "author", "Rivoli.AI" },
                    { "license", "Apache-2.0" }
                }
            });

            if (newGateway != null)
            {
                Console.WriteLine($"✓ Created gateway: {newGateway.Name} (ID: {newGateway.Id})");
                Console.WriteLine($"  Endpoint: {newGateway.Endpoint}");
                Console.WriteLine($"  Status: {newGateway.Status}");
                Console.WriteLine($"  Tags: {string.Join(", ", newGateway.Tags)}\n");

                // 2. Get gateway by ID
                Console.WriteLine("2. Retrieving gateway by ID...");
                var retrievedGateway = await GetGatewayByIdAsync(httpClient, newGateway.Id);
                if (retrievedGateway != null)
                {
                    Console.WriteLine($"✓ Retrieved: {retrievedGateway.Name}\n");
                }

                // 3. Update gateway
                Console.WriteLine("3. Updating gateway status...");
                var updatedGateway = await UpdateGatewayAsync(httpClient, newGateway.Id, new UpdateGatewayRequest
                {
                    Description = "Updated example MCP gateway",
                    Status = GatewayStatus.Active
                });
                if (updatedGateway != null)
                {
                    Console.WriteLine($"✓ Updated: {updatedGateway.Name}");
                    Console.WriteLine($"  New description: {updatedGateway.Description}\n");
                }

                // Create a few more gateways for search demonstration
                Console.WriteLine("4. Creating additional gateways for search demo...");
                await CreateGatewayAsync(httpClient, new CreateGatewayRequest
                {
                    Name = "Production Gateway",
                    Description = "Production MCP gateway",
                    Endpoint = "https://prod-gateway.com/api",
                    Version = "2.0.0",
                    Tags = new List<string> { "production", "stable" }
                });

                await CreateGatewayAsync(httpClient, new CreateGatewayRequest
                {
                    Name = "Development Gateway",
                    Description = "Development testing gateway",
                    Endpoint = "https://dev-gateway.com/api",
                    Version = "0.9.0",
                    Tags = new List<string> { "development", "test" }
                });
                Console.WriteLine("✓ Additional gateways created\n");

                // 5. Get all gateways
                Console.WriteLine("5. Retrieving all gateways...");
                var allGateways = await GetAllGatewaysAsync(httpClient);
                Console.WriteLine($"✓ Total gateways: {allGateways?.Count() ?? 0}");
                foreach (var gw in allGateways ?? Enumerable.Empty<McpGateway>())
                {
                    Console.WriteLine($"  - {gw.Name} ({gw.Version})");
                }
                Console.WriteLine();

                // 6. Search gateways
                Console.WriteLine("6. Searching for gateways with tag 'test'...");
                var searchResults = await SearchGatewaysAsync(httpClient, new GatewaySearchQuery
                {
                    Tags = new List<string> { "test" }
                });
                Console.WriteLine($"✓ Found {searchResults?.Count() ?? 0} gateways:");
                foreach (var gw in searchResults ?? Enumerable.Empty<McpGateway>())
                {
                    Console.WriteLine($"  - {gw.Name}");
                }
                Console.WriteLine();

                // 7. Search by term
                Console.WriteLine("7. Searching for gateways containing 'production'...");
                var termResults = await SearchGatewaysAsync(httpClient, new GatewaySearchQuery
                {
                    SearchTerm = "production"
                });
                Console.WriteLine($"✓ Found {termResults?.Count() ?? 0} gateways:");
                foreach (var gw in termResults ?? Enumerable.Empty<McpGateway>())
                {
                    Console.WriteLine($"  - {gw.Name}: {gw.Description}");
                }
                Console.WriteLine();

                // 8. Delete gateway
                Console.WriteLine("8. Deleting the example gateway...");
                var deleted = await DeleteGatewayAsync(httpClient, newGateway.Id);
                if (deleted)
                {
                    Console.WriteLine($"✓ Deleted gateway: {newGateway.Id}\n");
                }

                // 9. Verify deletion
                Console.WriteLine("9. Verifying deletion...");
                var deletedGateway = await GetGatewayByIdAsync(httpClient, newGateway.Id);
                if (deletedGateway == null)
                {
                    Console.WriteLine("✓ Gateway successfully deleted\n");
                }
            }

            Console.WriteLine("=== Example completed successfully! ===");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"\n❌ Error: Unable to connect to the API.");
            Console.WriteLine($"   Make sure the MCP Gateway Registry API is running at {baseUrl}");
            Console.WriteLine($"   Details: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
        }
    }

    static async Task<McpGateway?> CreateGatewayAsync(HttpClient client, CreateGatewayRequest request)
    {
        var response = await client.PostAsJsonAsync(baseUrl, request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<McpGateway>();
    }

    static async Task<McpGateway?> GetGatewayByIdAsync(HttpClient client, string id)
    {
        try
        {
            var response = await client.GetAsync($"{baseUrl}/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<McpGateway>();
        }
        catch
        {
            return null;
        }
    }

    static async Task<IEnumerable<McpGateway>?> GetAllGatewaysAsync(HttpClient client)
    {
        var response = await client.GetAsync(baseUrl);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<McpGateway>>();
    }

    static async Task<IEnumerable<McpGateway>?> SearchGatewaysAsync(HttpClient client, GatewaySearchQuery query)
    {
        var response = await client.PostAsJsonAsync($"{baseUrl}/search", query);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<McpGateway>>();
    }

    static async Task<McpGateway?> UpdateGatewayAsync(HttpClient client, string id, UpdateGatewayRequest request)
    {
        var response = await client.PutAsJsonAsync($"{baseUrl}/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<McpGateway>();
    }

    static async Task<bool> DeleteGatewayAsync(HttpClient client, string id)
    {
        var response = await client.DeleteAsync($"{baseUrl}/{id}");
        return response.IsSuccessStatusCode;
    }
}
