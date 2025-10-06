# MCP Gateway

A clean architecture implementation of an MCP (Model Context Protocol) Gateway that provides reverse proxy functionality and session-aware routing for MCP servers without using containers.

## Overview

This project implements a gateway service similar to Microsoft's [mcp-gateway](https://github.com/microsoft/mcp-gateway) but with a focus on clean architecture principles and non-containerized deployment. The gateway acts as a reverse proxy and management layer for MCP servers, enabling scalable, session-aware routing and lifecycle management.

## Architecture

The project follows Clean Architecture principles with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│  • Controllers (API Endpoints)                             │
│  • Middleware                                              │
│  • Swagger Documentation                                   │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                         │
│  • DTOs (Data Transfer Objects)                            │
│  • Application Services                                     │
│  • Application Interfaces                                  │
│  • DTO ↔ BO Mapping (AutoMapper)                          │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                     Domain Layer                            │
│  • Entities (Database Models)                              │
│  • Business Objects (BOs)                                  │
│  • Enums                                                   │
│  • Domain Interfaces (Repository Contracts)                │
│  • Domain Logic                                            │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                       │
│  • Repository Implementations                              │
│  • Database Context (Entity Framework)                     │
│  • External Service Integrations                           │
│  • Entity ↔ BO Mapping (AutoMapper)                       │
│  • HTTP Client Services                                    │
└─────────────────────────────────────────────────────────────┘
```

## Features

- **Reverse Proxy**: Routes client requests to appropriate MCP servers
- **Session Management**: Session-aware routing with state management
- **Server Management**: CRUD operations for MCP server registration
- **Health Monitoring**: Automatic health checks for registered servers
- **Load Balancing**: Distributes requests across available servers
- **Clean Architecture**: Maintainable and testable code structure
- **PostgreSQL Support**: Uses PostgreSQL as the database backend
- **RESTful API**: Comprehensive API for server and session management

## Prerequisites

- .NET 9.0 SDK
- PostgreSQL 12+ (or Docker for PostgreSQL)
- Visual Studio 2022 or VS Code (optional)

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd GateWay
```

### 2. Database Setup

#### Option A: Using Docker (Recommended)

```bash
docker run --name postgres-mcpgateway -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=mcpgateway -p 5432:5432 -d postgres:15
```

#### Option B: Local PostgreSQL Installation

1. Install PostgreSQL on your system
2. Create a database named `mcpgateway`
3. Update the connection string in `appsettings.json` if needed

### 3. Configure Connection String

Update the connection string in `src/McpGateway/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mcpgateway;Username=postgres;Password=postgres"
  }
}
```

### 4. Build and Run

```bash
dotnet build
dotnet run --project src/McpGateway
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## API Endpoints

### MCP Servers Management

- `GET /api/mcpservers` - Get all MCP servers
- `GET /api/mcpservers/active` - Get active MCP servers
- `GET /api/mcpservers/{id}` - Get server by ID
- `POST /api/mcpservers` - Create new MCP server
- `PUT /api/mcpservers/{id}` - Update MCP server
- `DELETE /api/mcpservers/{id}` - Delete MCP server
- `POST /api/mcpservers/{id}/activate` - Activate server
- `POST /api/mcpservers/{id}/deactivate` - Deactivate server

### Session Management

- `POST /api/proxy/sessions` - Create new session
- `GET /api/proxy/sessions/{sessionId}` - Get session details
- `DELETE /api/proxy/sessions/{sessionId}` - End session
- `GET /api/proxy/sessions/{sessionId}/validate` - Validate session
- `POST /api/proxy/sessions/{sessionId}/proxy` - Proxy request to MCP server

## Usage Examples

### 1. Register an MCP Server

```bash
curl -X POST "https://localhost:5001/api/mcpservers" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "my-mcp-server",
    "url": "http://localhost:8080",
    "description": "My custom MCP server",
    "maxConcurrentSessions": 50,
    "tags": "custom,production"
  }'
```

### 2. Create a Session

```bash
curl -X POST "https://localhost:5001/api/proxy/sessions" \
  -H "Content-Type: application/json" \
  -d '{
    "mcpServerId": "server-guid-here",
    "clientId": "my-client"
  }'
```

### 3. Proxy a Request

```bash
curl -X POST "https://localhost:5001/api/proxy/sessions/{sessionId}/proxy" \
  -H "Content-Type: application/json" \
  -d '{
    "method": "GET",
    "path": "/api/status",
    "headers": {
      "Accept": "application/json"
    }
  }'
```

## Configuration

### Environment Variables

- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string
- `ASPNETCORE_ENVIRONMENT` - Environment (Development, Production)

### App Settings

The application uses `appsettings.json` for configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mcpgateway;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## Development

### Project Structure

```
src/
├── McpGateway.Domain/          # Domain layer
│   ├── Entities/               # Database entities
│   ├── Models/                 # Business objects
│   ├── Interfaces/             # Repository contracts
│   └── Enums/                  # Domain enums
├── McpGateway.Application/     # Application layer
│   ├── DTOs/                   # Data transfer objects
│   ├── Interfaces/             # Service contracts
│   ├── Services/               # Application services
│   └── Mapping/                # AutoMapper profiles
├── McpGateway.Infrastructure/  # Infrastructure layer
│   ├── Data/                   # Database context
│   ├── Repositories/           # Repository implementations
│   ├── Services/               # External services
│   └── Mapping/                # Entity mappings
└── McpGateway/                 # Presentation layer
    ├── Controllers/            # API controllers
    ├── Program.cs              # Application entry point
    └── appsettings.json        # Configuration
```

### Adding New Features

1. **Domain Layer**: Add entities, models, and interfaces
2. **Application Layer**: Create DTOs, services, and mappings
3. **Infrastructure Layer**: Implement repositories and external services
4. **Presentation Layer**: Add controllers and endpoints

### Database Migrations

```bash
# Add migration
dotnet ef migrations add MigrationName --project src/McpGateway.Infrastructure --startup-project src/McpGateway

# Update database
dotnet ef database update --project src/McpGateway.Infrastructure --startup-project src/McpGateway
```

## Testing

### Unit Tests

```bash
dotnet test
```

### Integration Tests

```bash
dotnet test --filter Category=Integration
```

## Deployment

### Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet build -c Release

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "McpGateway.dll"]
```

### Production Considerations

1. **Database**: Use managed PostgreSQL service (AWS RDS, Azure Database, etc.)
2. **Security**: Implement authentication and authorization
3. **Monitoring**: Add application insights and logging
4. **Scaling**: Configure load balancing and auto-scaling
5. **SSL/TLS**: Use HTTPS in production

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Inspired by Microsoft's [mcp-gateway](https://github.com/microsoft/mcp-gateway)
- Built with .NET 9.0 and Clean Architecture principles
- Uses PostgreSQL for data persistence







