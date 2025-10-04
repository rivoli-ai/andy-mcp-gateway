# Andy.Mcp.Gateway

> ⚠️ **ALPHA RELEASE WARNING** ⚠️
>
> This software is in ALPHA stage. **NO GUARANTEES** are made about its functionality, stability, or safety.
> The authors assume **NO RESPONSIBILITY** for data loss, system damage, or security breaches.
>
> **USE AT YOUR OWN RISK**

An ASP.NET Core web application that provides a registry service for MCP (Model Context Protocol) gateways. This service allows you to register, search, update, and manage MCP gateway endpoints.

## Features

- **RESTful API** for managing MCP gateway registrations
- **Search functionality** with support for:
  - Text search across gateway names and descriptions
  - Tag-based filtering
  - Status filtering
  - Pagination
- **Swagger/OpenAPI documentation** for easy API exploration
- **In-memory storage** with thread-safe operations
- **Comprehensive test coverage** with unit and integration tests

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Your favorite IDE (Visual Studio, VS Code, Rider, etc.)

### Running the Application

1. **Clone the repository**:
   ```bash
   git clone https://github.com/rivoli-ai/andy-mcp-gateway.git
   cd andy-mcp-gateway
   ```

2. **Build the solution**:
   ```bash
   dotnet build Andy.Mcp.Gateway.sln
   ```

3. **Run the web application**:
   ```bash
   dotnet run --project src/Andy.Mcp.Gateway
   ```

4. **Access the API**:
   - API: `https://localhost:5001/api/GatewayRegistry`
   - Swagger UI: `https://localhost:5001` (in development mode)

### Running Tests

```bash
dotnet test Andy.Mcp.Gateway.sln
```

For coverage reports:
```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
reportgenerator -reports:"./TestResults/*/coverage.cobertura.xml" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html
```

### Running the Example Client

See the [examples/README.md](examples/README.md) for detailed instructions on running the example client application.

Quick start:
```bash
# Terminal 1: Start the API
dotnet run --project src/Andy.Mcp.Gateway

# Terminal 2: Run the example
dotnet run --project examples/Andy.Mcp.Gateway.Examples
```

## API Endpoints

### Gateway Management

- `GET /api/GatewayRegistry` - Get all gateways
- `GET /api/GatewayRegistry/{id}` - Get a specific gateway by ID
- `POST /api/GatewayRegistry` - Create a new gateway registration
- `PUT /api/GatewayRegistry/{id}` - Update an existing gateway
- `DELETE /api/GatewayRegistry/{id}` - Delete a gateway
- `POST /api/GatewayRegistry/search` - Search gateways with filters

### Example Request: Create Gateway

```bash
curl -X POST https://localhost:5001/api/GatewayRegistry \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Example Gateway",
    "description": "An example MCP gateway",
    "endpoint": "https://example.com/api",
    "version": "1.0.0",
    "tags": ["example", "demo"],
    "metadata": {
      "author": "Rivoli.AI"
    }
  }'
```

### Example Request: Search Gateways

```bash
curl -X POST https://localhost:5001/api/GatewayRegistry/search \
  -H "Content-Type: application/json" \
  -d '{
    "searchTerm": "example",
    "tags": ["demo"],
    "status": "Active",
    "page": 1,
    "pageSize": 20
  }'
```

## Project Structure

```
andy-mcp-gateway/
├── src/
│   └── Andy.Mcp.Gateway/          # ASP.NET Core web application
│       ├── Controllers/            # API controllers
│       ├── Models/                 # Data models and DTOs
│       ├── Services/               # Business logic and services
│       └── Program.cs              # Application entry point
├── tests/
│   └── Andy.Mcp.Gateway.Tests/    # Unit and integration tests
│       ├── Services/               # Service unit tests
│       └── Integration/            # API integration tests
└── examples/
    └── Andy.Mcp.Gateway.Examples/ # Example client application
```

## Technology Stack

- **Framework**: ASP.NET Core 9.0
- **API Documentation**: Swagger/OpenAPI
- **Testing**: xUnit, ASP.NET Core Testing
- **Storage**: In-memory (thread-safe)

## Contributing

This project follows standard .NET development practices:
- Write tests for new features
- Run `dotnet format` before committing
- Ensure all tests pass with `dotnet test`

## License

Apache License 2.0 - See LICENSE file for details
