# Andy Agentic - Clean Architecture Guide

## Overview

This project follows **Clean Architecture** principles to ensure maintainability, testability, and separation of concerns. The architecture is organized into three main layers with clear dependencies and responsibilities.

## Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│  • Controllers (API Endpoints)                             │
│  • SignalR Hubs (Real-time Communication)                  │
│  • Middleware                                              │
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
│  • SignalR Hub Implementations                             │
└─────────────────────────────────────────────────────────────┘
```

## Layer Responsibilities

### 1. Domain Layer (`Andy.Agentic.Domain`)

**Purpose**: Contains the core business logic and entities.

**Contains**:
- **Entities**: Database models (e.g., `WorkflowEntity`, `AgentEntity`)
- **Business Objects (BOs)**: Domain models (e.g., `Workflow`, `Agent`)
- **Enums**: Business constants (e.g., `WorkflowStatus`, `NodeStatus`)
- **Interfaces**: Repository contracts (e.g., `IWorkflowRepository`)
- **Domain Logic**: Core business rules

**Dependencies**: None (pure business logic)

**Example**:
```csharp
// Domain/Entities/WorkflowEntity.cs
public class WorkflowEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    // ... other properties
}

// Domain/Models/Workflow.cs
public class Workflow
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    // ... business logic
}

// Domain/Interfaces/IWorkflowRepository.cs
public interface IWorkflowRepository
{
    Task<Workflow> GetWorkflowByIdAsync(Guid id, Guid userId);
    Task<Workflow> CreateWorkflowAsync(Workflow workflow);
    // ... other methods
}
```

### 2. Application Layer (`Andy.Agentic.Application`)

**Purpose**: Contains application-specific logic and coordinates between layers.

**Contains**:
- **DTOs**: Data Transfer Objects for API communication
- **Application Services**: Business logic orchestration
- **Application Interfaces**: Service contracts
- **AutoMapper Profiles**: DTO ↔ BO mapping

**Dependencies**: Dom Layer only

**Example**:
```csharp
// Application/DTOs/WorkflowDto.cs
public class WorkflowDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    // ... API-specific properties
}

// Application/Services/WorkflowService.cs
public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IMapper _mapper;

    public async Task<WorkflowDto> CreateWorkflowAsync(CreateWorkflowDto dto, Guid userId)
    {
        // Map DTO to BO
        var workflow = _mapper.Map<Workflow>(dto);
        
        // Use domain repository
        var createdWorkflow = await _workflowRepository.CreateWorkflowAsync(workflow);
        
        // Map BO to DTO
        return _mapper.Map<WorkflowDto>(createdWorkflow);
    }
}
```

### 3. Infrastructure Layer (`Andy.Agentic.Infrastructure`)

**Purpose**: Handles external concerns like database access and external services.

**Contains**:
- **Repository Implementations**: Concrete implementations of domain interfaces
- **Database Context**: Entity Framework DbContext
- **External Service Integrations**: LLM providers, MCP servers, etc.
- **AutoMapper Profiles**: Entity ↔ BO mapping
- **SignalR Hub Implementations**

**Dependencies**: Domain Layer only

**Example**:
```csharp
// Infrastructure/Services/WorkflowRepository.cs
public class WorkflowRepository : IWorkflowRepository
{
    private readonly AndyDbContext _context;
    private readonly IMapper _mapper;

    public async Task<Workflow> CreateWorkflowAsync(Workflow workflow)
    {
        // Map BO to Entity
        var entity = _mapper.Map<WorkflowEntity>(workflow);
        
        // Database operations
        _context.Workflows.Add(entity);
        await _context.SaveChangesAsync();
        
        // Map Entity to BO
        return _mapper.Map<Workflow>(entity);
    }
}
```

### 4. Presentation Layer (`Andy.Agentic`)

**Purpose**: Handles HTTP requests and real-time communication.

**Contains**:
- **Controllers**: API endpoints
- **SignalR Hubs**: Real-time communication
- **Middleware**: Request/response processing
- **Configuration**: Dependency injection setup

**Dependencies**: Application Layer only

**Example**:
```csharp
// Controllers/WorkflowsController.cs
[ApiController]
[Route("api/[controller]")]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowService _workflowService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkflowDto>>> GetWorkflows()
    {
        var workflows = await _workflowService.GetWorkflowsAsync(userId);
        return Ok(workflows);
    }
}
```

## Data Flow

### 1. API Request Flow
```
Controller → Application Service → Domain Repository → Database
     ↓              ↓                    ↓
   DTOs          BOs                Entities
```

### 2. Data Mapping
```
API Request → DTO → BO → Entity → Database
                ↑    ↑      ↑
            AutoMapper AutoMapper
```

## Development Guidelines

### ✅ **DO's**

1. **Always use interfaces** for dependencies
2. **Map between layers** using AutoMapper
3. **Keep domain pure** - no external dependencies
4. **Use dependency injection** for all services
5. **Follow single responsibility** principle
6. **Write unit tests** for each layer
7. **Use async/await** for all I/O operations

### ❌ **DON'Ts**

1. **Never reference Infrastructure from Application**
2. **Never reference Application from Domain**
3. **Don't put business logic in controllers**
4. **Don't put database logic in application services**
5. **Don't skip mapping between layers**
6. **Don't use concrete types in interfaces**

## Adding New Features

### 1. **New Entity/Feature**

1. **Domain Layer**:
   - Create Entity in `Domain/Entities/`
   - Create BO in `Domain/Models/`
   - Create Enums in `Domain/Enums/`
   - Create Repository Interface in `Domain/Interfaces/`

2. **Application Layer**:
   - Create DTOs in `Application/DTOs/`
   - Create Service Interface in `Application/Interfaces/`
   - Create Service Implementation in `Application/Services/`
   - Add mappings in `Application/Mapping/DtosMapperProfile.cs`

3. **Infrastructure Layer**:
   - Create Repository Implementation in `Infrastructure/Services/`
   - Add mappings in `Infrastructure/Mapping/EntityMapperProfile.cs`
   - Update `AndyDbContext` with new DbSet

4. **Presentation Layer**:
   - Create Controller in `Controllers/`
   - Register services in `Program.cs`

### 2. **Example: Adding a New Feature**

```csharp
// 1. Domain/Entities/NewFeatureEntity.cs
public class NewFeatureEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

// 2. Domain/Models/NewFeature.cs
public class NewFeature
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

// 3. Domain/Interfaces/INewFeatureRepository.cs
public interface INewFeatureRepository
{
    Task<NewFeature> GetByIdAsync(Guid id);
    Task<NewFeature> CreateAsync(NewFeature feature);
}

// 4. Application/DTOs/NewFeatureDto.cs
public class NewFeatureDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

// 5. Application/Interfaces/INewFeatureService.cs
public interface INewFeatureService
{
    Task<NewFeatureDto> GetByIdAsync(Guid id);
    Task<NewFeatureDto> CreateAsync(CreateNewFeatureDto dto);
}

// 6. Application/Services/NewFeatureService.cs
public class NewFeatureService : INewFeatureService
{
    private readonly INewFeatureRepository _repository;
    private readonly IMapper _mapper;

    public async Task<NewFeatureDto> GetByIdAsync(Guid id)
    {
        var feature = await _repository.GetByIdAsync(id);
        return _mapper.Map<NewFeatureDto>(feature);
    }
}

// 7. Infrastructure/Services/NewFeatureRepository.cs
public class NewFeatureRepository : INewFeatureRepository
{
    private readonly AndyDbContext _context;
    private readonly IMapper _mapper;

    public async Task<NewFeature> GetByIdAsync(Guid id)
    {
        var entity = await _context.NewFeatures.FindAsync(id);
        return _mapper.Map<NewFeature>(entity);
    }
}

// 8. Controllers/NewFeatureController.cs
[ApiController]
[Route("api/[controller]")]
public class NewFeatureController : ControllerBase
{
    private readonly INewFeatureService _service;

    [HttpGet("{id}")]
    public async Task<ActionResult<NewFeatureDto>> GetById(Guid id)
    {
        var feature = await _service.GetByIdAsync(id);
        return Ok(feature);
    }
}
```

## AutoMapper Configuration

### DTO ↔ BO Mapping (Application Layer)
```csharp
// Application/Mapping/DtosMapperProfile.cs
public class DtosMapperProfile : Profile
{
    public DtosMapperProfile()
    {
        CreateMap<Workflow, WorkflowDto>().ReverseMap();
        CreateMap<WorkflowNode, WorkflowNodeDto>().ReverseMap();
        // ... other mappings
    }
}
```

### Entity ↔ BO Mapping (Infrastructure Layer)
```csharp
// Infrastructure/Mapping/EntityMapperProfile.cs
public class EntityMapperProfile : Profile
{
    public EntityMapperProfile()
    {
        CreateMap<WorkflowEntity, Workflow>()
            .ForMember(dest => dest.CreatedByUserName, 
                      opt => opt.MapFrom(src => src.CreatedByUser.DisplayName))
            .ReverseMap();
        // ... other mappings
    }
}
```

## Testing Strategy

### Unit Tests
- **Domain Layer**: Test business logic and domain rules
- **Application Layer**: Test service logic with mocked repositories
- **Infrastructure Layer**: Test repository implementations with in-memory database

### Integration Tests
- **API Controllers**: Test full request/response cycle
- **Database Operations**: Test with real database
- **SignalR Hubs**: Test real-time communication

## Dependency Injection

### Service Registration
```csharp
// Program.cs
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IWorkflowRepository, WorkflowRepository>();
builder.Services.AddScoped<IWorkflowExecutionService, WorkflowExecutionService>();
builder.Services.AddScoped<IWorkflowExecutionRepository, WorkflowExecutionRepository>();
```

## Database Migrations

### Adding New Entities
```bash
# Add migration
dotnet ef migrations add AddNewFeature --project src/Andy.Agentic.Infrastructure --startup-project src/Andy.Agentic

# Update database
dotnet ef database update --project src/Andy.Agentic.Infrastructure --startup-project src/Andy.Agentic
```

## SignalR Integration

### Real-time Events
```csharp
// Infrastructure/Semantic/OrchestrationHub.cs
public class OrchestrationHub : Hub
{
    public async Task JoinWorkflowGroup(string workflowId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"workflow-{workflowId}");
    }

    public async Task WorkflowUpdated(string workflowId, WorkflowDto workflow)
    {
        await Clients.Group($"workflow-{workflowId}").SendAsync("WorkflowUpdated", workflow);
    }
}
```

## Best Practices

1. **Keep layers thin** - Each layer should have a single responsibility
2. **Use dependency injection** - Makes testing easier and follows SOLID principles
3. **Map between layers** - Never pass entities or DTOs across layer boundaries
4. **Handle exceptions** - Use try-catch blocks and return appropriate HTTP status codes
5. **Log everything** - Use structured logging for better debugging
6. **Validate input** - Use data annotations and FluentValidation
7. **Use async/await** - For all I/O operations
8. **Write tests** - Unit tests for business logic, integration tests for APIs

## Common Patterns

### Repository Pattern
- Abstracts data access logic
- Makes testing easier with mocking
- Provides a consistent interface for data operations

### Service Layer Pattern
- Encapsulates business logic
- Coordinates between repositories
- Handles transactions and cross-cutting concerns

### DTO Pattern
- Separates internal models from API contracts
- Allows for versioning and evolution
- Provides a clean API interface

This architecture ensures maintainability, testability, and scalability while following industry best practices and Clean Architecture principles.




