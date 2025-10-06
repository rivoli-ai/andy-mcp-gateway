# MCP Gateway - Clean Architecture Implementation

## Overview

This project has been refactored to follow Clean Architecture principles as outlined in `backend-architecture.md`. The original monolithic `Controller.cs` has been split into two specialized controllers with proper separation of concerns.

## Architecture Changes

### Before (Monolithic)
- Single `Controller.cs` file with all functionality
- No separation of concerns
- Direct database access from controllers
- No proper layering

### After (Clean Architecture)
- **AdaptersController**: Manages MCP adapter CRUD operations
- **ProxyController**: Handles request forwarding and proxying
- Proper layering: Domain → Application → Infrastructure → Presentation
- PostgreSQL database with Entity Framework
- AutoMapper for object mapping
- Dependency injection throughout

## New Controllers

### AdaptersController (`/api/adapters`)
- `GET /` - Get all adapters
- `GET /enabled` - Get enabled adapters only
- `GET /{id}` - Get adapter by ID
- `GET /name/{name}` - Get adapter by name
- `POST /` - Create new adapter
- `PUT /{id}` - Update adapter
- `DELETE /{id}` - Delete adapter
- `GET /{id}/health` - Check adapter health
- `POST /health-check` - Check all adapters health
- `GET /search` - Search adapters
- `POST /reload` - Reload mappings

### ProxyController (`/api/proxy`)
- `POST /{adapterName}/messages` - Forward messages
- `POST /{adapterName}/{*method}` - Forward generic methods
- `GET /{adapterName}/sse` - Establish SSE connection
- `GET /{adapterName}/available` - Check adapter availability

## Database Schema

### McpAdapterEntity
```sql
CREATE TABLE mcp_adapters (
    id UUID PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    url VARCHAR(500) NOT NULL,
    description VARCHAR(1000),
    timeout_seconds INTEGER DEFAULT 30,
    enabled BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    last_health_check TIMESTAMP,
    is_healthy BOOLEAN DEFAULT false,
    last_response_time_ms INTEGER,
    last_error VARCHAR(1000)
);
```

## Setup Instructions

### 1. Database Setup
```bash
# Run the setup script
psql -U postgres -f src/McpGateway/scripts/setup-database.sql
```

### 2. Environment Configuration
Update `appsettings.json` with your PostgreSQL connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=mcpgateway;Username=agentic_user;Password=agentic_password;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;"
  }
}
```

### 3. Run the Application
```bash
cd src/McpGateway
dotnet run
```

The application will automatically create the database tables on first run.

## API Documentation

Once running, visit `https://localhost:8080/swagger` for interactive API documentation.

## Key Features

- **Health Monitoring**: Automatic health checks for all adapters
- **Retry Logic**: Built-in retry mechanism for failed requests
- **Caching**: Health status caching to reduce load
- **Search & Filter**: Advanced search capabilities
- **Audit Trail**: Created/updated timestamps and user tracking
- **Error Handling**: Comprehensive error handling and logging
- **Swagger Documentation**: Auto-generated API documentation

## Migration from Old Controller

The old `Controller.cs` functionality has been preserved but properly separated:

- Adapter management → `AdaptersController`
- Request proxying → `ProxyController`
- Health checks → Both controllers (specialized)
- Configuration → Database-driven with fallback to appsettings

## Benefits of New Architecture

1. **Maintainability**: Clear separation of concerns
2. **Testability**: Each layer can be tested independently
3. **Scalability**: Easy to add new features and adapters
4. **Flexibility**: Database-driven configuration
5. **Monitoring**: Built-in health checks and logging
6. **Documentation**: Auto-generated API docs




