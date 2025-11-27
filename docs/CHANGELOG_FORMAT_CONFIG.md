# Tournament Format Configuration - Changes Summary

## Overview
Implemented a centralized, single source of truth for tournament format configurations. All format-specific validations, rules, and business logic now use the `TournamentFormatConfiguration` service instead of scattered hardcoded values.

## Key Changes

### 1. Centralized Format Metadata
- Created `FormatMetadata` record with immutable format-specific constraints
- Dictionary-driven configuration pattern matching `TournamentStateMachine`
- Single source for min/max teams, group requirements, and bracket settings

### 2. Validator Refactoring
- `CreateTournamentValidator` now uses dependency injection to access `ITournamentFormatConfiguration`
- Replaced all hardcoded validation rules with dynamic metadata-driven validations
- Property-level validations for `MaxTeams`, `TeamsPerBracket`, and `TeamsPerGroup`
- Format-aware validation messages pulled from configuration

### 3. Format-Aware Team Selection
- Added `GetTeamsForBracketByFormat()` to handle both tournament formats:
  - **BracketOnly**: Returns all registered teams
  - **BracketAndGroup**: Returns teams advancing from groups
- Fixed missing team selection logic for BracketOnly bracket initialization

### 4. Service Updates
- `TournamentService` uses metadata to determine if groups/bracket required
- `StandingService` handles format-specific team selection
- Pipeline steps updated to use new format-aware methods

## Files Modified

### New Files
- `src/Domain/Configuration/FormatMetadata.cs`

### Modified Files
- `src/Domain/Configuration/ITournamentFormatConfiguration.cs`
- `src/Domain/Configuration/TournamentFormatConfiguration.cs`
- `src/Application/Validations/Tournament/CreateTournamentValidator.cs`
- `src/Application/Services/StandingService.cs`
- `src/Application/Services/TournamentService.cs`
- `src/Application/Contracts/IStandingService.cs`
- `src/Application/Pipelines/StartBracket/Steps/GetAdvancedTeamsStep.cs`
- `src/Tests/Unit/Validators/CreateTournamentValidatorTests.cs`

## Configuration Values

| Format | Min Teams | Max Teams | Teams/Group | Teams/Bracket |
|--------|-----------|-----------|-------------|---------------|
| BracketOnly | 2 | 32 | N/A | 2-32 |
| BracketAndGroup | 2 | 32 | 2-16 | 2-32 |

## Testing
- ✅ All 92 tests passing
- ✅ Build successful (0 errors, 0 warnings)
- ✅ Validator tests updated to match new configuration constraints

## Future Extensibility
Adding a new tournament format now requires:
1. Add enum value to `Domain.Enums.Format`
2. Add metadata entry to `TournamentFormatConfiguration._formatMetadata` dictionary
3. No changes needed to validators, services, or pipelines
