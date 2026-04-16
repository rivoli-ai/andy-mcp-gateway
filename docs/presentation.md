---
marp: true
theme: default
paginate: true
size: 16:9
header: 'Andy MCP Gateway — End-to-End Walkthrough'
footer: 'Rivoli AI · andy-mcp-gateway'
style: |
  section { font-size: 24px; }
  section h1 { color: #1f4e79; }
  section h2 { color: #2e75b6; border-bottom: 2px solid #2e75b6; padding-bottom: 4px; }
  code { background: #f4f4f4; padding: 2px 4px; border-radius: 3px; }
  pre { font-size: 18px; }
  table { font-size: 20px; }
---

<!-- _class: lead -->
<!-- _paginate: false -->

# Andy MCP Gateway
## End-to-End System Walkthrough

A registry and control plane for MCP endpoints across the Andy ecosystem.

*Designed for engineers who have never seen this service before.*

---

## What is Andy MCP Gateway?

A small **ASP.NET Core registry** that catalogs MCP (Model Context Protocol) endpoints — so clients (Claude Desktop, Cursor, ChatGPT, AI agents) can discover where the Andy MCP servers live (`andy-issues`, `andy-agents`, `andy-code-index`, `andy-docs`, `andy-settings`, `andy-rbac`, `andy-containers`, …).

**Today:** registry / discovery only.
**Future (roadmap):** a real aggregator/proxy that forwards tool calls.

> Status: ALPHA. No persistence or auth yet.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 9.0 |
| API | ASP.NET Core 9 REST |
| API docs | Swashbuckle / Swagger |
| Storage | **In-memory** (thread-safe `ConcurrentDictionary`) |
| Frontend | Static HTML landing page + Swagger UI |
| Testing | xUnit + Microsoft.AspNetCore.Mvc.Testing + Coverlet |
| License | Apache 2.0 |

No database. No auth. No aggregation. Just a catalog.

---

## Solution Layout

```
andy-mcp-gateway/
├── src/Andy.Mcp.Gateway/
│   ├── Controllers/
│   │   └── GatewayRegistryController.cs  ← 6 endpoints
│   ├── Models/
│   │   ├── McpGateway.cs                 ← aggregate root
│   │   ├── CreateGatewayRequest.cs
│   │   ├── UpdateGatewayRequest.cs
│   │   └── GatewaySearchQuery.cs
│   ├── Services/
│   │   ├── IGatewayRegistryService.cs
│   │   └── InMemoryGatewayRegistryService.cs
│   ├── Program.cs
│   └── wwwroot/index.html
├── tests/Andy.Mcp.Gateway.Tests/
└── examples/Andy.Mcp.Gateway.Examples/   ← console client
```

---

## Why a Registry?

Every Andy service exposes its own MCP endpoint (usually `/mcp`). A client needs to know:

- Where to connect (`endpoint` URL)
- What it is (`name`, `description`, `version`)
- How to find it (`tags`)
- Whether it's usable (`status`)

A **registry** centralises this lookup. Services self-register on startup; clients query the registry and then speak MCP directly to each backend.

It's a *control plane*, not a *data plane*.

---

## Domain Model — `McpGateway`

`src/Andy.Mcp.Gateway/Models/McpGateway.cs`:

```csharp
public class McpGateway
{
    public string Id { get; set; }              // Guid
    public string Name { get; set; }            // required
    public string Description { get; set; }
    public string Endpoint { get; set; }        // required
    public string Version { get; set; }
    public List<string> Tags { get; set; }
    public GatewayStatus Status { get; set; }   // Active | Inactive | Maintenance
    public DateTime RegisteredAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}
```

---

## Supporting DTOs

- **`CreateGatewayRequest`** — POST payload (Name + Endpoint required)
- **`UpdateGatewayRequest`** — PUT payload, all fields nullable (partial updates)
- **`GatewaySearchQuery`** — `Page`, `PageSize`, `SearchTerm`, `Tags[]`, `Status?`

Pagination is `Skip = (Page-1) * PageSize`; search matches name + description case-insensitively.

---

## Service Contract

```csharp
public interface IGatewayRegistryService {
    Task<IEnumerable<McpGateway>> GetAllGatewaysAsync();
    Task<McpGateway?>             GetGatewayByIdAsync(string id);
    Task<IEnumerable<McpGateway>> SearchGatewaysAsync(GatewaySearchQuery q);
    Task<McpGateway>              CreateGatewayAsync(CreateGatewayRequest r);
    Task<McpGateway?>             UpdateGatewayAsync(string id, UpdateGatewayRequest r);
    Task<bool>                    DeleteGatewayAsync(string id);
}
```

Implementation: `InMemoryGatewayRegistryService` with `ConcurrentDictionary<string, McpGateway>`.

Timestamps auto-managed (RegisteredAt at create, UpdatedAt on mutations).

---

## REST API Surface

| Verb | Route | Success / Failure |
|------|-------|------------------|
| GET  | `/api/GatewayRegistry` | 200 |
| GET  | `/api/GatewayRegistry/{id}` | 200 / 404 |
| POST | `/api/GatewayRegistry` | 201 / 400 |
| POST | `/api/GatewayRegistry/search` | 200 |
| PUT  | `/api/GatewayRegistry/{id}` | 200 / 404 |
| DELETE | `/api/GatewayRegistry/{id}` | 204 / 404 |

Host: `https://localhost:5001` (dev). Swagger UI at `/swagger`.

Controller: `GatewayRegistryController` (`[Route("api/[controller]")]`).

---

## Program.cs Startup

```csharp
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IGatewayRegistryService,
                             InMemoryGatewayRegistryService>();

app.UseSwagger();
app.UseSwaggerUI();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
```

That's essentially the whole wiring. Singleton in-memory store is by design — restart is a wipe.

---

## The Landing Page

`wwwroot/index.html` — a single static page:

- Bridge emoji (🌉) logo
- Features grid: *Search & Filter*, *Fast & Reliable*, *Well Documented*, *Type Safe*
- Buttons: "🔧 API Documentation" → `/swagger`, "📚 View on GitHub"
- Color-coded endpoint list (GET green, POST blue, PUT orange, DELETE red)
- Pulsing "API Online" badge

No JS framework — HTML + CSS only. All interactivity lives in Swagger UI.

---

## Example Client

`examples/Andy.Mcp.Gateway.Examples/Program.cs` — a standalone console app demonstrating:

- Registering a gateway with metadata
- Retrieving it by id
- Searching by tag and keyword
- Updating status + description
- Deleting the entry
- Accepts a custom API URL as an argument

Dev runs disable TLS cert validation to make localhost testing painless.

---

## Current Aggregation Pattern

The service registers backends; clients connect directly.

```
Backend (andy-code-index)
  └─ on startup: POST /api/GatewayRegistry
      { Name, Endpoint:"…/mcp", Tags:["code-index"], Status:Active }

Client (Claude Desktop / Cursor / ChatGPT / andy-agents)
  ├─ GET /api/GatewayRegistry?tags=code-index
  │    → [ { Endpoint:"https://code-index.andy.local/mcp" } ]
  └─ connects directly to backend Endpoint and speaks MCP
```

The gateway *never* sees individual tool calls today.

---

## Future — True Aggregator Proxy

Next iterations would extend the registry with:

1. **Tool aggregation** — fetch tool lists from each Active backend, expose them under a single `/mcp`
2. **Request routing** — intercept `tools/call`, forward to the right backend by name/tags
3. **Auth passthrough** — validate a single OAuth token against Andy Auth, forward as Bearer
4. **Caching + rate limiting** — uniform DoS protection across backends
5. **Health-driven routing** — fail over Degraded → Unreachable → next instance

The in-memory registry is a foundation for all of that.

---

## Auth Today — None

ALPHA caveat: **no authentication is currently implemented**.

- No `[Authorize]` attributes on controllers
- Development TLS is self-signed; example client skips cert validation
- `ILogger` injection leaves room for audit hooks

This is acceptable while the service is registry-only; when aggregation lands, OAuth passthrough to Andy Auth + `[RequirePermission]` via Andy RBAC are the obvious add-ons.

---

## Configuration

`appsettings.json` — minimal:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

`appsettings.Development.json` bumps `Andy.Mcp.Gateway` logging to Debug.

`Directory.Build.props`: version `1.0.0`, suffix `beta` on develop, `rc` on main. `TreatWarningsAsErrors = true`.

---

## Deployment

- **Hosting:** standard ASP.NET Core (Kestrel, IIS, or reverse proxy).
- **Dockerfile:** not present yet.
- **CI:** `.github/workflows/ci.yml` runs on Ubuntu / Windows / macOS:
  - `dotnet restore`
  - `dotnet build --configuration Release`
  - `dotnet test`
- No persistence → zero-state deploys, but registrations are lost on restart. Clients must re-register.

A future release would add a database + a self-registration handshake (e.g. heartbeat liveness).

---

## Testing

**Unit** — `Services/InMemoryGatewayRegistryServiceTests` (10 cases):

- Create generates Id + timestamps
- Get by Id (hit + miss)
- List / Search (by term, tag, status)
- Pagination (Page + PageSize)
- Update partial fields / null returns
- Delete cascades retrieval

**Integration** — `Integration/GatewayRegistryApiTests` via `WebApplicationFactory<Program>`:

- 201 Created on POST
- 400 on missing required fields
- 404 on unknown id
- 204 on DELETE

Coverage: Coverlet HTML reports in `TestResults/CoverageReport/`.

---

## Data Flow — Today's Lookup

```
1. andy-code-index on startup
   POST /api/GatewayRegistry
   { Name:"Andy Code Index",
     Endpoint:"https://code-index.andy.local/mcp",
     Tags:["code-index","search"] }

2. Claude Desktop on user "find code":
   GET /api/GatewayRegistry?tags=code-index
   → [ { Id, Endpoint, Status:"Active", … } ]

3. Claude connects directly to Endpoint
   Issues MCP tools/call — bypassing the gateway
```

Gateway's job ends at step 2. Everything after is client ↔ backend.

---

## Summary

| Aspect | Detail |
|--------|--------|
| Role | MCP registry / discovery service |
| .NET | 9.0 |
| Storage | In-memory `ConcurrentDictionary` |
| Auth | None yet |
| Aggregation | Not yet (clients connect directly) |
| UI | Static landing + Swagger |
| Tests | 10 unit + integration via WebApplicationFactory |
| License | Apache 2.0 |

**Read it as the seed for a future MCP proxy** — the data model, service contract, and Swagger surface are already in place.

---

<!-- _class: lead -->

# Where to start reading

1. `src/Andy.Mcp.Gateway/Models/McpGateway.cs` — the aggregate root
2. `src/Andy.Mcp.Gateway/Services/InMemoryGatewayRegistryService.cs`
3. `src/Andy.Mcp.Gateway/Controllers/GatewayRegistryController.cs`
4. `src/Andy.Mcp.Gateway/Program.cs` — the wiring
5. `examples/Andy.Mcp.Gateway.Examples/Program.cs` — client pattern
6. `tests/Andy.Mcp.Gateway.Tests/Integration/GatewayRegistryApiTests.cs`

Swagger at `/swagger`. README.md + `Directory.Build.props` for project metadata.
