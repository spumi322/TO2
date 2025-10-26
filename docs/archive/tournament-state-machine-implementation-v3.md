# Tournament State Machine Implementation Plan

**Status:** Ready for Implementation
**Created:** 2025-10-15
**Development Approach:** Vertical Slice Architecture
**Related Diagram:** `docs/TO2-StateMachineUpdatev2.drawio`

---

## Executive Summary

Refactor tournament lifecycle from 4-state system to explicit 8-state machine with admin control and transition states.

**Key Improvement:** Replace automatic bracket seeding with admin-approved transitions, allowing tournament organizers to review group results before starting bracket play.

**Architecture:** Each slice is a complete feature from Domain to UI. Test each slice before moving to the next.

**Note:** Bracket visualization (Slice 4) uses a temporary placeholder UI. Proper bracket tree visualization with a JS library will be implemented in a separate phase.

---

## Architecture: Domain Service Pattern

This implementation uses a **Domain Service** pattern for state machine validation to maintain an **anemic domain model**.

### Design Decisions

**Anemic Domain Model:** Tournament entities contain only data properties, no business logic methods. This keeps entities simple and focused on data structure.

**Domain Service:** `TournamentStateMachine` is a domain service (not a static class) that encapsulates state transition rules. It's implemented in the Domain layer but injected via Application layer interface.

**Dependency Injection:** The state machine is registered as a scoped service and injected into application services that need to validate state transitions.

### Structure

```
Domain/
  ├─ AggregateRoots/
  │   └─ Tournament.cs                    // Anemic entity (no business methods)
  └─ StateMachine/
      ├─ ITournamentStateMachine.cs       // Interface for dependency injection
      └─ TournamentStateMachine.cs        // Domain service implementation

Application/
  └─ Services/
      ├─ TournamentService.cs             // Injects ITournamentStateMachine
      └─ TournamentLifecycleService.cs    // Injects ITournamentStateMachine

WebAPI/
  └─ Program.cs                           // DI registration
```

### Usage Pattern

Services inject `ITournamentStateMachine` and call it before setting tournament status:

```csharp
// Application service
public class TournamentService
{
    private readonly ITournamentStateMachine _stateMachine;

    public TournamentService(ITournamentStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public async Task SomeMethod(Tournament tournament)
    {
        // Validate transition before setting status
        _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.NewState);
        tournament.Status = TournamentStatus.NewState;
    }
}
```

---

## Current vs Proposed System

### Current System (4 States)

```csharp
// src/Domain/Enums/TournamentStatus.cs
public enum TournamentStatus
{
    Upcoming = 1,    // Setup phase
    Ongoing = 2,     // Groups and bracket in progress
    Finished = 3,    // Tournament complete
    Cancelled = 4,   // Tournament cancelled
}
```

**Behavior:**
- No state validation
- Automatic bracket seeding when groups finish (no admin approval)
- No loading/transition states
- No distinction between "seeding" and "in progress" phases

**Key Problems:**
- `src/Application/Services/TournamentLifecycleService.cs:36-124` - Auto-seeds bracket (bad UX)
- `src/Application/Services/TournamentService.cs:180-207` - StartTournament() just closes registration

---

### Proposed System (8 States)

```csharp
public enum TournamentStatus
{
    Setup = 1,                    // 🟦 Initial config, registration open
    SeedingGroups = 2,            // 🟨 TRANSITION: Backend seeding groups
    GroupsInProgress = 3,         // 🟩 ACTIVE: Users scoring group matches
    GroupsCompleted = 4,          // 🟪 WAITING: Admin must start bracket
    SeedingBracket = 5,           // 🟨 TRANSITION: Backend seeding bracket
    BracketInProgress = 6,        // 🟩 ACTIVE: Users scoring bracket matches
    Finished = 7,                 // 🟥 COMPLETE: Champion declared
    Cancelled = 8,                // ⬜ CANCELLED: Tournament archived
}
```

**State Categories:**
- **Transition States** (2): SeedingGroups, SeedingBracket - Show loading spinner
- **Active States** (2): GroupsInProgress, BracketInProgress - Users can score matches
- **Waiting States** (1): GroupsCompleted - Admin decision needed

**Improvements:**
- ✅ Explicit `StartGroups()` and `StartBracket()` actions
- ✅ Admin approval required before bracket seeding
- ✅ State validation (enforced transitions)
- ✅ Loading indicators during backend processing

---

## Implementation - Vertical Slices

---

## Slice 0: State Machine Foundation

**Goal:** Add state machine infrastructure. Update database schema.

### Step 1: Update Domain Enum

**File:** `src/Domain/Enums/TournamentStatus.cs`

```csharp
namespace Domain.Enums
{
    /// <summary>
    /// Explicit state machine for tournament lifecycle.
    /// </summary>
    public enum TournamentStatus
    {
        /// <summary>🟦 Setup state - Initial configuration, registration open</summary>
        Setup = 1,

        /// <summary>🟨 Transition state - Backend seeding groups (show loading)</summary>
        SeedingGroups = 2,

        /// <summary>🟩 Active state - Users can score group matches</summary>
        GroupsInProgress = 3,

        /// <summary>🟪 Waiting state - All groups finished, awaiting admin approval</summary>
        GroupsCompleted = 4,

        /// <summary>🟨 Transition state - Backend seeding bracket (show loading)</summary>
        SeedingBracket = 5,

        /// <summary>🟩 Active state - Users can score bracket matches</summary>
        BracketInProgress = 6,

        /// <summary>🟥 Terminal state - Champion declared, tournament complete</summary>
        Finished = 7,

        /// <summary>⬜ Terminal state - Tournament cancelled/archived</summary>
        Cancelled = 8,
    }
}
```

### Step 2: Create State Machine Interface

**New File:** `src/Domain/StateMachine/ITournamentStateMachine.cs`

```csharp
using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Domain.StateMachine
{
    /// <summary>
    /// Domain service interface for tournament state machine validation and transitions.
    /// Implemented in Domain layer, injected via Application layer.
    /// </summary>
    public interface ITournamentStateMachine
    {
        bool IsTransitionValid(TournamentStatus currentState, TournamentStatus nextState);
        void ValidateTransition(TournamentStatus currentState, TournamentStatus nextState);
        IEnumerable<TournamentStatus> GetAllowedTransitions(TournamentStatus currentState);
        bool IsTransitionState(TournamentStatus status);
        bool IsActiveState(TournamentStatus status);
        bool IsTerminalState(TournamentStatus status);
        bool CanScoreMatches(TournamentStatus status);
        bool CanModifyTeams(TournamentStatus status);
    }
}
```

### Step 3: Implement State Machine Domain Service

**New File:** `src/Domain/StateMachine/TournamentStateMachine.cs`

```csharp
using Application.Contracts;
using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Domain.StateMachine
{
    /// <summary>
    /// Domain service that validates and manages tournament state transitions.
    /// Based on explicit state machine pattern with guard clauses.
    /// </summary>
    public class TournamentStateMachine : ITournamentStateMachine
    {
        private readonly Dictionary<TournamentStatus, HashSet<TournamentStatus>> _validTransitions = new()
        {
            { TournamentStatus.Setup, new HashSet<TournamentStatus>
                { TournamentStatus.SeedingGroups, TournamentStatus.Cancelled }
            },
            { TournamentStatus.SeedingGroups, new HashSet<TournamentStatus>
                { TournamentStatus.GroupsInProgress, TournamentStatus.Cancelled }
            },
            { TournamentStatus.GroupsInProgress, new HashSet<TournamentStatus>
                { TournamentStatus.GroupsCompleted, TournamentStatus.Cancelled }
            },
            { TournamentStatus.GroupsCompleted, new HashSet<TournamentStatus>
                { TournamentStatus.SeedingBracket, TournamentStatus.Cancelled }
            },
            { TournamentStatus.SeedingBracket, new HashSet<TournamentStatus>
                { TournamentStatus.BracketInProgress, TournamentStatus.Cancelled }
            },
            { TournamentStatus.BracketInProgress, new HashSet<TournamentStatus>
                { TournamentStatus.Finished, TournamentStatus.Cancelled }
            },
            { TournamentStatus.Finished, new HashSet<TournamentStatus>() },
            { TournamentStatus.Cancelled, new HashSet<TournamentStatus>() },
        };

        public bool IsTransitionValid(TournamentStatus currentState, TournamentStatus nextState)
        {
            if (!_validTransitions.ContainsKey(currentState))
                return false;

            return _validTransitions[currentState].Contains(nextState);
        }

        public void ValidateTransition(TournamentStatus currentState, TournamentStatus nextState)
        {
            if (!IsTransitionValid(currentState, nextState))
            {
                throw new InvalidOperationException(
                    $"Invalid state transition: {currentState} -> {nextState}. " +
                    $"Allowed: {string.Join(", ", GetAllowedTransitions(currentState))}"
                );
            }
        }

        public IEnumerable<TournamentStatus> GetAllowedTransitions(TournamentStatus currentState)
        {
            if (!_validTransitions.ContainsKey(currentState))
                return Array.Empty<TournamentStatus>();

            return _validTransitions[currentState];
        }

        public bool IsTransitionState(TournamentStatus status) =>
            status == TournamentStatus.SeedingGroups || status == TournamentStatus.SeedingBracket;

        public bool IsActiveState(TournamentStatus status) =>
            status == TournamentStatus.GroupsInProgress || status == TournamentStatus.BracketInProgress;

        public bool IsTerminalState(TournamentStatus status) =>
            status == TournamentStatus.Finished || status == TournamentStatus.Cancelled;

        public bool CanScoreMatches(TournamentStatus status) => IsActiveState(status);

        public bool CanModifyTeams(TournamentStatus status) => status == TournamentStatus.Setup;
    }
}
```

### Step 4: Update Tournament Aggregate Root (Anemic Domain)

**File:** `src/Domain/AggregateRoots/Tournament.cs`

**Important:** Keep the entity anemic - only update data properties, do NOT add business logic methods.

Remove any existing `using Domain.StateMachine;` statement (no longer needed).

Modify Status property to have public setter:

```csharp
public class Tournament : AggregateRootBase
{
    // ... existing code ...

    [Required]
    [EnumDataType(typeof(TournamentStatus))]
    [DefaultValue(TournamentStatus.Setup)]  // Changed from Upcoming
    public TournamentStatus Status { get; set; } = TournamentStatus.Setup; // Public setter

    public bool IsRegistrationOpen { get; set; } = true;  // Changed to true by default

    // ... existing code ...

    // DO NOT add methods like TransitionTo(), CanModifyTeams(), CanScoreMatches()
    // State validation is handled by ITournamentStateMachine service
}
```

**Why Anemic?** Tournament entity only holds data. State validation logic lives in the `TournamentStateMachine` domain service, which is injected into application services.

### Step 5: Register Domain Service in DI Container

**File:** `src/webAPI/Program.cs`

Add using statement:

```csharp
using Domain.StateMachine;
```

Register the service after repositories, before application services:

```csharp
// UoW, Repos
builder.Services.AddScoped<ITO2DbContext, TO2DbContext>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
// Domain Services
builder.Services.AddScoped<ITournamentStateMachine, TournamentStateMachine>();
// Application Services
builder.Services.AddScoped<ITournamentService, TournamentService>();
// ... rest of services
```

### Step 6: Update Database Schema

Run from `src/webAPI/`:

```bash
dotnet ef migrations add TournamentStateMachineRefactor --project ../Infrastructure --startup-project .
dotnet ef database update --project ../Infrastructure --startup-project .
```

This creates and applies the migration for the new enum values.

### Manual Testing

- [ ] Project compiles without errors
- [ ] State machine validator allows valid transitions (Setup → SeedingGroups)
- [ ] State machine validator blocks invalid transitions (Setup → Finished throws exception)
- [ ] Database schema updated successfully
- [ ] New tournaments default to `Status = Setup` and `IsRegistrationOpen = true`

---

## Slice 1: "Start Groups" Feature

**Goal:** Complete vertical feature - user clicks "Start Groups" button, backend seeds groups, UI updates.

### Step 1: Create DTOs

**New File:** `src/Application/DTOs/Tournament/StartGroupsResponseDTO.cs`

```csharp
using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record StartGroupsResponseDTO(
        bool Success,
        string Message,
        TournamentStatus TournamentStatus
    );
}
```

**New File:** `src/Application/DTOs/Tournament/TournamentStateDTO.cs`

```csharp
using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record TournamentStateDTO(
        TournamentStatus CurrentStatus,
        bool IsTransitionState,
        bool IsActiveState,
        bool CanScoreMatches,
        bool CanModifyTeams,
        string StatusDisplayName,
        string StatusDescription
    );
}
```

### Step 2: Update Service Interface

**File:** `src/Application/Contracts/ITournamentService.cs`

Add:

```csharp
Task<StartGroupsResponseDTO> StartGroups(long tournamentId);
Task<TournamentStateDTO> GetTournamentState(long tournamentId);
```

### Step 3: Implement in TournamentService

**File:** `src/Application/Services/TournamentService.cs`

Add `IMatchService` and `ITournamentStateMachine` dependencies to constructor, then add methods:

```csharp
// Add to constructor parameters
private readonly IMatchService _matchService;
private readonly ITournamentStateMachine _stateMachine;

public TournamentService(
    // ... existing parameters ...
    IMatchService matchService,
    ITournamentStateMachine stateMachine)
{
    // ... existing assignments ...
    _matchService = matchService;
    _stateMachine = stateMachine;
}

public async Task<StartGroupsResponseDTO> StartGroups(long tournamentId)
{
    var tournament = await _tournamentRepository.Get(tournamentId)
        ?? throw new Exception("Tournament not found");

    try
    {
        // 1. Validate and transition to SeedingGroups
        _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.SeedingGroups);
        tournament.Status = TournamentStatus.SeedingGroups;
        tournament.IsRegistrationOpen = false;
        await _tournamentRepository.Update(tournament);
        await _tournamentRepository.Save();

        // 2. Seed all groups
        var standings = await _standingService.GetStandingsAsync(tournamentId);
        var groupStandings = standings.Where(s => s.StandingType == StandingType.Group).ToList();

        if (!groupStandings.Any())
            throw new Exception("No group standings found");

        foreach (var group in groupStandings)
        {
            await _matchService.SeedGroup(tournamentId, group.Id);
        }

        // 3. Validate and transition to GroupsInProgress
        _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.GroupsInProgress);
        tournament.Status = TournamentStatus.GroupsInProgress;
        await _tournamentRepository.Update(tournament);
        await _tournamentRepository.Save();

        _logger.LogInformation($"Tournament {tournamentId} groups started. {groupStandings.Count} groups seeded.");

        return new StartGroupsResponseDTO(
            Success: true,
            Message: $"Groups started! {groupStandings.Count} groups created.",
            TournamentStatus: tournament.Status
        );
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning($"Invalid state transition: {ex.Message}");
        return new StartGroupsResponseDTO(false, ex.Message, tournament.Status);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error starting groups: {Message}", ex.Message);
        throw;
    }
}

public async Task<TournamentStateDTO> GetTournamentState(long tournamentId)
{
    var tournament = await _tournamentRepository.Get(tournamentId)
        ?? throw new Exception("Tournament not found");

    return new TournamentStateDTO(
        CurrentStatus: tournament.Status,
        IsTransitionState: _stateMachine.IsTransitionState(tournament.Status),
        IsActiveState: _stateMachine.IsActiveState(tournament.Status),
        CanScoreMatches: _stateMachine.CanScoreMatches(tournament.Status),
        CanModifyTeams: _stateMachine.CanModifyTeams(tournament.Status),
        StatusDisplayName: GetStatusDisplayName(tournament.Status),
        StatusDescription: GetStatusDescription(tournament.Status)
    );
}

private string GetStatusDisplayName(TournamentStatus status) => status switch
{
    TournamentStatus.Setup => "Setup",
    TournamentStatus.SeedingGroups => "Seeding Groups...",
    TournamentStatus.GroupsInProgress => "Groups In Progress",
    TournamentStatus.GroupsCompleted => "Groups Completed",
    TournamentStatus.SeedingBracket => "Seeding Bracket...",
    TournamentStatus.BracketInProgress => "Bracket In Progress",
    TournamentStatus.Finished => "Finished",
    TournamentStatus.Cancelled => "Cancelled",
    _ => "Unknown"
};

private string GetStatusDescription(TournamentStatus status) => status switch
{
    TournamentStatus.Setup => "Add teams and configure tournament",
    TournamentStatus.SeedingGroups => "Generating group matches...",
    TournamentStatus.GroupsInProgress => "Group stage in progress - score matches",
    TournamentStatus.GroupsCompleted => "All groups finished - ready to start bracket",
    TournamentStatus.SeedingBracket => "Generating bracket matches...",
    TournamentStatus.BracketInProgress => "Bracket stage in progress - score matches",
    TournamentStatus.Finished => "Tournament complete",
    TournamentStatus.Cancelled => "Tournament cancelled",
    _ => ""
};
```

### Step 4: Add API Endpoints

**File:** `src/webAPI/Controllers/TournamentsController.cs`

```csharp
/// <summary>
/// Starts the group stage (Setup -> GroupsInProgress).
/// </summary>
[HttpPost("{id}/start-groups")]
[ProducesResponseType(typeof(StartGroupsResponseDTO), 200)]
[ProducesResponseType(400)]
public async Task<IActionResult> StartGroups(long id)
{
    try
    {
        var result = await _tournamentService.StartGroups(id);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error starting groups for tournament {TournamentId}", id);
        return BadRequest(new { error = ex.Message });
    }
}

/// <summary>
/// Gets the current state machine status.
/// </summary>
[HttpGet("{id}/state")]
[ProducesResponseType(typeof(TournamentStateDTO), 200)]
[ProducesResponseType(404)]
public async Task<IActionResult> GetTournamentState(long id)
{
    try
    {
        var state = await _tournamentService.GetTournamentState(id);
        return Ok(state);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting tournament state for {TournamentId}", id);
        return NotFound(new { error = ex.Message });
    }
}
```

### Step 5: Update Angular Models

**File:** `src/UI/src/app/models/tournament.model.ts`

```typescript
export enum TournamentStatus {
  Setup = 1,
  SeedingGroups = 2,
  GroupsInProgress = 3,
  GroupsCompleted = 4,
  SeedingBracket = 5,
  BracketInProgress = 6,
  Finished = 7,
  Cancelled = 8
}

export interface TournamentStateDTO {
  currentStatus: TournamentStatus;
  isTransitionState: boolean;
  isActiveState: boolean;
  canScoreMatches: boolean;
  canModifyTeams: boolean;
  statusDisplayName: string;
  statusDescription: string;
}

export interface StartGroupsResponse {
  success: boolean;
  message: string;
  tournamentStatus: TournamentStatus;
}
```

### Step 6: Update Angular Service

**File:** `src/UI/src/app/services/tournament/tournament.service.ts`

```typescript
startGroups(tournamentId: number): Observable<StartGroupsResponse> {
  return this.http.post<StartGroupsResponse>(
    `${this.apiUrl}/${tournamentId}/start-groups`,
    {}
  );-
}

getTournamentState(tournamentId: number): Observable<TournamentStateDTO> {
  return this.http.get<TournamentStateDTO>(
    `${this.apiUrl}/${tournamentId}/state`
  );
}
```

### Step 7: Update Component

**File:** `src/UI/src/app/components/tournament/tournament-detail/tournament-detail.component.ts`

```typescript
export class TournamentDetailComponent implements OnInit {
  tournament: Tournament;
  tournamentState: TournamentStateDTO;
  TournamentStatus = TournamentStatus; // Make enum available in template

  ngOnInit(): void {
    this.loadTournament();
    this.loadTournamentState();
  }

  loadTournamentState(): void {
    this.tournamentService.getTournamentState(this.tournamentId)
      .subscribe(state => {
        this.tournamentState = state;
      });
  }

  onStartGroups(): void {
    if (confirm('Start group stage? Registration will be closed.')) {
      this.tournamentService.startGroups(this.tournamentId)
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.messageService.add({
                severity: 'success',
                summary: 'Groups Started',
                detail: response.message
              });
              this.loadTournamentState();
              this.loadTournament();
            } else {
              this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: response.message
              });
            }
          },
          error: (err) => this.handleError(err)
        });
    }
  }
}
```

### Step 8: Update Template

**File:** `src/UI/src/app/components/tournament/tournament-detail/tournament-detail.component.html`

```html
<!-- State Banner -->
<div class="state-banner" [ngClass]="'state-' + tournamentState?.currentStatus">
  <h3>{{ tournamentState?.statusDisplayName }}</h3>
  <p>{{ tournamentState?.statusDescription }}</p>
  <p-progressSpinner *ngIf="tournamentState?.isTransitionState"></p-progressSpinner>
</div>

<!-- Start Groups Button -->
<button
  pButton
  label="Start Groups"
  icon="pi pi-play"
  (click)="onStartGroups()"
  *ngIf="tournamentState?.currentStatus === TournamentStatus.Setup"
  class="p-button-success">
</button>

<!-- Team Management (only in Setup state) -->
<div *ngIf="tournamentState?.canModifyTeams">
  <!-- Your existing add/remove team UI -->
</div>
```

### Manual Testing

- [ ] Create tournament with `BracketAndGroup` format
- [ ] Add teams to tournament
- [ ] Verify "Start Groups" button visible in Setup state
- [ ] Click "Start Groups"
- [ ] Verify tournament transitions to `GroupsInProgress`
- [ ] Verify `IsRegistrationOpen` becomes `false`
- [ ] Verify group matches generated
- [ ] Verify UI shows "Groups In Progress" state
- [ ] Verify "Add Team" button now disabled
- [ ] Try calling `/start-groups` again - should return error (invalid state transition)

---

## Slice 2: Auto-Transition on Match Completion

**Goal:** When matches complete, auto-transition tournament states (GroupsInProgress → GroupsCompleted, BracketInProgress → Finished).

### Step 1: Update TournamentLifecycleService

**File:** `src/Application/Services/TournamentLifecycleService.cs`

Add `IGenericRepository<Tournament>` and `ITournamentStateMachine` to constructor:

```csharp
private readonly IGenericRepository<Tournament> _tournamentRepository;
private readonly ITournamentStateMachine _stateMachine;

public TournamentLifecycleService(
    ILogger<TournamentLifecycleService> logger,
    IStandingService standingService,
    IMatchService matchService,
    IGenericRepository<Standing> standingRepository,
    IGenericRepository<Tournament> tournamentRepository,
    ITournamentStateMachine stateMachine)
{
    _logger = logger;
    _standingService = standingService;
    _matchService = matchService;
    _standingRepository = standingRepository;
    _tournamentRepository = tournamentRepository;
    _stateMachine = stateMachine;
}
```

Replace `OnMatchCompleted()` method:

```csharp
public async Task<MatchResultDTO> OnMatchCompleted(long matchId, long winnerId, long loserId, long tournamentId)
{
    _logger.LogInformation($"=== Match {matchId} completed ===");

    var tournament = await _tournamentRepository.Get(tournamentId)
        ?? throw new Exception("Tournament not found");

    // Check if standing finished
    bool standingJustFinished = await _standingService.CheckAndMarkStandingAsFinishedAsync(tournamentId);

    if (!standingJustFinished)
    {
        _logger.LogInformation("No standing finished.");
        return new MatchResultDTO(winnerId, loserId);
    }

    _logger.LogInformation("✓ Standing finished! Checking tournament state...");

    // Handle state-specific logic
    if (tournament.Status == TournamentStatus.GroupsInProgress)
    {
        bool allGroupsFinished = await _standingService.CheckAndMarkAllGroupsAreFinishedAsync(tournamentId);

        if (allGroupsFinished)
        {
            _logger.LogInformation("✓✓ ALL GROUPS FINISHED! Waiting for admin to start bracket.");

            // Validate and auto-transition to GroupsCompleted
            _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.GroupsCompleted);
            tournament.Status = TournamentStatus.GroupsCompleted;
            await _tournamentRepository.Update(tournament);
            await _tournamentRepository.Save();

            return new MatchResultDTO(
                WinnerId: winnerId,
                LoserId: loserId,
                AllGroupsFinished: true,
                Message: "All groups completed. Admin can now start bracket."
            );
        }
    }
    else if (tournament.Status == TournamentStatus.BracketInProgress)
    {
        var bracket = (await _standingService.GetStandingsAsync(tournamentId))
            .FirstOrDefault(s => s.StandingType == StandingType.Bracket);

        if (bracket != null && bracket.IsFinished)
        {
            _logger.LogInformation("✓✓ BRACKET FINISHED! Tournament complete.");

            // Validate and auto-transition to Finished
            _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.Finished);
            tournament.Status = TournamentStatus.Finished;
            await _tournamentRepository.Update(tournament);
            await _tournamentRepository.Save();

            return new MatchResultDTO(
                WinnerId: winnerId,
                LoserId: loserId,
                TournamentComplete: true,
                Message: "Tournament complete!"
            );
        }
    }

    return new MatchResultDTO(winnerId, loserId);
}
```

### Step 2: Update MatchResultDTO (if needed)

**File:** `src/Application/DTOs/Match/MatchResultDTO.cs`

Ensure it has these optional fields:

```csharp
namespace Application.DTOs.Match
{
    public record MatchResultDTO(
        long WinnerId,
        long LoserId,
        bool AllGroupsFinished = false,
        bool TournamentComplete = false,
        string? Message = null
    );
}
```

### Manual Testing

- [ ] Start tournament in `GroupsInProgress`
- [ ] Score matches in one group → verify standing marked finished
- [ ] Verify tournament still `GroupsInProgress` (partial completion)
- [ ] Score all remaining group matches
- [ ] **Verify tournament auto-transitions to `GroupsCompleted`** ← KEY TEST

---

## Slice 3: UI State Guards & Polish

**Goal:** Show/hide buttons based on state. Loading spinners during transitions.

### Step 1: Update Component

**File:** `src/UI/src/app/components/tournament/tournament-detail/tournament-detail.component.ts`

```typescript
export class TournamentDetailComponent implements OnInit {
  tournament: Tournament;
  tournamentState: TournamentStateDTO;
  TournamentStatus = TournamentStatus;

  ngOnInit(): void {
    this.loadTournament();
    this.loadTournamentState();
    this.checkIfTransitionState();
  }

  checkIfTransitionState(): void {
    if (this.tournamentState?.isTransitionState) {
      // Poll every 2 seconds during transitions
      setTimeout(() => {
        this.loadTournamentState();
        this.checkIfTransitionState();
      }, 2000);
    }
  }

  canStartGroups(): boolean {
    return this.tournamentState?.currentStatus === TournamentStatus.Setup &&
           this.tournament?.teams?.length >= 2;
  }

  canAddTeams(): boolean {
    return this.tournamentState?.canModifyTeams;
  }

  canScoreMatches(): boolean {
    return this.tournamentState?.canScoreMatches;
  }

  getStateBannerClass(): string {
    if (!this.tournamentState) return '';

    const status = this.tournamentState.currentStatus;
    if (status === TournamentStatus.Setup) return 'state-setup';
    if (this.tournamentState.isTransitionState) return 'state-transition';
    if (this.tournamentState.isActiveState) return 'state-active';
    if (status === TournamentStatus.GroupsCompleted) return 'state-waiting';
    if (status === TournamentStatus.Finished) return 'state-finished';
    if (status === TournamentStatus.Cancelled) return 'state-cancelled';
    return '';
  }
}
```

### Step 2: Update Template

**File:** `src/UI/src/app/components/tournament/tournament-detail/tournament-detail.component.html`

```html
<!-- State Banner with Loading Spinner -->
<div class="tournament-state-banner" [ngClass]="getStateBannerClass()">
  <div class="state-info">
    <i class="pi pi-info-circle"></i>
    <div>
      <h3>{{ tournamentState?.statusDisplayName }}</h3>
      <p>{{ tournamentState?.statusDescription }}</p>
    </div>
  </div>
  <p-progressSpinner
    *ngIf="tournamentState?.isTransitionState"
    strokeWidth="4"
    [style]="{'width': '40px', 'height': '40px'}">
  </p-progressSpinner>
</div>

<!-- Action Buttons -->
<div class="tournament-actions">
  <!-- Start Groups -->
  <button
    pButton
    label="Start Groups"
    icon="pi pi-play"
    (click)="onStartGroups()"
    *ngIf="tournamentState?.currentStatus === TournamentStatus.Setup"
    [disabled]="!canStartGroups()"
    class="p-button-success">
  </button>

  <!-- Add/Remove Teams -->
  <button
    pButton
    label="Add Team"
    icon="pi pi-plus"
    (click)="onAddTeam()"
    [disabled]="!canAddTeams()">
  </button>
</div>

<!-- Match Scoring (only in Active states) -->
<p-panel header="Matches" *ngIf="canScoreMatches()">
  <!-- Your existing match UI -->
</p-panel>

<!-- Transition Overlay -->
<div *ngIf="tournamentState?.isTransitionState" class="transition-overlay">
  <p-progressSpinner></p-progressSpinner>
  <p>{{ tournamentState.statusDescription }}</p>
</div>
```

### Step 3: Add CSS

**File:** `src/UI/src/app/components/tournament/tournament-detail/tournament-detail.component.css`

```css
.tournament-state-banner {
  padding: 1rem;
  margin-bottom: 1.5rem;
  border-radius: 4px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.state-info {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.state-info i {
  font-size: 1.5rem;
}

.state-setup {
  background-color: #dae8fc;
  border-left: 4px solid #6c8ebf;
}

.state-transition {
  background-color: #fff2cc;
  border-left: 4px solid #d6b656;
}

.state-active {
  background-color: #d5e8d4;
  border-left: 4px solid #82b366;
}

.state-waiting {
  background-color: #e1d5e7;
  border-left: 4px solid #9673a6;
}

.state-finished {
  background-color: #f8cecc;
  border-left: 4px solid #b85450;
}

.state-cancelled {
  background-color: #f5f5f5;
  border-left: 4px solid #666;
}

.tournament-actions {
  display: flex;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.transition-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  z-index: 9999;
  color: white;
}
```

### Manual Testing

- [ ] **Setup state**: Only "Start Groups" and "Add Team" enabled
- [ ] **SeedingGroups**: Loading spinner visible, all buttons disabled
- [ ] **GroupsInProgress**: Match scoring enabled, team management disabled
- [ ] **GroupsCompleted**: UI shows waiting state banner
- [ ] Polling works during transition states (auto-refreshes)

---

## Slice 4: "Start Bracket" with Placeholder Bracket Display

**Goal:** Admin manually starts bracket + view bracket matches in simple list format for testing.

⚠️ **Note:** This slice uses a **temporary placeholder UI** for bracket display. The bracket will be shown as a simple list of matches. Proper bracket tree visualization using a JS library will be implemented in a separate phase after state machine is complete.

### Step 1: Create DTO

**New File:** `src/Application/DTOs/Tournament/StartBracketResponseDTO.cs`

```csharp
using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record StartBracketResponseDTO(
        bool Success,
        string Message,
        TournamentStatus TournamentStatus
    );
}
```

### Step 2: Update Service Interface

**File:** `src/Application/Contracts/ITournamentService.cs`

```csharp
Task<StartBracketResponseDTO> StartBracket(long tournamentId);
```

### Step 3: Implement in TournamentService

**File:** `src/Application/Services/TournamentService.cs`

Add `ITournamentLifecycleService` dependency to constructor (ITournamentStateMachine should already be injected from Slice 1):

```csharp
private readonly ITournamentLifecycleService _tournamentLifecycleService;
private readonly ITournamentStateMachine _stateMachine; // Should already exist from Slice 1

// Add to constructor
public TournamentService(
    // ... existing parameters ...
    ITournamentLifecycleService tournamentLifecycleService,
    ITournamentStateMachine stateMachine) // Should already exist from Slice 1
{
    // ... existing assignments ...
    _tournamentLifecycleService = tournamentLifecycleService;
    _stateMachine = stateMachine; // Should already exist from Slice 1
}

public async Task<StartBracketResponseDTO> StartBracket(long tournamentId)
{
    var tournament = await _tournamentRepository.Get(tournamentId)
        ?? throw new Exception("Tournament not found");

    try
    {
        // Validate state
        if (tournament.Status != TournamentStatus.GroupsCompleted)
        {
            return new StartBracketResponseDTO(
                Success: false,
                Message: $"Cannot start bracket from {tournament.Status}. Groups must be completed first.",
                TournamentStatus: tournament.Status
            );
        }

        // 1. Validate and transition to SeedingBracket
        _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.SeedingBracket);
        tournament.Status = TournamentStatus.SeedingBracket;
        await _tournamentRepository.Update(tournament);
        await _tournamentRepository.Save();

        // 2. Seed bracket
        var seedResult = await _tournamentLifecycleService.SeedBracketIfReady(tournamentId);
        if (!seedResult.Success)
        {
            return new StartBracketResponseDTO(false, seedResult.Message, tournament.Status);
        }

        // 3. Validate and transition to BracketInProgress
        _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.BracketInProgress);
        tournament.Status = TournamentStatus.BracketInProgress;
        await _tournamentRepository.Update(tournament);
        await _tournamentRepository.Save();

        _logger.LogInformation($"Tournament {tournamentId} bracket started.");

        return new StartBracketResponseDTO(
            Success: true,
            Message: "Bracket started successfully!",
            TournamentStatus: tournament.Status
        );
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning($"Invalid state transition: {ex.Message}");
        return new StartBracketResponseDTO(false, ex.Message, tournament.Status);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error starting bracket: {Message}", ex.Message);
        throw;
    }
}
```

### Step 4: Add API Endpoint

**File:** `src/webAPI/Controllers/TournamentsController.cs`

```csharp
/// <summary>
/// Starts the bracket stage (GroupsCompleted -> BracketInProgress).
/// </summary>
[HttpPost("{id}/start-bracket")]
[ProducesResponseType(typeof(StartBracketResponseDTO), 200)]
[ProducesResponseType(400)]
public async Task<IActionResult> StartBracket(long id)
{
    try
    {
        var result = await _tournamentService.StartBracket(id);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error starting bracket for tournament {TournamentId}", id);
        return BadRequest(new { error = ex.Message });
    }
}
```

### Step 5: Update Angular Models

**File:** `src/UI/src/app/models/tournament.model.ts`

```typescript
export interface StartBracketResponse {
  success: boolean;
  message: string;
  tournamentStatus: TournamentStatus;
}
```

### Step 6: Update Angular Service

**File:** `src/UI/src/app/services/tournament/tournament.service.ts`

```typescript
startBracket(tournamentId: number): Observable<StartBracketResponse> {
  return this.http.post<StartBracketResponse>(
    `${this.apiUrl}/${tournamentId}/start-bracket`,
    {}
  );
}
```

### Step 7: Update Component

**File:** `src/UI/src/app/components/tournament/tournament-detail/tournament-detail.component.ts`

Add to existing component:

```typescript
bracketMatches: any[] = []; // Placeholder for bracket matches

ngOnInit(): void {
  this.loadTournament();
  this.loadTournamentState();
  this.checkIfTransitionState();
  this.loadBracketMatches(); // Load bracket if in bracket stage
}

loadBracketMatches(): void {
  // Only load if in bracket stage
  if (this.tournamentState?.currentStatus === TournamentStatus.BracketInProgress ||
      this.tournamentState?.currentStatus === TournamentStatus.Finished) {

    // TODO: Replace with proper match service call
    // This is a placeholder - get bracket matches from your existing match service
    this.matchService.getMatchesByTournament(this.tournamentId)
      .subscribe(matches => {
        // Filter for bracket matches only
        this.bracketMatches = matches.filter(m => m.standingType === 'Bracket')
          .sort((a, b) => b.round - a.round || a.seed - b.seed);
      });
  }
}

onStartBracket(): void {
  if (confirm('Start bracket? This will seed teams from group results.')) {
    this.tournamentService.startBracket(this.tournamentId)
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.messageService.add({
              severity: 'success',
              summary: 'Bracket Started',
              detail: response.message
            });
            this.loadTournamentState();
            this.loadBracketMatches(); // Reload to show bracket
            this.loadTournament();
          } else {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: response.message
            });
          }
        },
        error: (err) => this.handleError(err)
      });
  }
}
```

### Step 8: Update Template with Placeholder Bracket Display

**File:** `src/UI/src/app/components/tournament/tournament-detail/tournament-detail.component.html`

Add to action buttons section:

```html
<!-- Start Bracket Button (only in GroupsCompleted state) -->
<button
  pButton
  label="Start Bracket"
  icon="pi pi-trophy"
  (click)="onStartBracket()"
  *ngIf="tournamentState?.currentStatus === TournamentStatus.GroupsCompleted"
  class="p-button-warning">
</button>
```

Add placeholder bracket display section:

```html
<!-- PLACEHOLDER Bracket Display (simple list view for testing) -->
<div *ngIf="tournamentState?.currentStatus === TournamentStatus.BracketInProgress ||
            tournamentState?.currentStatus === TournamentStatus.Finished"
     class="bracket-placeholder">

  <p-panel header="Bracket Matches">
    <p-message severity="info"
               text="Note: This is a placeholder UI for testing. Bracket tree visualization will be added in next phase."
               styleClass="mb-3">
    </p-message>

    <div *ngFor="let match of bracketMatches" class="bracket-match-item">
      <div class="match-header">
        <span class="match-round">Round {{ match.round }}</span>
        <span class="match-seed">Match {{ match.seed }}</span>
        <span class="match-status"
              [class.completed]="match.winnerId"
              [class.pending]="!match.winnerId">
          {{ match.winnerId ? 'Completed' : 'Pending' }}
        </span>
      </div>

      <div class="match-teams">
        <div class="team-row" [class.winner]="match.winnerId === match.team1?.id">
          <span class="team-name">{{ match.team1?.name || 'TBD' }}</span>
          <span *ngIf="match.winnerId === match.team1?.id" class="winner-badge">
            <i class="pi pi-check-circle"></i> Winner
          </span>
        </div>
        <div class="vs-divider">vs</div>
        <div class="team-row" [class.winner]="match.winnerId === match.team2?.id">
          <span class="team-name">{{ match.team2?.name || 'TBD' }}</span>
          <span *ngIf="match.winnerId === match.team2?.id" class="winner-badge">
            <i class="pi pi-check-circle"></i> Winner
          </span>
        </div>
      </div>

      <!-- Score Match Button (if your existing UI supports it) -->
      <button *ngIf="!match.winnerId && canScoreMatches()"
              pButton
              label="Score Match"
              class="p-button-sm"
              (click)="onScoreMatch(match)">
      </button>
    </div>

    <div *ngIf="!bracketMatches || bracketMatches.length === 0" class="no-matches">
      <p>No bracket matches yet. Click "Start Bracket" to generate bracket.</p>
    </div>
  </p-panel>
</div>
```

### Step 9: Add Placeholder CSS

**File:** `src/UI/src/app/components/tournament/tournament-detail/tournament-detail.component.css`

Add:

```css
/* Placeholder Bracket Display Styles */
.bracket-placeholder {
  margin-top: 2rem;
}

.bracket-match-item {
  border: 1px solid #ddd;
  border-radius: 4px;
  padding: 1rem;
  margin-bottom: 1rem;
  background: white;
}

.match-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.75rem;
  padding-bottom: 0.5rem;
  border-bottom: 1px solid #eee;
}

.match-round {
  font-weight: 600;
  color: #495057;
}

.match-seed {
  color: #6c757d;
  font-size: 0.9rem;
}

.match-status {
  padding: 0.25rem 0.75rem;
  border-radius: 12px;
  font-size: 0.85rem;
  font-weight: 500;
}

.match-status.completed {
  background-color: #d4edda;
  color: #155724;
}

.match-status.pending {
  background-color: #fff3cd;
  color: #856404;
}

.match-teams {
  margin-bottom: 1rem;
}

.team-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem;
  border-radius: 4px;
  background: #f8f9fa;
  margin-bottom: 0.5rem;
}

.team-row.winner {
  background: #d4edda;
  border: 2px solid #28a745;
}

.team-name {
  font-weight: 500;
  font-size: 1.1rem;
}

.vs-divider {
  text-align: center;
  color: #6c757d;
  font-style: italic;
  margin: 0.25rem 0;
}

.winner-badge {
  color: #28a745;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.no-matches {
  text-align: center;
  padding: 2rem;
  color: #6c757d;
}
```

### Manual Testing

- [ ] Start from tournament in `GroupsCompleted` state (after Slice 2)
- [ ] Verify "Start Bracket" button appears in UI
- [ ] Click "Start Bracket"
- [ ] Verify tournament transitions to `BracketInProgress`
- [ ] **Verify placeholder bracket display shows list of bracket matches**
- [ ] Verify matches grouped by round
- [ ] Verify team names displayed correctly
- [ ] Score bracket matches using your existing match scoring UI
- [ ] Verify match status updates from "Pending" to "Completed"
- [ ] Verify winner badge appears next to winning team
- [ ] Score finals match
- [ ] **Verify tournament auto-transitions to `Finished`** (from Slice 2)
- [ ] Try calling `/start-bracket` from wrong state - should fail

---

## Summary: Implementation Order

| Slice | Feature | What You Test |
|-------|---------|---------------|
| **0** | State machine foundation | State validation works, DB updated |
| **1** | "Start Groups" button | End-to-end: Click → Backend seeds → UI updates |
| **2** | Auto-transitions | Match completion triggers state changes |
| **3** | UI guards | Buttons show/hide based on state, loading spinners |
| **4** | "Start Bracket" + placeholder | Admin starts bracket, see simple list view |

Each slice is fully functional before moving to next.

**Next Step After Slice 4:** Replace placeholder bracket display with proper JS library (e.g., `bracket-tree`, `challonge-brackets`) for visual bracket tree.

---

## References

- **Diagram:** `docs/TO2-StateMachineUpdatev2.drawio`
- **State Machine Pattern:** [Stateless C# Library](https://github.com/dotnet-state-machine/stateless)

---

**End of Implementation Plan**
