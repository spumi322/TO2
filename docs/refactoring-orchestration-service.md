# Refactoring OrchestrationService.ProcessGameResult()

## Current Problem

The `ProcessGameResult()` method in OrchestrationService is 234 lines and violates multiple SOLID principles:

**Single Responsibility Principle (SRP)**: Does 7+ different things
- Scores individual games
- Determines match winners
- Updates standing statistics
- Handles bracket progression
- Manages tournament state transitions
- Calculates final placements
- Coordinates between 11 different repositories

**Open/Closed Principle (OCP)**: Hard-coded if/else logic
- Group vs Bracket handling is tightly coupled
- Adding new tournament formats requires modifying existing code

**Dependency Inversion Principle (DIP)**: Constructor has 14 dependencies
- Depends directly on 11 concrete repositories
- Violates the "no more than 3-4 dependencies" guideline

## Proposed Solution: Pipeline + Strategy Pattern

Break the monolithic method into a **pipeline** of small, focused steps. Each step is a separate class with one responsibility.

### High-Level Architecture

```
GameResultRequest → Pipeline → GameResultResponse

Pipeline Steps:
1. Score Game
2. Check Match Completion
3. Update Standing Stats (Strategy: Group vs Bracket)
4. Handle Standing Completion
5. Progress Bracket (if applicable)
6. Transition Tournament State
7. Calculate Final Placements (if tournament finished)
8. Build Response
```

Each step:
- Has ONE clear responsibility
- Is 15-35 lines max
- Can be tested independently
- Can be easily replaced or extended

## Step-by-Step Implementation Plan

### Phase 1: Create the Pipeline Infrastructure (1-2 hours)

**Step 1.1: Define Pipeline Contracts**
- Create interface for pipeline step (e.g., `IGameResultPipelineStep`)
- Create context object to pass between steps (holds all data needed)
- Create result object for pipeline output

**Step 1.2: Build Pipeline Executor**
- Create orchestrator that runs steps in sequence
- Each step receives context, modifies it, returns continue/stop signal
- If any step fails, pipeline stops and returns error

**Step 1.3: Create Base Step Class**
- Abstract base class with common functionality
- Logging, error handling
- All concrete steps inherit from this

### Phase 2: Extract Steps One at a Time (3-4 hours)

**Migration Strategy**: Keep old method working while building new system alongside it.

**Step 2.1: Extract "Score Game" Step**
- Create `ScoreGameStep` class
- Move game scoring logic (lines 73-95 in current code)
- Dependencies: GameRepository only
- Test it independently

**Step 2.2: Extract "Check Match Completion" Step**
- Create `CheckMatchCompletionStep` class
- Move match winner determination logic (lines 96-125)
- Dependencies: GameRepository, MatchRepository
- Updates context with matchFinished flag, winnerId, loserId

**Step 2.3: Extract "Update Standing Stats" Step with Strategy**
- Create `IStandingStatsStrategy` interface
- Create `GroupStatsStrategy` (handles group wins/losses/points)
- Create `BracketStatsStrategy` (handles bracket progression)
- Create `UpdateStandingStatsStep` that uses appropriate strategy
- Dependencies: GroupRepository, StandingRepository
- Strategy selected based on standing type

**Step 2.4: Extract "Handle Standing Completion" Step**
- Create `HandleStandingCompletionStep` class
- Check if all matches in standing are finished
- Mark standing as finished if complete
- Dependencies: MatchRepository, StandingRepository

**Step 2.5: Extract "Progress Bracket" Step**
- Create `ProgressBracketStep` class
- Only runs if match is bracket match with winner
- Advances winner to next round
- Dependencies: MatchRepository, GroupRepository

**Step 2.6: Extract "Transition Tournament State" Step**
- Create `TransitionTournamentStateStep` class
- Determines new tournament status based on context
- Updates tournament entity
- Dependencies: TournamentRepository, StandingRepository

**Step 2.7: Extract "Calculate Final Placements" Step**
- Create `CalculateFinalPlacementsStep` class
- Only runs if tournament finished
- Generates final standings array
- Dependencies: GroupRepository, StandingRepository, TournamentRepository

**Step 2.8: Extract "Build Response" Step**
- Create `BuildResponseStep` class
- Takes context and creates GameProcessResult DTO
- No repository dependencies

### Phase 3: Wire Everything Together (1 hour)

**Step 3.1: Register Steps in DI Container**
- Register all steps as services
- Register strategies
- Register pipeline executor

**Step 3.2: Update OrchestrationService**
- Inject pipeline executor
- Replace 234-line method with 10-line method that calls pipeline
- Keep old method as `ProcessGameResult_Legacy()` for safety

**Step 3.3: Create Feature Flag**
- Add configuration setting to toggle between old/new implementation
- Test new pipeline in development
- Switch to new implementation in production when confident

### Phase 4: Clean Up (30 minutes)

**Step 4.1: Remove Old Code**
- Delete `ProcessGameResult_Legacy()` method
- Remove feature flag
- Reduce dependencies in OrchestrationService constructor to just pipeline executor

**Step 4.2: Remove Unused Dependencies**
- OrchestrationService should now only need 2-3 dependencies
- 11 repositories move to individual steps

## File Structure Recommendation

```
Application/
├── Pipelines/
│   ├── GameResult/
│   │   ├── Contracts/
│   │   │   ├── IGameResultPipelineStep.cs
│   │   │   ├── GameResultContext.cs
│   │   │   └── GameResultPipelineResult.cs
│   │   ├── Steps/
│   │   │   ├── ScoreGameStep.cs
│   │   │   ├── CheckMatchCompletionStep.cs
│   │   │   ├── UpdateStandingStatsStep.cs
│   │   │   ├── HandleStandingCompletionStep.cs
│   │   │   ├── ProgressBracketStep.cs
│   │   │   ├── TransitionTournamentStateStep.cs
│   │   │   ├── CalculateFinalPlacementsStep.cs
│   │   │   └── BuildResponseStep.cs
│   │   ├── Strategies/
│   │   │   ├── IStandingStatsStrategy.cs
│   │   │   ├── GroupStatsStrategy.cs
│   │   │   └── BracketStatsStrategy.cs
│   │   └── GameResultPipeline.cs
│   └── Common/
│       └── PipelineStepBase.cs
└── Services/
    └── OrchestrationService.cs (now just calls pipeline)
```

## Testing Strategy

**Unit Tests**: Each step in isolation
- Mock dependencies
- Test happy path
- Test edge cases
- Test error handling

**Integration Tests**: Full pipeline
- Use in-memory database
- Test complete game result flow
- Test group tournaments
- Test bracket tournaments
- Test bracket-and-groups tournaments
- Test tournament completion

**Regression Tests**: Compare old vs new
- Run same inputs through both implementations
- Verify outputs match
- Use real tournament scenarios from database

## Benefits After Refactoring

1. **Maintainability**: Each step is 15-35 lines and does ONE thing
2. **Testability**: Steps can be tested independently with mocked dependencies
3. **Extensibility**: Add new tournament formats by creating new strategies
4. **Readability**: Pipeline flow is self-documenting
5. **Debuggability**: Easy to add breakpoints/logging to specific steps
6. **Dependency Management**: OrchestrationService goes from 14 dependencies to 2-3

## Migration Risk Mitigation

- Keep old implementation working during migration
- Use feature flag to switch between old/new
- Write comprehensive tests before switching
- Deploy to staging environment first
- Monitor production carefully after deployment
- Have rollback plan ready (flip feature flag back)

## Estimated Time

- Phase 1: 1-2 hours (infrastructure)
- Phase 2: 3-4 hours (extract steps)
- Phase 3: 1 hour (wire up)
- Phase 4: 30 minutes (clean up)

**Total: 5-8 hours of focused work**

## Key Principles to Remember

- **KISS**: Each step should be simple and obvious
- **DRY**: Extract common logic to base class
- **YAGNI**: Don't add complexity you don't need yet
- **One Thing Well**: Each class has ONE responsibility
- **Testability First**: If it's hard to test, it's poorly designed
