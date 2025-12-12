Group Standings Component Refactor

 Problem

 Frontend: Makes 1 + (2N) API calls for N groups with complex RxJS chains
 - 1 call: GET /api/standings/{tournamentId}
 - N calls: GET /api/teams/{standingId}/teams-with-stats per group
 - N calls: GET /api/matches/all/{standingId} per group
 - Total for 4 groups: 9 API calls

 Backend: N+1 query pattern in service layer
 - Current: 1 + (2N) database queries for N groups
 - Example: 4 groups = 9 database queries

 Component Complexity:
 - Nested RxJS operators (switchMap + forkJoin)
 - Client-side sorting of teams by points
 - Dual state management (groups$ observable + groups array)

 Solution

 Create single optimized endpoint returning denormalized data (groups + teams + matches), simplify frontend to single API call with minimal RxJS.

 Performance Improvement

 - API calls: 9 → 1 (89% reduction)
 - Database queries: 9 → 5 (44% reduction)
 - RxJS operators: 4 nested → 1 simple
 - Component lines: 25 → 10 (60% reduction)

 ---
 Implementation Steps

 Backend Changes

 1. Create Optimized Repository Method

 File: G:/Code/TO2/src/Application/Contracts/Repositories/IGroupRepository.cs

 Add method:
 /// <summary>
 /// Gets group entries ordered by points descending. Single query.
 /// </summary>
 Task<IReadOnlyList<Group>> GetByStandingIdOrderedAsync(long standingId);

 File: G:/Code/TO2/src/Infrastructure/Persistence/Repository/GroupRepository.cs

 Implementation:
 public async Task<IReadOnlyList<Group>> GetByStandingIdOrderedAsync(long standingId)
     => await _dbSet
         .Where(g => g.StandingId == standingId)
         .OrderByDescending(g => g.Points)
         .ThenByDescending(g => g.Wins)
         .ThenBy(g => g.Losses)
         .ToListAsync();

 ---
 2. Create Standing Repository with Eager Loading

 File: G:/Code/TO2/src/Application/Contracts/Repositories/IStandingRepository.cs (NEW)

 namespace Application.Contracts.Repositories;

 public interface IStandingRepository : IRepository<Standing>
 {
     /// <summary>
     /// Gets group standings with matches eager loaded.
     /// </summary>
     Task<IReadOnlyList<Standing>> GetGroupsWithMatchesAsync(long tournamentId);
 }

 File: G:/Code/TO2/src/Infrastructure/Persistence/Repository/StandingRepository.cs (NEW)

 using Application.Contracts.Repositories;
 using Domain.AggregateRoots;
 using Domain.Enums;
 using Infrastructure.Persistence;
 using Microsoft.EntityFrameworkCore;

 namespace Infrastructure.Persistence.Repository;

 public class StandingRepository : Repository<Standing>, IStandingRepository
 {
     public StandingRepository(TO2DbContext dbContext) : base(dbContext) { }

     public async Task<IReadOnlyList<Standing>> GetGroupsWithMatchesAsync(long tournamentId)
     {
         return await _dbSet
             .Where(s => s.TournamentId == tournamentId && s.StandingType == StandingType.Group)
             .Include(s => s.Matches)
             .ToListAsync();
     }
 }

 ---
 3. Create Denormalized Response DTO

 File: G:/Code/TO2/src/Application/DTOs/Standing/GetGroupsWithDetailsResponseDTO.cs (NEW)

 namespace Application.DTOs.Standing;

 /// <summary>
 /// Denormalized DTO for group standings display.
 /// Contains all data in single response.
 /// </summary>
 public record GetGroupsWithDetailsResponseDTO
 {
     public long Id { get; init; }
     public string Name { get; init; } = string.Empty;
     public bool IsFinished { get; init; }
     public bool IsSeeded { get; init; }
     public List<GroupTeamDTO> Teams { get; init; } = new();
     public List<GroupMatchDTO> Matches { get; init; } = new();
 }

 public record GroupTeamDTO
 {
     public long Id { get; init; }
     public string Name { get; init; } = string.Empty;
     public int Wins { get; init; }
     public int Losses { get; init; }
     public int Points { get; init; }
     public int Status { get; init; }
 }

 public record GroupMatchDTO
 {
     public long Id { get; init; }
     public long StandingId { get; init; }  // Required by match component for game result
     public int? Round { get; init; }
     public int? Seed { get; init; }
     public long? TeamAId { get; init; }
     public long? TeamBId { get; init; }
     public long? WinnerId { get; init; }
     public long? LoserId { get; init; }
     public int BestOf { get; init; }
 }

 ---
 4. Add Service Method

 File: G:/Code/TO2/src/Application/Contracts/IStandingService.cs

 Add method:
 /// <summary>
 /// Gets all groups with teams and matches in optimized response.
 /// Teams pre-sorted by points descending.
 /// </summary>
 Task<List<GetGroupsWithDetailsResponseDTO>> GetGroupsWithDetailsAsync(long tournamentId);

 File: G:/Code/TO2/src/Application/Services/StandingService.cs

 Add implementation (after line 473):
 public async Task<List<GetGroupsWithDetailsResponseDTO>> GetGroupsWithDetailsAsync(long tournamentId)
 {
     // Get standings with matches eager loaded (2 queries via Include)
     var standings = await _standingRepository.GetGroupsWithMatchesAsync(tournamentId);

     var result = new List<GetGroupsWithDetailsResponseDTO>();

     foreach (var standing in standings)
     {
         // Get teams ordered by points (1 query per standing)
         var groupEntries = await _groupRepository.GetByStandingIdOrderedAsync(standing.Id);

         var dto = new GetGroupsWithDetailsResponseDTO
         {
             Id = standing.Id,
             Name = standing.Name,
             IsFinished = standing.IsFinished,
             IsSeeded = standing.IsSeeded,
             Teams = groupEntries.Select(ge => new GroupTeamDTO
             {
                 Id = ge.TeamId,
                 Name = ge.TeamName,
                 Wins = ge.Wins,
                 Losses = ge.Losses,
                 Points = ge.Points,
                 Status = (int)ge.Status
             }).ToList(),
             Matches = standing.Matches.Select(m => new GroupMatchDTO
             {
                 Id = m.Id,
                 StandingId = m.StandingId,  // Required by match component
                 Round = m.Round,
                 Seed = m.Seed,
                 TeamAId = m.TeamAId,
                 TeamBId = m.TeamBId,
                 WinnerId = m.WinnerId,
                 LoserId = m.LoserId,
                 BestOf = (int)m.BestOf
             }).ToList()
         };

         result.Add(dto);
     }

     return result;
 }

 Note: For 4 groups: 1 query (standings+matches) + 4 queries (teams per group) = 5 total queries

 ---
 5. Replace Controller Endpoint (Breaking Change)

 File: G:/Code/TO2/src/webAPI/Controllers/StandingsController.cs

 Replace existing GET endpoint (around line 20):

 /// <summary>
 /// Gets all group standings with teams and matches.
 /// Optimized endpoint returning denormalized data.
 /// </summary>
 [HttpGet("{tournamentId}")]
 public async Task<IActionResult> GetGroupsWithDetails(long tournamentId)
 {
     var result = await _standingService.GetGroupsWithDetailsAsync(tournamentId);
     return Ok(result);
 }

 Note: This replaces the old GetAll method - breaking change

 ---
 6. Update Service Constructor

 File: G:/Code/TO2/src/Application/Services/StandingService.cs

 Replace IRepository<Standing> with IStandingRepository (line ~40):

 private readonly IStandingRepository _standingRepository;

 public StandingService(
     IStandingRepository standingRepository,  // Changed from IRepository<Standing>
     IRepository<Group> groupRepository,
     // ... other dependencies
 )
 {
     _standingRepository = standingRepository;
     // ... rest of constructor
 }

 ---
 7. Register Dependencies

 File: G:/Code/TO2/src/webAPI/Program.cs

 Find repository registrations section (around line 60-80), add:

 // Specialized repositories
 builder.Services.AddScoped<IStandingRepository, StandingRepository>();
 builder.Services.AddScoped<IRepository<Standing>>(sp => sp.GetRequiredService<IStandingRepository>());

 Note: Dual registration allows injection of either interface

 ---
 Frontend Changes

 8. Simplify Standing Service

 File: G:/Code/TO2/src/UI/src/app/services/standing/standing.service.ts

 Replace method getGroupsWithTeamsByTournamentId (lines 25-35) with:

 getGroupsWithDetails(tournamentId: number): Observable<Standing[]> {
   return this.http.get<Standing[]>(`${this.apiUrl}/${tournamentId}`);
 }

 Remove deprecated methods:
 - Delete getGroupsWithTeamsByTournamentId (lines 25-35)
 - Delete getTeamsWithStatsByStandingId (lines 36-38) - no longer used
 - Keep getStandingsByTournamentId if used elsewhere

 ---
 9. Simplify Group Component

 File: G:/Code/TO2/src/UI/src/app/components/standing/group/group.component.ts

 Replace constructor (remove MatchService dependency):
 constructor(
   private standingService: StandingService,
   // Remove: private matchService: MatchService
 ) {}

 Replace refreshGroups method (lines 30-55):
 refreshGroups(): void {
   if (this.tournament.id) {
     this.standingService.getGroupsWithDetails(this.tournament.id).pipe(
       catchError(() => of([]))
     ).subscribe((groups) => {
       this.groups = groups;
     });
   }
 }

 Remove unused state:
 - Delete groups$: Observable<Standing[]> (line 24) - not needed

 Simplified component:
 - No RxJS switchMap/forkJoin complexity
 - No client-side sorting
 - No MatchService dependency
 - Single observable subscription

 ---
 Match Component Compatibility

 Analysis

 Match component (G:/Code/TO2/src/UI/src/app/components/matches/matches.component.ts) is fully compatible with this refactor and requires zero changes.

 Why Match Component Works As-Is:

 1. Loose Coupling: Match component receives data via @Input properties:
   - @Input() matches: Match[] - Array of match objects
   - @Input() teams: Team[] - Array of team objects for name lookup
   - @Input() isGroupFinished: boolean - Control UI state
   - @Input() tournamentId: number - For game result API calls
 2. Data Structure Compatible: New GroupMatchDTO contains all fields match component needs:
   - id, standingId, teamAId, teamBId, winnerId, loserId, bestOf
   - Match component only reads these properties
 3. Service Independence: Match component only calls MatchService methods:
   - getAllGamesByMatch(matchId) - Load game scores
   - setGameResult(gameResult) - Score a game
   - These methods are NOT modified in refactor
 4. Event Emission: Match component emits generic matchFinished event
   - Parent (group component) handles event and refreshes data
   - Flow remains identical

 Current Flow (Unchanged):
 User clicks score button in Match Component
   ↓
 Match Component: getAllGamesByMatch() → find unscored game
   ↓
 Match Component: setGameResult() → submit score
   ↓
 Match Component: emit matchFinished event
   ↓
 Group Component: onMatchFinished() → refreshGroups()
   ↓
 Group Component: Now uses single API call instead of 9

 Critical Fix Applied:
 - Added StandingId to GroupMatchDTO (line 140)
 - Match component needs this when calling setGameResult() (line 74 in component)
 - Without this field, game scoring would fail

 Result: Match component works perfectly with new group data structure without any code changes.

 ---
 Files Modified Summary

 New Files (3)

 G:/Code/TO2/src/Application/Contracts/Repositories/IStandingRepository.cs
 G:/Code/TO2/src/Infrastructure/Persistence/Repository/StandingRepository.cs
 G:/Code/TO2/src/Application/DTOs/Standing/GetGroupsWithDetailsResponseDTO.cs

 Modified Files (7)

 Backend (5)
 G:/Code/TO2/src/Application/Contracts/Repositories/IGroupRepository.cs
 G:/Code/TO2/src/Infrastructure/Persistence/Repository/GroupRepository.cs
 G:/Code/TO2/src/Application/Contracts/IStandingService.cs
 G:/Code/TO2/src/Application/Services/StandingService.cs
 G:/Code/TO2/src/webAPI/Controllers/StandingsController.cs
 G:/Code/TO2/src/webAPI/Program.cs

 Frontend (2)
 G:/Code/TO2/src/UI/src/app/services/standing/standing.service.ts
 G:/Code/TO2/src/UI/src/app/components/standing/group/group.component.ts

 ---
 Testing Steps

 1. Build backend: cd G:/Code/TO2/src/webAPI && dotnet build
 2. Build frontend: cd G:/Code/TO2/src/UI && npm run build
 3. Start backend: cd G:/Code/TO2/src/webAPI && dotnet run
 4. Start frontend: cd G:/Code/TO2/src/UI && npm start
 5. Navigate to tournament with groups, verify:
   - Groups display correctly
   - Teams sorted by points descending
   - Matches display correctly
   - Single API call in Network tab (was 9 before)

 ---
 Rollback Plan

 If issues occur:
 1. Revert controller endpoint to old GetAll implementation
 2. Revert frontend service method to getGroupsWithTeamsByTournamentId
 3. Revert group component refreshGroups method
 4. Keep new repository/DTO code (harmless if unused)

 ---
 Performance Comparison

 | Metric               | Before   | After    | Improvement |
 |----------------------|----------|----------|-------------|
 | API calls (4 groups) | 9        | 1        | 89%         |
 | Database queries     | 9        | 5        | 44%         |
 | Frontend complexity  | 25 LOC   | 10 LOC   | 60%         |
 | RxJS operators       | 4 nested | 1 simple | 75%         |

 ---
 Notes

 - Breaking change: Old endpoint replaced, not deprecated
 - Coordinated deployment: Deploy backend + frontend together
 - No backward compatibility: Simpler implementation, no code duplication
 - Database sorting: Moved from client (Angular) to database (EF Core OrderBy)
 - Focused scope: Only group component refactored, other N+1 queries untouched
 - Match component: Zero changes required - already optimal and compatible
 - Component independence: Both components can be refactored separately while maintaining current flow
 - Critical fix: Added StandingId to GroupMatchDTO for match component compatibility