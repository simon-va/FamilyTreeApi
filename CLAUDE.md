# CLAUDE.md — FamilyTree API

## Project Overview

A web application for building and managing family trees, targeting German-speaking casual users and families (DACH region). The focus is on simplicity, modern UX, data privacy (DSGVO-compliant), and collaborative family tree management.

**Current status:** MVP exists. Auth, Boards, Members, and Persons endpoints are implemented. Relations, Residences, Locks, Documents, and AuditLogs are planned but not yet implemented.

**Frontend:** Angular web app (separate repository). No mobile apps planned at this stage.

---

## Tech Stack

### Backend
- **ASP.NET Core** — Web API
- **Supabase** — PostgreSQL database, authentication (JWT), and file storage
- **Dapper + Npgsql** — Raw SQL queries, no ORM or EF Core migrations
- **ErrorOr** — Result type for handler return values instead of exceptions as control flow
- **FluentValidation** — Request DTO validation, one validator per feature
- **AspNetCore JwtBearer** — Validates Supabase user tokens
- **Scalar.AspNetCore** — API documentation (replaces Swagger UI)

### Testing
- **xUnit** — Test runner
- **Moq** — Mocking interfaces in unit tests
- **FluentAssertions** — Readable test assertions

---

## Architecture

### Guiding Principle
A pragmatic compromise between a flat structure and full Clean Architecture. Features are grouped vertically (by domain), not horizontally (by technical role). The structure is intentionally migration-friendly: if Clean Architecture is needed later, only project boundaries shift — the folder structure stays identical.

### Folder Structure

```
FamilyTree.Api/
├── Features/          # One folder per domain feature
│   ├── Auth/
│   ├── Boards/
│   ├── Members/
│   └── Persons/
├── Shared/            # Domain concepts used by multiple features, no own controller
│   └── BoardRole.cs
├── Infrastructure/    # Services that talk to external systems, used by multiple features
│   └── Database/      # DbConnectionFactory + Dapper type handlers (BoardRoleTypeHandler, GenderTypeHandler)
├── Common/            # Technical cross-cutting concerns, no domain knowledge
│   └── ErrorMapper.cs
├── Extensions/        # DI registration, split by concern
├── Program.cs
└── appsettings.json

FamilyTree.UnitTests/
└── Features/
    ├── Boards/
    ├── Members/
    └── Persons/
```

### Each Feature Folder Contains
```
Features/{Name}/
├── I{Name}Repository.cs     # Interface (contract)
├── {Name}Repository.cs      # Dapper implementation
├── {Name}Handler.cs         # Business logic, returns ErrorOr<T>
├── {Name}Errors.cs          # Static error definitions for this feature
├── {Name}Validator.cs       # FluentValidation for request DTOs
├── {Name}Controller.cs      # HTTP layer, maps ErrorOr via ErrorMapper
├── {Name}Dtos.cs            # Request and response DTOs
└── {EnumName}.cs            # Feature-scoped enums (e.g. Gender.cs in Persons/)
```

### Auth Feature — Special Case
`Features/Auth/` follows the same structure but differs in two ways:

1. **No board scoping** — Auth exists outside the board context. No `BoardAuthorizationService` check, no audit log, no locking.
2. **Supabase as the core** — Sign-up and login are proxied to Supabase Auth. The API adds a thin layer on top.

### Placement Rules

| Where | What belongs there |
|---|---|
| `Features/{Name}/` | Everything with its own controller and clear domain scope |
| `Shared/` | Domain concepts used by multiple features, no own controller (e.g. `BoardRole`) |
| `Infrastructure/` | Services with external dependencies used by multiple features (Storage, Auth, AuditLog) |
| `Common/` | Technical cross-cutting concerns with no domain knowledge (Exceptions, Enums, ErrorMapper) |

**Key rule:** Features never import other features. Anything needed by more than one feature goes into `Shared/` or `Infrastructure/`.

### Layer Responsibilities
```
Controller  →  Handler  →  Repository
                  ↓
           Infrastructure/
    (BoardAuth, AuditLog, Storage)
```

- **Controller:** Receives HTTP request, calls handler, maps `ErrorOr<T>` to `IActionResult` via `ErrorMapper`. No business logic.
- **Handler:** Contains all business logic. Calls `IBoardAuthorizationService`, writes audit log via `IAuditLogService`, calls repository.
- **Repository:** Database access only via Dapper. No business logic.
- **Infrastructure services:** Injected into handlers via interfaces. Single responsibility each.

### Interfaces
Every repository and every infrastructure service has an interface defined next to its implementation. This enables Moq-based unit testing without a live database.

```
IBoardAuthorizationService.cs  ←→  BoardAuthorizationService.cs
IPersonsRepository.cs          ←→  PersonsRepository.cs
```

---

## Database

### Platform
Supabase (PostgreSQL). Hosted in EU region. DSGVO-compliant.

### Query Strategy
Dapper with raw SQL. No ORM, no auto-migrations. Schema changes are written as plain SQL scripts and applied manually or via Supabase SQL editor.

### Core Tables
| Table | Status | Notes |
|---|---|---|
| `users` | Implemented | Managed by Supabase Auth. Extended with `first_name` and `last_name` written by the API on sign-up. |
| `boards` | Implemented | Represents a family tree. All data is scoped to a board. |
| `board_members` | Implemented | Links users to boards with a role (`owner`, `editor`, `viewer`) and `privacy_overrides` (JSONB) |
| `persons` | Implemented | Core data, scoped to `board_id` |
| `relations` | Planned | Scoped to `board_id` |
| `residences` | Planned | Scoped to `board_id` |
| `fuzzy_dates` | Planned | Shared date type referenced by other tables. No own endpoint. |
| `resource_locks` | Planned | Tracks which user is currently editing a resource, with TTL |
| `audit_logs` | Planned | Append-only log of all write actions, stores JSON snapshot of resource state |
| `documents` | Planned | File metadata. Actual files stored in Supabase Storage. |

### Soft Delete Strategy
All data tables have `is_deleted (bool)` and `deleted_at (timestamptz)`. DELETE endpoints set `is_deleted = true` — no physical deletion. Physical deletion only happens when an entire board is deleted (cascading hard delete). All GET queries filter `WHERE is_deleted = false`.

### Board Scoping
Every data record (person, relation, residence, document, lock, audit entry) is scoped to a `board_id`. No data is bound to a `user_id` directly. Access is controlled via the `board_members` table.

### Privacy for Living Persons
Persons have an `is_living (bool)` flag. When a viewer accesses a board, fields of living persons are filtered based on `privacy_overrides` (JSONB) stored on their `board_members` record. Filtering happens in the handler, not in SQL.

### Connection
`DbConnectionFactory` in `Infrastructure/Database/` provides `IDbConnection` (Npgsql) to all repositories via DI.

---

## Key Architectural Decisions

- **No MediatR:** Direct handler injection into controllers. Chosen deliberately to avoid boilerplate overhead for a solo developer.
- **No EF Core:** Dapper used for full SQL control and simplicity given Supabase as the managed DB.
- **ErrorOr over exceptions:** Handlers return `ErrorOr<T>`. Controllers map results via shared `Common/ErrorMapper.cs`. Exceptions reserved for truly unexpected errors only.
- **Supabase Auth over Keycloak:** JWT tokens issued by Supabase are validated via `JwtBearer`. Sign-up and login are proxied through `Features/Auth/` — the API adds `first_name`/`last_name` on top. Keycloak is not needed at this stage.
- **Interfaces everywhere:** Every repository and infrastructure service has an interface. Required for Moq-based unit tests in the `FamilyTree.UnitTests/` project.

## Coding Conventions

- **Explizite Variablennamen:** Variablen werden nach dem Domain-Objekt benannt, das sie enthalten — nicht nach ihrer technischen Rolle. Beispiele: `persons` statt `rows`, `person` statt `row`, `roleString` statt `result`, `deletedCount` statt `rowsAffected`.

---

## Testing

If a new handler is created or an existing one is updated, read [TestingStrategy](TestingStrategy.md).