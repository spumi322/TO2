# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TO2 (Tourney Org 2) is a tournament management system with a .NET 8 backend and Angular 17 frontend. It manages competitive tournaments with various formats including bracket-only and bracket-with-groups tournaments.

## Common Commands

### Backend (.NET)

Navigate to `src/webAPI/` to run backend commands:

```bash
# Build the solution
dotnet build

# Run the API (starts on default port with Swagger UI at /)
dotnet run

# Create a new migration
dotnet ef migrations add MigrationName --project ../Infrastructure --startup-project .

# Update the database
dotnet ef database update --project ../Infrastructure --startup-project .

# Run from solution root
dotnet build src/webAPI/TO2.sln
dotnet run --project src/webAPI/TO2.csproj
```

### Frontend (Angular)

Navigate to `src/UI/` to run frontend commands:

```bash
# Start dev server (runs with SSL, proxies API calls)
npm start

# Build for production
ng build

# Run unit tests
ng test

# Generate a new component (non-standalone mode)
ng generate component components/feature-name/component-name

# Generate a service
ng generate service services/service-name/service-name
```

## Architecture

### Backend: Clean Architecture

The backend follows Clean Architecture with four layers:

1. **Domain** (`src/Domain/`) - Core business logic, no dependencies
   - Aggregate Roots: `Tournament`, `Team`, `Match`
   - Entities: `Standing`, `Group`, `Bracket`, `Game`, `Player`, `TournamentTeam`
   - Value Objects: `Prize`
   - Enums: `Format`, `TournamentStatus`, `StandingType`, `BestOf`, `TeamStatus`
   - Domain Events: `StandingFinishedEvent`, `AllGroupsFinishedEvent`

2. **Application** (`src/Application/`) - Business rules and orchestration
   - Services: `TournamentService`, `TeamService`, `MatchService`, `GameService`, `StandingService`
   - DTOs: Request/Response objects for API
   - Contracts: Interfaces (`IGenericRepository<T>`, `ITO2DbContext`, service interfaces)
   - Event Handlers: Domain event handlers
   - Validators: FluentValidation validators

3. **Infrastructure** (`src/Infrastructure/`) - Data persistence and external concerns
   - `TO2DbContext`: EF Core DbContext with domain event dispatching
   - `GenericRepository<T>`: Repository pattern implementation
   - Migrations: EF Core migrations
   - AutoMapper profiles

4. **WebAPI** (`src/webAPI/`) - ASP.NET Core Web API
   - Controllers: `TournamentsController`, `TeamsController`, `MatchesController`, `StandingsController`
   - `Program.cs`: DI configuration and middleware setup

**Dependency Flow**: WebAPI → Infrastructure → Application → Domain (dependencies point inward)

### Frontend: Angular 17

- **Non-standalone mode**: Components, directives, and pipes use NgModule declarations
- **UI Libraries**: PrimeNG (primary) and Angular Material
- **State Management**: RxJS Observables with services
- **Routing**: Lazy loading not yet implemented, all routes in `app-routing.module.ts`

Key directories:
- `src/app/components/` - UI components (tournament, matches, standings)
- `src/app/services/` - HTTP services for API communication
- `src/app/models/` - TypeScript interfaces matching backend DTOs
- `src/app/pipes/` - Custom pipes (e.g., filter-by-status)

## Key Architectural Patterns

### Domain-Driven Design

**Aggregate Roots** (entities with identity that control consistency boundaries):
- `Tournament`: Root for tournament-related entities
- `Team`: Root for team and player data
- `Match`: Root for match and game data

**Entities vs Value Objects**:
- Entities have `Id` and inherit from `EntityBase` or `AggregateRootBase`
- Value Objects (like `Prize`) have no identity, compared by value

### Domain Events System

Domain events enable decoupled state transitions:

1. Entities raise domain events via `AddDomainEvent()`
2. `TO2DbContext.SaveChangesAsync()` queues events before persisting
3. `DomainEventDispatcher` dispatches queued events after successful save
4. Event handlers process events (e.g., `StandingFinishedEventHandler`)

**Example**: When all matches in a standing are complete, `StandingFinishedEvent` is raised, triggering bracket seeding logic.

### Repository Pattern

`GenericRepository<T>` provides CRUD operations for aggregate roots:
- `Get(id)`, `GetAll()`, `GetAllByFK(foreignKey, value)`
- `Add(entity)`, `Update(entity)`, `Delete(entity)`
- `Save()` - calls `DbContext.SaveChangesAsync()` (triggers domain events)

### Many-to-Many Relationships

**Tournament ↔ Team** relationship uses explicit join entity `TournamentTeam`:
```csharp
// Tournament.cs
public ICollection<TournamentTeam> TournamentTeams { get; private set; }

// Team.cs
public ICollection<TournamentTeam> TournamentParticipations { get; private set; }
```

This allows additional metadata on the relationship if needed in the future.

## Tournament Lifecycle

Tournaments follow a state machine pattern:

### 1. Creation (Status: Upcoming)
- `IsRegistrationOpen = true`
- Creates standings based on `Format`:
  - **BracketOnly**: One "Main Bracket" standing
  - **BracketAndGroup**: One bracket + multiple group standings
- Standings have `IsFinished = false`, `IsSeeded = false`

### 2. Team Registration
- Teams added via `TournamentTeam` join entity
- Teams associated with standings via `Group` entries
- `Group` entity tracks team stats (Wins, Losses, Points, Eliminated)

### 3. Start Tournament
- Sets `IsRegistrationOpen = false`, `Status = InProgress`
- Seeds groups randomly (no seeding weights yet)
- Matches can now be scored

### 4. Playing Matches
- Match results set via `MatchService.SetMatchResult()`
- Game-by-game scoring with `GameService.SetGameResult()`
- When all group matches complete: `AllGroupsFinishedEvent` fires
- Event handler seeds the bracket by pairing top teams from groups

### 5. Completion
- When bracket finishes: `Status = Finished`, `IsFinished = true`
- Winner determined from final bracket match

## Important Domain Concepts

### Standing Types
- **Group**: Round-robin, tracks team stats (wins/losses/points)
- **Bracket**: Single/double elimination, tracks match tree (Round, Seed)

### Tournament Formats
- **BracketOnly**: Direct elimination bracket
- **BracketAndGroup**: Group stage → bracket stage (common in esports)

### Match Structure
- `Match` contains multiple `Game` entities (BestOf 1/3/5)
- `WinnerId`/`LoserId` populated when match completes
- Belongs to a `Standing`, has `Round` and `Seed` for bracket positioning

### Group Entity
The `Group` entity serves as both:
1. Tournament participation record (links Team to Tournament+Standing)
2. Stats tracker (Wins, Losses, Points, Status, Eliminated)

This dual purpose is important - it's NOT just a join table.

## Database

- **SQLite** database located at `src/Infrastructure/app.db`
- Connection string hardcoded in `TO2DbContext.OnConfiguring()`
- Use EF Core migrations from `src/webAPI/` directory
- Soft deletes: Set `TournamentStatus.Cancelled` instead of removing

## Development Configuration

### CORS (Backend)
API allows CORS from `https://127.0.0.1:4200` and `https://localhost:4200` for local Angular dev server.

### Angular Dev Server
- Runs with SSL via `npm start` (uses run-script-os for cross-platform)
- SSL certificates managed by aspnetcore-https setup
- Proxy configuration in `src/UI/src/proxy.conf.js` routes `/api/*` to backend

### Swagger UI
Available at the root path `/` in development mode.

## Validation

- **Backend**: FluentValidation for DTOs (e.g., `CreateTournamentValidator`)
- **Frontend**: Angular Reactive Forms with validators

## Current Limitations

- No authentication/authorization system
- Prize pool distribution not implemented
- Seeding uses random assignment (no skill-based seeding)
- No scheduling system (StartDate/EndDate commented out)
- Standing generation partially commented out in `StandingService.GenerateStanding()`
