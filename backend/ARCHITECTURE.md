# Backend Architecture Guidelines

This backend connects a web frontend and a fleet of robots. It uses:
- SignalR for all frontend communications
- NATS JetStream for all robot communications
- EF Core + PostgreSQL (with PostGIS) for data persistence
- DTOs and mappers to keep API contracts stable and model clean

The goals are separation of concerns, testability, and maintainability. Code must be simple, readable, and consistent.

## Folder Structure

Use a clean architecture-inspired layout. Group code by responsibility, not by framework.

```
backend/
  Api/                         // Web API endpoints (CRUD)
  Model/                       // Core models and logic (no external deps)
  Application/                 // Use cases, services, handlers orchestrating domain
  Infrastructure/              // Adapters to external systems
    Persistence/               // EF Core, Npgsql, PostGIS config, repositories
    Messaging/                 // NATS JetStream producers/consumers
    Realtime/                  // SignalR hubs, clients, connection mgmt
    Configuration/             // Typed options, configuration bindings
  Dto/                         // DTOs for frontend/robot contracts
  Mapping/                     // Mappers (manual or AutoMapper profiles)
  Topics/                      // Centralized topic names for SignalR and NATS
  Endpoints/                   // Centralized API route names
  Validators/                  // Request/DTO validation (optional, e.g., FluentValidation)
  Program.cs                   // Composition root: DI, middlewares
  ARCHITECTURE.md              // This guideline
```

Rationale:
- Realtime/SignalR is isolated for easy maintenance
- Messaging/NATS JetStream is isolated for easy maintenance
- Persistence/DB code is isolated for easy maintenance
- Topics are centralized in one place (no magic strings)
- DTOs and Mapping are centralized to protect domain and keep controllers thin
- API endpoints for CRUD are separated for clarity and testing

## Communication

### Frontend ↔ Backend: SignalR
- All communication to the frontend must use SignalR.
- Define one or more hubs under `Infrastructure/Realtime/` (e.g., `Realtime/TelemetryHub.cs`, `Realtime/NotificationsHub.cs`).
- Expose strongly-typed hub methods and client contracts to avoid stringly-typed calls.
- Keep hub logic thin: delegate to `Application` services for business rules.

Example registration in Program.cs (conceptual):

```csharp
builder.Services.AddSignalR();
app.MapHub<TelemetryHub>("/hubs/telemetry");
app.MapHub<NotificationsHub>("/hubs/notifications");
```

#### Streaming Status via SignalR
- Use server-to-client streaming for live robot status/telemetry.
- Prefer `IAsyncEnumerable<T>` for streaming to clients.
- Keep streaming methods in hubs minimal and delegate fetching/updating to `Application` services.

Conceptual streaming example:

```csharp
public class TelemetryHub : Hub {
    public async IAsyncEnumerable<RobotStatusDto> StreamRobotStatus(string robotId, [EnumeratorCancellation] CancellationToken ct) {
        await foreach (var status in _statusService.Stream(robotId, ct)) {
            yield return status;
        }
    }
}
```

### Backend ↔ Robots: NATS JetStream
- All robot communication must use NATS JetStream.
- Create producers/consumers in `Infrastructure/Messaging/NatsJetstream/`.
- Use a single connection factory and DI-lifetime scoped clients.
- Keep subjects in `Topics/NatsTopics.cs`. Do not inline strings.
- Encapsulate serialization (JSON, protobuf, etc.) in messaging layer so `Application` only deals in DTOs/domain objects.

Conceptual producer interface:

```csharp
public interface IRobotCommandPublisher {
    Task PublishMoveCommandAsync(RobotMoveCommandDto dto, CancellationToken ct);
}
```

Conceptual consumer handler:

```csharp
public class RobotTelemetryConsumer : IHostedService {
    public Task StartAsync(CancellationToken ct) { /* subscribe JetStream */ }
    public Task StopAsync(CancellationToken ct) { /* unsubscribe */ }
}
```

## Topics

Centralize topic names to eliminate duplication and typos.

```
Topics/
  SignalRTopics.cs         // All hub-to-client event names
  NatsTopics.cs            // All NATS subjects (JetStream)
```

Example:

```csharp
public static class SignalRTopics {
    public const string TelemetryUpdated = "telemetry:updated";
    public const string RobotStatusChanged = "robot:statusChanged";
}

public static class NatsTopics {
    public const string TelemetryIngest = "robots.telemetry.ingest";
    public const string CommandMove = "robots.command.move";
    public const string CommandStop = "robots.command.stop";
}
```

## API Layer & CRUD

- Implement all CRUD operations through RESTful Web API endpoints under `Api/`.
- Endpoints receive/return DTOs; mapping occurs in `Mapping/`.
- Controllers must be thin and delegate to `Application` services which orchestrate domain logic and persistence.
- Route names are centralized in `Endpoints/ApiRoutes.cs`.

Example route names:

```csharp
public static class ApiRoutes {
    public const string Robots = "/api/robots";
    public const string RobotsById = "/api/robots/{id}";
    public const string Telemetry = "/api/telemetry";
    public const string Tasks = "/api/tasks";
    public const string TasksById = "/api/tasks/{id}";
}
```

## Data & Persistence

### EF Core + Postgres + PostGIS
- Use `Npgsql.EntityFrameworkCore.PostgreSQL` with `NetTopologySuite` to leverage PostGIS.
- Configure EF Core in `Infrastructure/Persistence/`.
- Model models should use `NetTopologySuite` types (e.g., `Point`, `Polygon`) only where geospatial is inherent to the domain.
- Prefer repositories or query services that return DTOs to controllers; keep LINQ inside application/infrastructure layers.

Configuration in Program.cs (conceptual):

```csharp
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Database"),
        npgsql => npgsql.UseNetTopologySuite()));
```

Example PostGIS query via NetTopologySuite translation:

```csharp
var nearby = await db.RobotPositions
    .Where(rp => rp.Location.Distance(targetPoint) <= 1000) // meters
    .ToListAsync(ct);
```

EF Core will translate geometry operations into PostGIS functions when `UseNetTopologySuite()` is enabled.

### Connection String

Store the DB connection string in configuration, not code. Use environment variables or `appsettings.*.json`.

```
Host=localhost;Port=5432;Database=sky;Username=postgres;Password=1234
```

Example `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5432;Database=sky;Username=postgres;Password=1234"
  }
}
```

Override via environment variable `ConnectionStrings__Database` in production.

## DTOs, Models, and Mappers

- Place API-facing DTOs under `Dto/`.
- Model entities stay under `Model/` and should not leak into controllers or hubs.
- Use mapper classes in `Mapping/` or AutoMapper profiles for conversion between `Model` and `Dto`.
- Keep mapping deterministic and unit-testable.

Example mapping approach:

```csharp
public static class RobotMapper {
    public static RobotDto ToDto(Robot entity) => new RobotDto { Id = entity.Id, Name = entity.Name, Status = entity.Status };
    public static Robot ToEntity(RobotDto dto) => new Robot(dto.Id, dto.Name, dto.Status);
}
```

## Composition Root

All wiring happens in `Program.cs`:
- Register SignalR hubs
- Register NATS JetStream connection factory, publishers, consumers
- Register EF Core DbContext
- Register application services and mappers
- Configure health checks and diagnostics
- Register Web API controllers in `Api/` and route constants from `Endpoints/ApiRoutes.cs`

Keep `Program.cs` readable; extract long registrations into extension methods under `Infrastructure/Configuration/`.

## Clean Code Practices

- Keep controllers and hubs thin: offload logic to `Application` services.
- No magic strings: use `Topics/*` and constants for routes/hub methods.
- Prefer async everywhere; avoid blocking.
- Validate incoming DTOs; reject invalid payloads early.
- Log structured events; avoid logging secrets.
- Add unit tests for mappers and application services; add integration tests for NATS and EF Core.
- Keep files short and single-responsibility; split into folders accordingly.
 - CRUD is implemented via API endpoints; streaming status updates are via SignalR

## Domain Readiness

The architecture is prepared for:

- Traffic Control
  - Represent lanes, zones, and segments in `Model/` using geospatial types (e.g., `Polygon`, `LineString`).
  - Persist and query with EF Core/PostGIS (`Infrastructure/Persistence/`) using spatial indexes.
  - Coordinate reservations and occupancy in `Application/TrafficControlService` with optimistic concurrency.
  - Broadcast occupancy/alerts to frontend via SignalR topics in `Realtime/` (e.g., `SignalRTopics.TrafficUpdated`).
  - Dispatch control commands to robots via NATS JetStream subjects in `Messaging/` (e.g., `NatsTopics.CommandStop`).

- Route Planning
  - Provide `Application/RoutePlanningService` to compute paths (A*, Dijkstra) over graph data sourced from the database.
  - Use PostGIS functions for geospatial checks (e.g., `ST_Distance`, `ST_LineIntersects`, `ST_Within`) and indexes.
  - Expose CRUD for route templates/constraints under `Api/` and route names in `Endpoints/ApiRoutes`.
  - Stream planning progress or ETA updates to frontend via SignalR streaming methods.

- Robot Telemetry Stream
  - Ingest telemetry from robots via a JetStream consumer (`Infrastructure/Messaging/NatsJetstream/RobotTelemetryConsumer`).
  - Persist telemetry snapshots and paths (e.g., `Point` sequences) with EF Core/PostGIS.
  - Stream live status to frontend via SignalR (e.g., `TelemetryHub.StreamRobotStatus`).
  - Publish anomalies or alerts via SignalR topics and durable events in JetStream when required.

## Application vs Worker Services

- Application
  - Owns business rules and request-driven orchestration (use cases).
  - Exposed via API controllers and SignalR hubs; transport-agnostic.
  - Calls repositories and external ports via abstractions; highly unit-testable.
  - Examples: `TrafficControlService` (reserve lanes/zones, validate conflicts), `RoutePlanningService` (compute routes on demand).

- Worker Service
  - Long-running, event/time-driven background processes (`IHostedService`/`BackgroundService`).
  - Subscribes to NATS JetStream, ingests telemetry, enforces policies continuously.
  - Performs precomputation/caching (e.g., route matrices, graph indexes) and reacts to topology changes.
  - Examples: `RobotTelemetryConsumer` (ingest streams), `TrafficEnforcementWorker` (monitor violations), `RouteCacheRefresher` (precompute routes).

Responsibility Split:
- Traffic control: Application for decision logic; Worker for continuous monitoring/enforcement.
- Route planning: Application for on-demand computation; Worker for precomputation/caching and recalculation on map changes.
## Packages (Recommendations)

These are recommended; verify and pin versions in the project:
- SignalR: `Microsoft.AspNetCore.SignalR`
- NATS JetStream: `NATS.Client` (JetStream support)
- PostgreSQL & EF Core: `Npgsql.EntityFrameworkCore.PostgreSQL` and `NetTopologySuite`
- Optional: `AutoMapper` and `FluentValidation`

## Environment & Configuration

- Keep secrets out of source: use user secrets or environment variables.
- Provide `appsettings.Development.json` checked in; keep production config external.
- Use typed options bound in `Infrastructure/Configuration/` and validate on startup.

## Example Registration Snippet

```csharp
// Program.cs
builder.Services.AddSignalR();
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Database"),
        npgsql => npgsql.UseNetTopologySuite()));

builder.Services.AddHostedService<RobotTelemetryConsumer>();
builder.Services.AddScoped<IRobotCommandPublisher, RobotCommandPublisher>();
builder.Services.AddScoped<IRobotService, RobotService>();

app.MapHub<TelemetryHub>("/hubs/telemetry");
app.MapHub<NotificationsHub>("/hubs/notifications");
```

## Testing

- Unit test `Mapping/` and `Application/` services (pure logic, fast).
- Integration test `Infrastructure/Persistence/` against a containerized Postgres with PostGIS enabled.
- Integration test `Infrastructure/Messaging/` with a local NATS JetStream server.
- Smoke test hubs with SignalR client integration tests for key events.

## Deployment Notes

- Ensure NATS server is running with JetStream enabled.
- Ensure Postgres has PostGIS extension installed (`CREATE EXTENSION postgis;`).
- Configure connection strings and subjects via environment for each environment.

---

This guideline defines structure, boundaries, and practices to keep the backend clean, robust, and easy to maintain as robot and frontend integrations evolve.
