# Backend Service Methods Reference

**Purpose**: Comprehensive documentation of all backend service methods for sequence diagram creation and design analysis.

**Generated**: 2025-10-14

---

## Table of Contents

1. [TournamentService](#tournamentservice)
2. [TeamService](#teamservice)
3. [MatchService](#matchservice)
4. [GameService](#gameservice)
5. [StandingService](#standingservice)
6. [TournamentLifecycleService](#tournamentlifecycleservice)
7. [Design Analysis](#design-analysis)
8. [Unused Methods](#unused-methods)
9. [Method Call Graph](#method-call-graph)

---

## TournamentService

**Location**: `src/Application/Services/TournamentService.cs`

**Dependencies**:
- `IGenericRepository<Tournament>`
- `ITeamService`
- `ITO2DbContext`
- `IStandingService`
- `IMapper`
- `ILogger<TournamentService>`

### Methods

#### `CreateTournamentAsync`
```csharp
Task<CreateTournamentResponseDTO> CreateTournamentAsync(CreateTournamentRequestDTO request)
```
**Purpose**: Creates a new tournament with standings based on format

**Returns**: `CreateTournamentResponseDTO` containing tournament ID

**Method Calls**:
- `_mapper.Map<Tournament>(request)`
- `_tournamentRepository.Add(tournament)`
- `_tournamentRepository.Save()`
- `_standingService.GenerateStanding()` - Main Bracket
- `_standingService.GenerateStanding()` - Groups (if BracketAndGroup format)

**Called By**: TournamentsController.Post() ✓

**Status**: ✓ USED

---

#### `GetTournamentAsync`
```csharp
Task<GetTournamentResponseDTO> GetTournamentAsync(long id)
```
**Purpose**: Retrieves a single tournament by ID

**Returns**: `GetTournamentResponseDTO`

**Method Calls**:
- `_tournamentRepository.Get(id)`
- `_mapper.Map<GetTournamentResponseDTO>()`

**Called By**: TournamentsController.Get() ✓

**Status**: ✓ USED

---

#### `GetAllTournamentsAsync`
```csharp
Task<List<GetAllTournamentsResponseDTO>> GetAllTournamentsAsync()
```
**Purpose**: Retrieves all tournaments

**Returns**: `List<GetAllTournamentsResponseDTO>`

**Method Calls**:
- `_tournamentRepository.GetAll()`
- `_mapper.Map<List<GetAllTournamentsResponseDTO>>()`

**Called By**: TournamentsController.GetAll() ✓

**Status**: ✓ USED

---

#### `UpdateTournamentAsync`
```csharp
Task<UpdateTournamentResponseDTO> UpdateTournamentAsync(long id, UpdateTournamentRequestDTO request)
```
**Purpose**: Updates tournament properties

**Returns**: `UpdateTournamentResponseDTO`

**Method Calls**:
- `_tournamentRepository.Get(id)`
- `_mapper.Map(request, existingTournament)`
- `_tournamentRepository.Update()`
- `_tournamentRepository.Save()`

**Called By**: TournamentsController.Put() ✓

**Status**: ✓ USED

---

#### `SoftDeleteTournamentAsync`
```csharp
Task SoftDeleteTournamentAsync(long id)
```
**Purpose**: Soft deletes tournament by setting status to Cancelled

**Returns**: `Task` (void)

**Method Calls**:
- `_tournamentRepository.Get(id)`
- `_tournamentRepository.Update()`
- `_tournamentRepository.Save()`

**Called By**: TournamentsController.Delete() ✓

**Status**: ✓ USED

---

#### `SetTournamentStatusAsync`
```csharp
Task SetTournamentStatusAsync(long id, TournamentStatus status)
```
**Purpose**: Sets tournament status to specific value

**Returns**: `Task` (void)

**Method Calls**:
- `_tournamentRepository.Get(id)`
- `_tournamentRepository.Update()`
- `_tournamentRepository.Save()`

**Called By**: TournamentsController.Put() ✓

**Status**: ✓ USED

---

#### `GetTeamsByTournamentAsync`
```csharp
Task<List<GetTeamResponseDTO>> GetTeamsByTournamentAsync(long tournamentId)
```
**Purpose**: Gets all teams registered in a tournament

**Returns**: `List<GetTeamResponseDTO>`

**Method Calls**:
- `_dbContext.TournamentTeams.Where().Select().ToListAsync()`
- `_mapper.Map<List<GetTeamResponseDTO>>()`

**Called By**:
- TournamentsController.GetByTournament() ✓
- MatchService.SeedGroups() ✓

**Status**: ✓ USED (2 callers)

---

#### `RemoveTeamFromTournamentAsync`
```csharp
Task RemoveTeamFromTournamentAsync(long teamId, long tournamentId)
```
**Purpose**: Removes a team from tournament if registration is still open

**Returns**: `Task` (void)

**Method Calls**:
- `_teamService.GetTeamAsync(teamId)`
- `_tournamentRepository.Get(tournamentId)`
- `_dbContext.TournamentTeams.FirstOrDefaultAsync()`
- `_dbContext.TournamentTeams.Remove()`
- `_dbContext.SaveChangesAsync()`

**Called By**: TournamentsController.RemoveTeam() ✓

**Status**: ✓ USED

---

#### `StartTournament`
```csharp
Task<StartTournamentDTO> StartTournament(long tournamentId)
```
**Purpose**: Closes registration and starts the tournament

**Returns**: `StartTournamentDTO` (message and success flag)

**Method Calls**:
- `_tournamentRepository.Get(tournamentId)`
- `_tournamentRepository.Update()`
- `_tournamentRepository.Save()`

**Called By**: TournamentsController.StartTournament() ✓

**Status**: ✓ USED

---

#### `CheckNameIsUniqueAsync`
```csharp
Task<IsNameUniqueResponseDTO> CheckNameIsUniqueAsync(string name)
```
**Purpose**: Validates tournament name uniqueness

**Returns**: `IsNameUniqueResponseDTO`

**Method Calls**:
- `_tournamentRepository.GetAll()`

**Called By**: TournamentsController.CheckTournamentNameIsUnique() ✓

**Status**: ✓ USED

---

#### `DeclareChampion`
```csharp
Task DeclareChampion(long tournamentId, long championTeamId)
```
**Purpose**: Marks tournament as finished and declares champion

**Returns**: `Task` (void)

**Method Calls**:
- `_tournamentRepository.Get(tournamentId)`
- `_standingService.GetStandingsAsync(tournamentId)`
- `_dbContext.BracketEntries.FirstOrDefaultAsync()`
- `_tournamentRepository.Update()`
- `_tournamentRepository.Save()`

**Called By**: MatchService.CheckAndGenerateNextRound() ✓

**Status**: ✓ USED

---

#### `GetChampion`
```csharp
Task<GetTeamResponseDTO?> GetChampion(long tournamentId)
```
**Purpose**: Returns the tournament champion if tournament is finished

**Returns**: `GetTeamResponseDTO?` (nullable)

**Method Calls**:
- `_tournamentRepository.Get(tournamentId)`
- `_standingService.GetStandingsAsync(tournamentId)`
- `_dbContext.BracketEntries.Include().FirstOrDefaultAsync()`
- `_mapper.Map<GetTeamResponseDTO>()`

**Called By**: TournamentsController.GetChampion() ✓

**Status**: ✓ USED

---

#### `GetFinalStandings`
```csharp
Task<List<FinalStandingDTO>> GetFinalStandings(long tournamentId)
```
**Purpose**: Returns final standings (top 8 teams) from bracket

**Returns**: `List<FinalStandingDTO>`

**Method Calls**:
- `_tournamentRepository.Get(tournamentId)`
- `_standingService.GetStandingsAsync(tournamentId)`
- `_dbContext.BracketEntries.Where().OrderByDescending().ToListAsync()`

**Called By**: TournamentsController.GetFinalStandings() ✓

**Status**: ✓ USED

---

## TeamService

**Location**: `src/Application/Services/TeamService.cs`

**Dependencies**:
- `IGenericRepository<Team>`
- `IGenericRepository<Tournament>`
- `IGenericRepository<TournamentTeam>`
- `ITO2DbContext`
- `IMapper`
- `ILogger<TeamService>`

### Methods

#### `CreateTeamAsync`
```csharp
Task<CreateTeamResponseDTO> CreateTeamAsync(CreateTeamRequestDTO request)
```
**Purpose**: Creates a new team

**Returns**: `CreateTeamResponseDTO` containing team ID

**Method Calls**:
- `_mapper.Map<Team>(request)`
- `_teamRepository.Add(team)`
- `_teamRepository.Save()`

**Called By**: TeamsController.Post() ✓

**Status**: ✓ USED

---

#### `GetAllTeamsAsync`
```csharp
Task<List<GetAllTeamsResponseDTO>> GetAllTeamsAsync()
```
**Purpose**: Retrieves all teams

**Returns**: `List<GetAllTeamsResponseDTO>`

**Method Calls**:
- `_teamRepository.GetAll()`
- `_mapper.Map<List<GetAllTeamsResponseDTO>>()`

**Called By**: TeamsController.GetAll() ✓

**Status**: ✓ USED

---

#### `GetTeamsWithStatsAsync`
```csharp
Task<List<GetTeamWithStatsResponseDTO>> GetTeamsWithStatsAsync(long standingId)
```
**Purpose**: Gets teams with their group stats for a standing

**Returns**: `List<GetTeamWithStatsResponseDTO>`

**Method Calls**:
- `_dbContext.GroupEntries.Where().ToListAsync()`
- `_mapper.Map<List<GetTeamWithStatsResponseDTO>>()`

**Called By**: TeamsController.GetTeamsWithStats() ✓

**Status**: ✓ USED

---

#### `GetTeamAsync`
```csharp
Task<GetTeamResponseDTO> GetTeamAsync(long teamId)
```
**Purpose**: Retrieves a single team by ID

**Returns**: `GetTeamResponseDTO`

**Method Calls**:
- `_teamRepository.Get(teamId)`
- `_mapper.Map<GetTeamResponseDTO>()`

**Called By**:
- TeamsController.Get() ✓
- TournamentService.RemoveTeamFromTournamentAsync() ✓

**Status**: ✓ USED (2 callers)

---

#### `UpdateTeamAsync`
```csharp
Task<UpdateTeamResponseDTO> UpdateTeamAsync(long id, UpdateTeamRequestDTO request)
```
**Purpose**: Updates team properties

**Returns**: `UpdateTeamResponseDTO`

**Method Calls**:
- `_teamRepository.Get(id)`
- `_mapper.Map(request, existingTeam)`
- `_teamRepository.Update()`
- `_teamRepository.Save()`

**Called By**: TeamsController.Put() ✓

**Status**: ✓ USED

---

#### `DeleteTeamAsync`
```csharp
Task DeleteTeamAsync(long teamId)
```
**Purpose**: Hard deletes a team

**Returns**: `Task` (void)

**Method Calls**:
- `_teamRepository.Delete(teamId)`
- `_teamRepository.Save()`

**Called By**: TeamsController.Delete() ✓

**Status**: ✓ USED

---

#### `AddTeamToTournamentAsync`
```csharp
Task<AddTeamToTournamentResponseDTO> AddTeamToTournamentAsync(AddTeamToTournamentRequestDTO request)
```
**Purpose**: Registers a team to a tournament with validation

**Returns**: `AddTeamToTournamentResponseDTO`

**Validations**:
- Team exists
- Tournament exists
- Registration is open
- Team not already in tournament
- No duplicate team names in tournament
- Tournament not at capacity

**Method Calls**:
- `_teamRepository.Get(request.TeamId)`
- `_tournamentRepository.Get(request.TournamentId)`
- `_dbContext.TournamentTeams.Where().FirstOrDefaultAsync()` (check existing)
- `_dbContext.TournamentTeams.Where().Join().AnyAsync()` (check name)
- `_dbContext.TournamentTeams.CountAsync()` (check capacity)
- `_tournamentTeamRepository.Add()`
- `_tournamentTeamRepository.Save()`

**Called By**: TeamsController.AddTeamToTournament() ✓

**Status**: ✓ USED

---

## MatchService

**Location**: `src/Application/Services/MatchService.cs`

**Dependencies**:
- `IStandingService`
- `ITournamentService`
- `IGenericRepository<Match>`
- `IGenericRepository<Standing>`
- `IGenericRepository<Bracket>`
- `ITO2DbContext`
- `IMapper`
- `ILogger<MatchService>`

### Methods

#### `GetMatchAsync`
```csharp
Task<Match> GetMatchAsync(long id)
```
**Purpose**: Retrieves a single match by ID

**Returns**: `Match`

**Method Calls**:
- `_matchRepository.Get(id)`

**Called By**:
- MatchesController.GetMatch() ✓
- GameService.SetGameResult() ✓
- GameService.DetermineMatchWinner() ✓

**Status**: ✓ USED (3 callers)

---

#### `GetMatchesAsync`
```csharp
Task<List<Match>> GetMatchesAsync(long standingId)
```
**Purpose**: Gets all matches for a standing

**Returns**: `List<Match>`

**Method Calls**:
- `_matchRepository.GetAllByFK("StandingId", standingId)`

**Called By**:
- MatchesController.GetMatches() ✓
- MatchService.CheckAndGenerateNextRound() ✓
- StandingsController.GenerateGames() ✓

**Status**: ✓ USED (3 callers)

---

#### `GenerateMatch`
```csharp
Task<long> GenerateMatch(Team teamA, Team teamB, int round, int seed, long standingId)
```
**Purpose**: Creates a match between two teams

**Returns**: `Task<long>` (match ID)

**Method Calls**:
- `_matchRepository.Add(match)`
- Note: Does NOT call Save() - relies on caller to save

**Called By**:
- MatchService.SeedGroups() ✓
- MatchService.SeedBracket() ✓
- MatchService.CheckAndGenerateNextRound() ✓

**Status**: ✓ USED (3 callers, all internal)

**Design Note**: This method does not save changes - relies on EF Core change tracking and caller to save. This is a deliberate pattern for batch operations.

---

#### `SeedGroups`
```csharp
Task<SeedGroupsResponseDTO> SeedGroups(long tournamentId)
```
**Purpose**: Seeds group stages with teams and generates round-robin matches

**Returns**: `SeedGroupsResponseDTO`

**Algorithm**:
1. Gets all standings
2. Checks if already seeded
3. Gets teams from tournament
4. Randomly distributes teams across groups
5. Creates GroupEntry records
6. Generates round-robin matches (all vs all)
7. Marks standings as seeded

**Method Calls**:
- `_standingService.GetStandingsAsync(tournamentId)`
- `_tournamentService.GetTeamsByTournamentAsync(tournamentId)`
- `_mapper.Map<List<Team>>(teamsDTO)`
- `_dbContext.GroupEntries.FirstOrDefaultAsync()`
- `_dbContext.GroupEntries.AddAsync()`
- `GenerateMatch()` (multiple times)
- `_standingRepository.Update()`
- `_standingRepository.Save()`
- `_dbContext.SaveChangesAsync()`

**Called By**: StandingsController.GenerateGroupMatches() ✓

**Status**: ✓ USED

**Design Note**: Uses TWO DbContext instances (_standingRepository's context and _dbContext). This could cause issues with transaction boundaries.

---

#### `SeedBracket`
```csharp
Task<BracketSeedResponseDTO> SeedBracket(long tournamentId, List<BracketSeedDTO> advancedTeams)
```
**Purpose**: Seeds bracket with teams advancing from groups

**Returns**: `BracketSeedResponseDTO`

**Algorithm**:
1. Gets bracket standing
2. Checks if already seeded
3. Pairs teams: highest vs lowest from different groups
4. Creates matches with sequential seeds
5. Creates BracketEntry records
6. Marks bracket as seeded

**Method Calls**:
- `_standingService.GetStandingsAsync(tournamentId)`
- `_dbContext.Teams.FindAsync()`
- `GenerateMatch()`
- `_bracketRepository.Add()`
- `_standingRepository.Update()`

**Called By**: TournamentLifecycleService.SeedBracketIfReady() ✓

**Status**: ✓ USED

**Design Note**: Does NOT save changes at the end - relies on caller to save.

---

#### `CheckAndGenerateNextRound`
```csharp
Task CheckAndGenerateNextRound(long tournamentId, long standingId, int currentRound)
```
**Purpose**: Checks if round is complete and generates next round or declares champion

**Returns**: `Task` (void)

**Algorithm**:
1. Gets all matches in current round
2. Checks if all matches complete
3. If only 1 match (finals): declares champion
4. Otherwise: pairs winners sequentially for next round
5. Updates BracketEntry status and CurrentRound

**Method Calls**:
- `GetMatchesAsync(standingId)`
- `_tournamentService.DeclareChampion()` (if finals)
- `_dbContext.Teams.FindAsync()`
- `GenerateMatch()` (multiple times)
- `_dbContext.BracketEntries.Where().ToListAsync()`
- `_dbContext.SaveChangesAsync()`

**Called By**: GameService.UpdateStandingAfterMatch() ✓

**Status**: ✓ USED

---

## GameService

**Location**: `src/Application/Services/GameService.cs`

**Dependencies**:
- `IGenericRepository<Game>`
- `IGenericRepository<Match>`
- `IGenericRepository<Standing>`
- `ITO2DbContext`
- `IStandingService`
- `IMatchService`
- `ITournamentLifecycleService`
- `ILogger<GameService>`

### Methods

#### `GenerateGames`
```csharp
Task GenerateGames(long matchId)
```
**Purpose**: Creates game records for a match based on BestOf setting

**Returns**: `Task` (void)

**Method Calls**:
- `_matchService.GetMatchAsync(matchId)`
- `_gameRepository.AddRange(games)`
- `_gameRepository.Save()`

**Called By**: StandingsController.GenerateGames() ✓

**Status**: ✓ USED

---

#### `GetGameAsync`
```csharp
Task<Game> GetGameAsync(long gameId)
```
**Purpose**: Retrieves a single game by ID

**Returns**: `Game`

**Method Calls**:
- `_gameRepository.Get(gameId)`

**Called By**: MatchesController.GetGame() ✓

**Status**: ✓ USED

---

#### `GetAllGamesByMatch`
```csharp
Task<List<Game>> GetAllGamesByMatch(long matchId)
```
**Purpose**: Gets all games for a match

**Returns**: `List<Game>`

**Method Calls**:
- `_gameRepository.GetAllByFK("MatchId", matchId)`

**Called By**:
- MatchesController.GetGames() ✓
- GameService.DetermineMatchWinner() ✓

**Status**: ✓ USED (2 callers)

---

#### `SetGameResult`
```csharp
Task<MatchResultDTO?> SetGameResult(long gameId, SetGameResultDTO request)
```
**Purpose**: Sets the result of a single game and checks if match is complete

**Returns**: `Task<MatchResultDTO?>` (nullable - only returns value if match completes)

**Algorithm**:
1. Sets game winner and optional scores
2. Determines if match has a winner
3. If match complete: delegates to TournamentLifecycleService
4. Returns enriched DTO with tournament state info

**Method Calls**:
- `_gameRepository.Get(gameId)`
- `_matchService.GetMatchAsync()`
- `_gameRepository.Update()`
- `_gameRepository.Save()`
- `DetermineMatchWinner()`
- `_standingRepository.Get()`
- `_lifecycleService.OnMatchCompleted()` (if match complete)

**Called By**: MatchesController.SetGameResult() ✓

**Status**: ✓ USED

---

#### `DetermineMatchWinner`
```csharp
Task<MatchResult?> DetermineMatchWinner(long matchId)
```
**Purpose**: Checks if a team has won enough games to win the match

**Returns**: `MatchResult?` (nullable internal type)

**Algorithm**:
1. Calculates games needed to win (Bo1=1, Bo3=2, Bo5=3)
2. Groups games by winner
3. If a team reached threshold: sets match winner/loser
4. Calls UpdateStandingAfterMatch

**Method Calls**:
- `_matchService.GetMatchAsync(matchId)`
- `_gameRepository.GetAllByFK("MatchId", matchId)`
- `_matchRepository.Update()`
- `_matchRepository.Save()`
- `UpdateStandingAfterMatch(match)`

**Called By**: GameService.SetGameResult() ✓

**Status**: ✓ USED (internal only)

---

#### `UpdateStandingAfterMatch`
```csharp
Task UpdateStandingAfterMatch(Match match)
```
**Purpose**: Updates group/bracket standings based on match result

**Returns**: `Task` (void)

**Algorithm**:
- **For Group standings**: Updates Wins, Losses, Points
- **For Bracket standings**: Sets winner to Advanced, loser to Eliminated, then calls CheckAndGenerateNextRound

**Method Calls**:
- `_standingRepository.Get(match.StandingId)`
- `_dbContext.GroupEntries.FirstOrDefaultAsync()` (for groups)
- `_dbContext.BracketEntries.FirstOrDefaultAsync()` (for brackets)
- `_dbContext.SaveChangesAsync()`
- `_matchService.CheckAndGenerateNextRound()` (for brackets only)

**Called By**: GameService.DetermineMatchWinner() ✓

**Status**: ✓ USED (internal only)

---

## StandingService

**Location**: `src/Application/Services/StandingService.cs`

**Dependencies**:
- `IGenericRepository<Standing>`
- `IGenericRepository<Match>`
- `IGenericRepository<Tournament>`
- `ITO2DbContext`
- `ILogger<StandingService>`

### Methods

#### `GenerateStanding`
```csharp
Task GenerateStanding(long tournamentId, string name, StandingType type, int? teamsPerStanding)
```
**Purpose**: Creates a standing (bracket or group) for a tournament

**Returns**: `Task` (void)

**Method Calls**:
- `_standingRepository.Add(standing)`
- `_standingRepository.Save()`

**Called By**: TournamentService.CreateTournamentAsync() ✓

**Status**: ✓ USED

---

#### `GetStandingsAsync`
```csharp
Task<List<Standing>> GetStandingsAsync(long tournamentId)
```
**Purpose**: Gets all standings for a tournament

**Returns**: `List<Standing>`

**Method Calls**:
- `_standingRepository.GetAllByFK("TournamentId", tournamentId)`

**Called By**:
- StandingsController.GetAll() ✓
- TournamentService.DeclareChampion() ✓
- TournamentService.GetChampion() ✓
- TournamentService.GetFinalStandings() ✓
- MatchService.SeedGroups() ✓
- MatchService.SeedBracket() ✓
- StandingService.PrepareTeamsForBracket() ✓
- TournamentLifecycleService.SeedBracketIfReady() ✓

**Status**: ✓ USED (8 callers - HEAVILY USED)

---

#### `CheckAndMarkStandingAsFinishedAsync`
```csharp
Task<bool> CheckAndMarkStandingAsFinishedAsync(long tournamentId)
```
**Purpose**: Checks all seeded unfinished standings and marks as finished if all matches complete

**Returns**: `Task<bool>` (true if any standing finished)

**Method Calls**:
- `GetStandingsAsync(tournamentId)`
- `_matchRepository.GetAllByFK("StandingId", standing.Id)`
- `_standingRepository.Update()`
- `_standingRepository.Save()`

**Called By**: TournamentLifecycleService.OnMatchCompleted() ✓

**Status**: ✓ USED

**Design Note**: Replaced old domain event system. This is part of explicit lifecycle management.

---

#### `CheckAndMarkAllGroupsAreFinishedAsync`
```csharp
Task<bool> CheckAndMarkAllGroupsAreFinishedAsync(long tournamentId)
```
**Purpose**: Checks if ALL groups are finished

**Returns**: `Task<bool>` (true if all groups finished)

**Method Calls**:
- `_standingRepository.GetAllByFK("TournamentId", tournamentId)`

**Called By**:
- TournamentLifecycleService.OnMatchCompleted() ✓
- StandingsController.FinishGroupsAsync() ✓

**Status**: ✓ USED (2 callers)

**Design Note**: This is a query method only - doesn't modify state. Name is misleading ("Mark" suggests mutation).

---

#### `PrepareTeamsForBracket`
```csharp
Task<List<BracketSeedDTO>> PrepareTeamsForBracket(long tournamentId)
```
**Purpose**: Ranks teams in each group and marks advancing/eliminated

**Returns**: `List<BracketSeedDTO>` (teams with placement info)

**Algorithm**:
1. Calculates teams advancing per group
2. Ranks teams by Points → Wins → Losses
3. Marks top teams as Advanced
4. Marks remaining as Eliminated
5. Returns advancing teams with placement

**Method Calls**:
- `_tournamentRepository.Get(tournamentId)`
- `GetStandingsAsync(tournamentId)`
- `_dbContext.GroupEntries.Where().OrderByDescending().ToListAsync()`

**Called By**: TournamentLifecycleService.SeedBracketIfReady() ✓

**Status**: ✓ USED

**Design Note**: Modifies GroupEntry status but relies on caller to save changes (EF Core change tracking).

---

## TournamentLifecycleService

**Location**: `src/Application/Services/TournamentLifecycleService.cs`

**Purpose**: Explicit state machine for tournament lifecycle management. Replaces domain event-based implicit state transitions.

**Dependencies**:
- `ILogger<TournamentLifecycleService>`
- `IStandingService`
- `IMatchService`
- `IGenericRepository<Standing>`

### Methods

#### `OnMatchCompleted`
```csharp
Task<MatchResultDTO> OnMatchCompleted(long matchId, long winnerId, long loserId, long tournamentId)
```
**Purpose**: Central orchestration point when a match completes

**Returns**: `MatchResultDTO` (enriched with lifecycle state)

**Algorithm**:
1. Check if this match completion causes standing to finish
2. If standing finished: check if ALL groups finished
3. If all groups finished: seed bracket
4. Return enriched DTO with lifecycle state info

**Method Calls**:
- `_standingService.CheckAndMarkStandingAsFinishedAsync()`
- `_standingService.CheckAndMarkAllGroupsAreFinishedAsync()`
- `SeedBracketIfReady()`

**Called By**: GameService.SetGameResult() ✓

**Status**: ✓ USED

**Design Note**: This is the NEW explicit state machine. Replaces the old domain event system.

---

#### `SeedBracketIfReady`
```csharp
Task<BracketSeedResponseDTO> SeedBracketIfReady(long tournamentId)
```
**Purpose**: Seeds bracket when all groups are finished

**Returns**: `BracketSeedResponseDTO`

**Algorithm**:
1. Gets bracket standing
2. Checks if already seeded (idempotent)
3. Prepares advancing teams from groups
4. Delegates to MatchService.SeedBracket()

**Method Calls**:
- `_standingService.GetStandingsAsync(tournamentId)`
- `_standingService.PrepareTeamsForBracket(tournamentId)`
- `_matchService.SeedBracket(tournamentId, advancingTeams)`

**Called By**: TournamentLifecycleService.OnMatchCompleted() ✓

**Status**: ✓ USED (internal only)

---

## Design Analysis

### Architecture Patterns

#### 1. Orchestration Pattern
**TournamentLifecycleService** acts as a saga/orchestrator for tournament state transitions.
- Replaced implicit domain events with explicit method calls
- Centralized lifecycle logic
- Better testability and debuggability

#### 2. Repository Pattern
All services use `IGenericRepository<T>` for data access.
- Consistent interface
- Saves are explicit (`Save()` must be called)
- Some methods rely on EF Core change tracking without saving

#### 3. Service Layer Pattern
Clear separation of concerns:
- **TournamentService**: Tournament CRUD
- **TeamService**: Team CRUD + registration
- **MatchService**: Match generation + bracket logic
- **GameService**: Game-level scoring + match completion
- **StandingService**: Standing management + queries
- **TournamentLifecycleService**: State machine orchestration

---

### Design Flaws

#### CRITICAL Issues

##### 1. Multiple DbContext Usage (SeedGroups)
**Location**: `MatchService.SeedGroups()`

**Issue**: Uses TWO DbContext instances:
- `_standingRepository.Save()` (repository's context)
- `_dbContext.SaveChangesAsync()` (injected context)

**Risk**:
- Potential transaction boundary issues
- Changes might be committed at different times
- Could cause race conditions

**Fix**: Use single DbContext or explicit transaction coordination

---

##### 2. Inconsistent Save Patterns
**Locations**: Multiple services

**Issue**: Some methods save, others rely on caller to save:
- `GenerateMatch()` - does NOT save
- `SeedBracket()` - does NOT save
- `PrepareTeamsForBracket()` - does NOT save (but modifies entities)
- `GenerateStanding()` - DOES save

**Risk**:
- Unclear ownership of transaction boundaries
- Easy to forget to save
- Difficult to reason about consistency

**Fix**: Document pattern clearly or make consistent (all save or none save)

---

##### 3. Method Name Misleading
**Location**: `StandingService.CheckAndMarkAllGroupsAreFinishedAsync()`

**Issue**: Name suggests it marks groups as finished, but it only checks/queries

**Fix**: Rename to `AreAllGroupsFinishedAsync()` or `CheckIfAllGroupsFinished()`

---

#### MODERATE Issues

##### 4. Circular Service Dependencies
**Services have circular references**:
- TournamentService → StandingService
- TournamentService → TeamService
- MatchService → TournamentService
- MatchService → StandingService
- GameService → MatchService
- GameService → StandingService
- GameService → TournamentLifecycleService
- TournamentLifecycleService → MatchService
- TournamentLifecycleService → StandingService

**Risk**:
- Tight coupling
- Difficult to test in isolation
- Potential for dependency injection issues

**Fix**: Consider extracting query services or using CQRS pattern

---

##### 5. Direct DbContext Usage Alongside Repositories
**Locations**: Multiple services

Services inject BOTH `IGenericRepository<T>` AND `ITO2DbContext`:
- TournamentService
- TeamService
- MatchService
- GameService
- StandingService

**Why**: Need to query related entities (TournamentTeams, GroupEntries, BracketEntries) which don't have repositories.

**Issue**: Breaks repository abstraction

**Fix**: Either:
- Create repositories for all entities
- OR drop repository pattern and use DbContext directly
- OR use specification pattern for complex queries

---

##### 6. Tight Coupling to EF Core Change Tracking
**Locations**: `GenerateMatch()`, `PrepareTeamsForBracket()`, `SeedBracket()`

**Issue**: Methods modify entities and rely on EF Core change tracking + caller to save

**Risk**:
- Breaks if not using EF Core
- Implicit behavior (not obvious from method signature)
- Easy to forget to save

**Fix**: Either save within method OR return modified entities explicitly

---

#### MINOR Issues

##### 7. Missing Null Checks
**Location**: `TeamService.GetTeamAsync()`

Called by `TournamentService.RemoveTeamFromTournamentAsync()`:
```csharp
var existingTeam = await _teamService.GetTeamAsync(teamId) ?? throw new Exception("Team not found");
```

But `GetTeamAsync` can throw exception internally, so null check is redundant.

**Fix**: Remove null check or make GetTeamAsync consistently return null

---

##### 8. Generic Exception Throwing
**Locations**: Throughout all services

Services throw `new Exception()` with string messages instead of custom exceptions.

**Risk**:
- Difficult to catch specific errors
- No structured error handling
- Poor error codes for API

**Fix**: Create domain-specific exception types (e.g., `TournamentNotFoundException`, `RegistrationClosedException`)

---

##### 9. Inconsistent Return Types
Some methods return DTOs, others return domain entities:
- `GetMatchAsync()` → `Match` (domain entity)
- `GetGameAsync()` → `Game` (domain entity)
- `GetTeamAsync()` → `GetTeamResponseDTO` (DTO)

**Risk**: Leaking domain entities to controllers

**Fix**: Consistently return DTOs from service layer

---

##### 10. Hard-Coded Constants
**Location**: `TournamentService.GetFinalStandings()`

```csharp
if (placement > 8)
    break;
```

**Issue**: Magic number 8 hard-coded

**Fix**: Make configurable or use named constant

---

### Performance Issues

#### 1. N+1 Query Potential
**Locations**:
- `SeedGroups()` - Loops through teams and checks `GroupEntries.FirstOrDefaultAsync()`
- `CheckAndGenerateNextRound()` - Loops through winners and calls `FindAsync()`

**Risk**: Multiple database round-trips

**Fix**: Load entities in batch with single query

---

#### 2. Missing Indexes
Cannot verify from code, but these queries should have indexes:
- `TournamentTeams.TournamentId` (FK - likely has index)
- `TournamentTeams.TeamId` (FK - likely has index)
- `GroupEntries.StandingId` (FK)
- `BracketEntries.StandingId` (FK)
- `Match.StandingId` (FK)
- `Game.MatchId` (FK)

---

### Positive Design Patterns

#### 1. TournamentLifecycleService
- Excellent explicit state machine
- Replaces complex domain events
- Clear control flow
- Easy to test

#### 2. Validation in TeamService.AddTeamToTournamentAsync
- Comprehensive validation
- Business rules enforced at service layer
- Clear error messages

#### 3. Logging
- Extensive logging throughout
- Helps with debugging tournament flow
- Good use of structured logging

---

## Unused Methods

### NONE!
All service methods are used by either:
- Controllers (API endpoints)
- Other services (internal orchestration)

**This is a good sign** - no dead code in services.

---

## Method Call Graph

### Tournament Creation Flow
```
TournamentsController.Post()
  └─> TournamentService.CreateTournamentAsync()
      ├─> _tournamentRepository.Add()
      ├─> _tournamentRepository.Save()
      ├─> _standingService.GenerateStanding() [Main Bracket]
      └─> _standingService.GenerateStanding() [Groups x N]
          └─> _standingRepository.Add()
          └─> _standingRepository.Save()
```

### Tournament Start & Group Seeding Flow
```
TournamentsController.StartTournament()
  └─> TournamentService.StartTournament()
      └─> Sets IsRegistrationOpen = false

StandingsController.GenerateGroupMatches()
  └─> MatchService.SeedGroups()
      ├─> _standingService.GetStandingsAsync()
      ├─> _tournamentService.GetTeamsByTournamentAsync()
      ├─> For each group:
      │   ├─> _dbContext.GroupEntries.AddAsync() [for each team]
      │   └─> GenerateMatch() [all vs all]
      └─> _dbContext.SaveChangesAsync()
```

### Game Completion Flow
```
MatchesController.SetGameResult()
  └─> GameService.SetGameResult()
      ├─> _gameRepository.Update()
      ├─> _gameRepository.Save()
      └─> DetermineMatchWinner()
          ├─> Checks if team won enough games
          ├─> _matchRepository.Update()
          ├─> _matchRepository.Save()
          └─> UpdateStandingAfterMatch()
              ├─> For Group: Update GroupEntries (wins/losses/points)
              └─> For Bracket:
                  ├─> Update BracketEntries (status)
                  └─> CheckAndGenerateNextRound()
                      ├─> If finals: DeclareChampion()
                      │   └─> _tournamentService.DeclareChampion()
                      │       ├─> Sets tournament status to Finished
                      │       └─> Marks BracketEntry as Champion
                      └─> Otherwise: GenerateMatch() for next round
```

### Lifecycle: Match → Group Finish → Bracket Seeding Flow
```
MatchesController.SetGameResult()
  └─> GameService.SetGameResult()
      └─> DetermineMatchWinner()
          └─> UpdateStandingAfterMatch()
              └─> If Group match complete:
                  └─> [No immediate action - waits for all groups]

GameService.SetGameResult() [continued]
  └─> _lifecycleService.OnMatchCompleted()
      ├─> _standingService.CheckAndMarkStandingAsFinishedAsync()
      │   └─> If standing finished: mark IsFinished = true
      ├─> _standingService.CheckAndMarkAllGroupsAreFinishedAsync()
      │   └─> Returns true if ALL groups finished
      └─> If all groups finished:
          └─> SeedBracketIfReady()
              ├─> _standingService.PrepareTeamsForBracket()
              │   └─> Rank teams, mark Advanced/Eliminated
              └─> _matchService.SeedBracket()
                  ├─> Pair highest vs lowest from different groups
                  ├─> GenerateMatch() for each pairing
                  └─> Create BracketEntry records
```

### Query Flows

#### Get Tournament with Teams
```
TournamentsController.GetByTournament()
  └─> TournamentService.GetTeamsByTournamentAsync()
      └─> _dbContext.TournamentTeams.Where().Select().ToListAsync()
```

#### Get Champion
```
TournamentsController.GetChampion()
  └─> TournamentService.GetChampion()
      ├─> _standingService.GetStandingsAsync()
      └─> _dbContext.BracketEntries.FirstOrDefaultAsync() [Status = Champion]
```

#### Get Group Standings
```
TeamsController.GetTeamsWithStats()
  └─> TeamService.GetTeamsWithStatsAsync()
      └─> _dbContext.GroupEntries.Where().ToListAsync()
```

---

## Service Call Matrix

| Caller Service | Called Service | Method | Count |
|---------------|---------------|---------|-------|
| TournamentService | StandingService | GetStandingsAsync() | 3 |
| TournamentService | StandingService | GenerateStanding() | 1+ |
| TournamentService | TeamService | GetTeamAsync() | 1 |
| MatchService | TournamentService | GetTeamsByTournamentAsync() | 1 |
| MatchService | TournamentService | DeclareChampion() | 1 |
| MatchService | StandingService | GetStandingsAsync() | 2 |
| GameService | MatchService | GetMatchAsync() | 2 |
| GameService | MatchService | CheckAndGenerateNextRound() | 1 |
| GameService | StandingService | (none - uses repository) | 0 |
| StandingService | (none - uses repositories) | - | 0 |
| TournamentLifecycleService | StandingService | CheckAndMarkStandingAsFinishedAsync() | 1 |
| TournamentLifecycleService | StandingService | CheckAndMarkAllGroupsAreFinishedAsync() | 1 |
| TournamentLifecycleService | StandingService | GetStandingsAsync() | 1 |
| TournamentLifecycleService | StandingService | PrepareTeamsForBracket() | 1 |
| TournamentLifecycleService | MatchService | SeedBracket() | 1 |

---

## Recommended Fixes Priority

### High Priority
1. **Fix SeedGroups multiple DbContext usage** - Could cause data corruption
2. **Document or fix inconsistent Save patterns** - Critical for maintainability
3. **Rename CheckAndMarkAllGroupsAreFinishedAsync** - Misleading name

### Medium Priority
4. **Create custom exception types** - Better error handling
5. **Make entity return types consistent (all DTOs)** - Better abstraction
6. **Add batch loading to prevent N+1 queries** - Performance

### Low Priority
7. **Remove redundant null checks** - Code clarity
8. **Extract magic numbers to constants** - Maintainability
9. **Consider CQRS or query services** - Reduce circular dependencies

---

## Notes for Sequence Diagrams

When creating sequence diagrams, focus on these key flows:

1. **Tournament Creation** (Simple)
2. **Group Seeding** (Moderate complexity)
3. **Game Scoring → Match Completion** (Moderate complexity)
4. **Match Completion → Group Finish → Bracket Seeding** (Complex - involves TournamentLifecycleService)
5. **Bracket Round Progression** (Moderate complexity)
6. **Finals → Champion Declaration** (Simple)

The most interesting flow for design understanding is **#4** - the lifecycle transition from groups to bracket.

---

## Summary

**Total Services**: 6
**Total Methods**: 38
**Unused Methods**: 0
**Critical Issues**: 3
**Moderate Issues**: 3
**Minor Issues**: 4

**Overall Assessment**:
- Well-structured service layer
- Good separation of concerns
- No dead code
- Main issues are around transaction boundaries and consistency patterns
- TournamentLifecycleService is an excellent architectural pattern
- Would benefit from standardization of error handling and save patterns
