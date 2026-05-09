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

- **.NET 10 SDK** (see `TargetFramework` in `backend/src/**/*.csproj`)
- **Node.js 20+** and npm (for the Angular UI under `frontend/`)
- **PostgreSQL 12+**, or **Docker** / **Docker Compose** to run the database and optional full stack
- Visual Studio 2022 or VS Code (optional)

## How to run

### Option A: Docker Compose (PostgreSQL + API + Angular UI)

From the repository root:

1. Copy the environment template and adjust values (especially `JWT_SECRET_KEY` for anything beyond local dev):

   ```bash
   cp .env.example .env
   ```

2. Start all services:

   ```bash
   docker compose up -d
   ```

3. **UI:** [http://localhost:4201](http://localhost:4201) (default `FRONTEND_HOST_PORT`; nginx serves the SPA and proxies `/api`, `/adapters`, and `/health` to the API).
4. **API only:** [http://localhost:8070](http://localhost:8070) (default `BACKEND_HOST_PORT`; the process still listens on **8080 inside the container**).

If you change the host port mapped to the UI, set `API_URL` in `.env` to the same origin you use in the browser (for example `http://localhost:4201`), so MCP client URLs built in the SPA stay correct.

Host port overrides (optional): `POSTGRES_HOST_PORT` (default **5434** on the host → **5432** in the container), `BACKEND_HOST_PORT` (default **8070**), `FRONTEND_HOST_PORT` (default **4201**); see `.env.example`. From your machine, Postgres is at **localhost:5434** when Compose is running with the defaults.

**Postgres `28P01` / “password authentication failed”:** the data volume was initialized with different `DB_USER` / `DB_PASSWORD` than in your `.env` (the official image only applies `POSTGRES_*` on the first empty data directory). Fix by either setting `DB_*` to match that first init, or removing the volume and recreating: `docker compose down -v` then `docker compose up -d` (this **wipes the database**).

To build only the API image (from repo root): `docker build -f backend/Dockerfile ./backend`

### Option B: Local development (backend + frontend)

1. **Database:** Run PostgreSQL locally and create a database named **`mcp_gateway`**, or override `ConnectionStrings__DefaultConnection` (defaults are in `backend/src/McpGateway/appsettings.json`).
2. **Backend** (from repo root):

   ```bash
   dotnet run --project backend/src/McpGateway/McpGateway.csproj
   ```

   By default the HTTP profile listens at **http://localhost:5080** (`Properties/launchSettings.json`).

3. **Frontend:**

   ```bash
   cd frontend
   npm install
   npm start
   ```

   The dev server is typically **http://localhost:4200**. Point the UI at your API (`environment` files and/or `public/assets/config/config.json`) so it matches the backend URL (often `http://localhost:5080`).

### Tests

```bash
dotnet test backend/src/McpGateway.sln
```

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd andy-mcp-gateway
```

### 2. Database Setup

#### Option A: Using Docker (PostgreSQL only)

```bash
docker run --name postgres-mcpgateway -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=mcp_gateway -p 5432:5432 -d postgres:16-alpine
```

#### Option B: Local PostgreSQL Installation

1. Install PostgreSQL on your system
2. Create a database named **`mcp_gateway`**
3. Update the connection string in `appsettings.json` if needed

### 3. Configure Connection String

Update the connection string in `backend/src/McpGateway/appsettings.json` if your database name or credentials differ.

### 4. Build and Run

```bash
dotnet build backend/src/McpGateway.sln
dotnet run --project backend/src/McpGateway/McpGateway.csproj
```

The API defaults (HTTP profile) are:
- HTTP: `http://localhost:5080`
- HTTPS profile (if used): `https://localhost:7082` and `http://localhost:5080`
- Swagger UI: available when `ASPNETCORE_ENVIRONMENT=Development` (e.g. `http://localhost:5080/swagger`)

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
backend/src/
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

frontend/                       # Angular SPA (management UI)
```

### Adding New Features

1. **Domain Layer**: Add entities, models, and interfaces
2. **Application Layer**: Create DTOs, services, and mappings
3. **Infrastructure Layer**: Implement repositories and external services
4. **Presentation Layer**: Add controllers and endpoints

### Database Migrations

```bash
# Add migration
dotnet ef migrations add MigrationName --project backend/src/McpGateway.Infrastructure --startup-project backend/src/McpGateway

# Update database
dotnet ef database update --project backend/src/McpGateway.Infrastructure --startup-project backend/src/McpGateway
```

## Testing

### Unit Tests

```bash
dotnet test backend/src/McpGateway.sln
```

### Integration Tests

```bash
dotnet test --filter Category=Integration
```

## Deployment

### Docker Deployment

Use **`backend/Dockerfile`** with **`docker compose up`** (see [How to run](#how-to-run)). The image targets **.NET 10** and listens on **8080** inside the container.

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
- Built with .NET 10 and Clean Architecture principles
- Uses PostgreSQL for data persistence







