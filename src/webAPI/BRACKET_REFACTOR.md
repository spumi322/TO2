 Bracket Component Refactor Plan

 Goal

 Refactor bracket component to follow group component pattern: reduce 5-7 API calls per game to 1-2, move match result calculation to backend, create specialized repositories,
 maintain brackets-viewer library compatibility.

 Current Issues

 - 5-7 API calls per game scored (1 + N forkJoin + 1 + N reload)
 - Frontend calculates match results from games
 - No specialized repositories (uses generic IRepository)
 - Returns raw entities (no DTOs)
 - Full bracket re-render after every score

 Target State

 - 2 API calls per game (score + reload)
 - Backend calculates TeamAWins/TeamBWins
 - Specialized repositories (IMatchRepository with eager loading)
 - DTOs (BracketWithDetailsDTO structure)
 - Single efficient endpoint GET /standings/{tournamentId}/bracket

 Implementation Steps

 Backend - Phase 1: Repository Layer

 1.1 Create IMatchRepository
 File: src/Application/Contracts/Repositories/IMatchRepository.cs

 public interface IMatchRepository : IRepository<Match>
 {
     Task<IReadOnlyList<Match>> GetByStandingIdWithGamesAsync(long standingId);
     Task<IReadOnlyList<Match>> GetBracketMatchesWithGamesAsync(long tournamentId);
 }

 1.2 Implement MatchRepository
 File: src/Infrastructure/Persistence/Repository/MatchRepository.cs

 public class MatchRepository : Repository<Match>, IMatchRepository
 {
     public async Task<IReadOnlyList<Match>> GetByStandingIdWithGamesAsync(long standingId)
     {
         return await _dbSet
             .Where(m => m.StandingId == standingId)
             .Include(m => m.Games)  // Eager load - eliminates N+1
             .OrderBy(m => m.Round)
             .ThenBy(m => m.Seed)
             .ToListAsync();
     }

     public async Task<IReadOnlyList<Match>> GetBracketMatchesWithGamesAsync(long tournamentId)
     {
         return await _dbSet
             .Where(m => m.Standing.TournamentId == tournamentId
                      && m.Standing.StandingType == StandingType.Bracket)
             .Include(m => m.Games)
             .OrderBy(m => m.Round)
             .ThenBy(m => m.Seed)
             .ToListAsync();
     }
 }

 1.3 Update IStandingRepository
 Add method to src/Application/Contracts/Repositories/IStandingRepository.cs:

 Task<Standing?> GetBracketWithMatchesAsync(long tournamentId);

 Implement in StandingRepository.cs:
 public async Task<Standing?> GetBracketWithMatchesAsync(long tournamentId)
 {
     return await _dbSet
         .Where(s => s.TournamentId == tournamentId && s.StandingType == StandingType.Bracket)
         .Include(s => s.Matches)
         .ThenInclude(m => m.Games)  // Nested eager loading
         .FirstOrDefaultAsync();
 }

 Backend - Phase 2: DTO Layer & Shared Utilities

 2.1 Create Shared Base DTOs
 File: src/Application/DTOs/Standing/StandingMatchBaseDTO.cs

 // Shared base for GroupMatchDTO and BracketMatchDTO
 public record StandingMatchBaseDTO
 {
     public long Id { get; init; }
     public long StandingId { get; init; }
     public int? Round { get; init; }
     public int? Seed { get; init; }
     public long? TeamAId { get; init; }
     public long? TeamBId { get; init; }
     public long? WinnerId { get; init; }
     public long? LoserId { get; init; }
     public int BestOf { get; init; }

     // Backend-calculated results (shared logic)
     public int TeamAWins { get; init; }
     public int TeamBWins { get; init; }
 }

 File: src/Application/DTOs/Standing/StandingGameDTO.cs

 // Shared game DTO (used by both groups and brackets)
 public record StandingGameDTO
 {
     public long Id { get; init; }
     public long MatchId { get; init; }
     public long? TeamAId { get; init; }
     public int? TeamAScore { get; init; }
     public long? TeamBId { get; init; }
     public int? TeamBScore { get; init; }
     public long? WinnerId { get; init; }
 }

 2.2 Create BracketMatchDTO (inherits base)
 File: src/Application/DTOs/Standing/BracketMatchDTO.cs

 public record BracketMatchDTO : StandingMatchBaseDTO
 {
     public List<StandingGameDTO> Games { get; init; } = new();
 }

 2.3 Update GroupMatchDTO (inherits base)
 File: src/Application/DTOs/Standing/GroupMatchDTO.cs - UPDATE

 // Update existing GroupMatchDTO to inherit from base
 public record GroupMatchDTO : StandingMatchBaseDTO
 {
     public List<StandingGameDTO> Games { get; init; } = new();
 }

 2.4 Create GetBracketWithDetailsResponseDTO
 File: src/Application/DTOs/Standing/GetBracketWithDetailsResponseDTO.cs

 public record GetBracketWithDetailsResponseDTO
 {
     public long Id { get; init; }
     public string Name { get; init; } = string.Empty;
     public bool IsFinished { get; init; }
     public bool IsSeeded { get; init; }
     public List<BracketMatchDTO> Matches { get; init; } = new();
 }

 2.5 Create Shared DTO Mapper Utility
 File: src/Application/Utilities/StandingDTOMapper.cs - NEW

 namespace Application.Utilities
 {
     public static class StandingDTOMapper
     {
         /// <summary>
         /// Calculates TeamAWins and TeamBWins from games (shared logic)
         /// </summary>
         public static (int TeamAWins, int TeamBWins) CalculateMatchResults(
             IEnumerable<Game> games,
             long? teamAId,
             long? teamBId)
         {
             int teamAWins = games.Count(g => g.WinnerId == teamAId);
             int teamBWins = games.Count(g => g.WinnerId == teamBId);
             return (teamAWins, teamBWins);
         }

         /// <summary>
         /// Maps Game entity to StandingGameDTO (shared logic)
         /// </summary>
         public static StandingGameDTO MapGameToDTO(Game game)
         {
             return new StandingGameDTO
             {
                 Id = game.Id,
                 MatchId = game.MatchId,
                 TeamAId = game.TeamAId,
                 TeamAScore = game.TeamAScore,
                 TeamBId = game.TeamBId,
                 TeamBScore = game.TeamBScore,
                 WinnerId = game.WinnerId
             };
         }

         /// <summary>
         /// Maps Match entity to BracketMatchDTO (reduces duplication)
         /// </summary>
         public static BracketMatchDTO MapToBracketMatchDTO(Match match)
         {
             var (teamAWins, teamBWins) = CalculateMatchResults(
                 match.Games,
                 match.TeamAId,
                 match.TeamBId
             );

             return new BracketMatchDTO
             {
                 Id = match.Id,
                 StandingId = match.StandingId,
                 Round = match.Round,
                 Seed = match.Seed,
                 TeamAId = match.TeamAId,
                 TeamBId = match.TeamBId,
                 WinnerId = match.WinnerId,
                 LoserId = match.LoserId,
                 BestOf = (int)match.BestOf,
                 TeamAWins = teamAWins,
                 TeamBWins = teamBWins,
                 Games = match.Games.Select(MapGameToDTO).ToList()
             };
         }

         /// <summary>
         /// Maps Match entity to GroupMatchDTO (can also use this in GetGroupsWithDetailsAsync)
         /// </summary>
         public static GroupMatchDTO MapToGroupMatchDTO(Match match)
         {
             var (teamAWins, teamBWins) = CalculateMatchResults(
                 match.Games,
                 match.TeamAId,
                 match.TeamBId
             );

             return new GroupMatchDTO
             {
                 Id = match.Id,
                 StandingId = match.StandingId,
                 Round = match.Round,
                 Seed = match.Seed,
                 TeamAId = match.TeamAId,
                 TeamBId = match.TeamBId,
                 WinnerId = match.WinnerId,
                 LoserId = match.LoserId,
                 BestOf = (int)match.BestOf,
                 TeamAWins = teamAWins,
                 TeamBWins = teamBWins,
                 Games = match.Games.Select(MapGameToDTO).ToList()
             };
         }
     }
 }

 Backend - Phase 3: Service Layer

 3.1 Add GetBracketWithDetailsAsync to IStandingService
 File: src/Application/Contracts/IStandingService.cs

 Task<GetBracketWithDetailsResponseDTO?> GetBracketWithDetailsAsync(long tournamentId);

 3.2 Implement in StandingService (using shared mapper)
 File: src/Application/Services/StandingService.cs

 using Application.Utilities; // ADD import

 public async Task<GetBracketWithDetailsResponseDTO?> GetBracketWithDetailsAsync(long tournamentId)
 {
     // Single query with nested Include (Standing → Matches → Games)
     var bracketStanding = await _standingRepository.GetBracketWithMatchesAsync(tournamentId);

     if (bracketStanding == null) return null;

     return new GetBracketWithDetailsResponseDTO
     {
         Id = bracketStanding.Id,
         Name = bracketStanding.Name,
         IsFinished = bracketStanding.IsFinished,
         IsSeeded = bracketStanding.IsSeeded,
         // Use shared mapper - eliminates duplication
         Matches = bracketStanding.Matches
             .Select(StandingDTOMapper.MapToBracketMatchDTO)
             .ToList()
     };
 }

 3.3 Update GetGroupsWithDetailsAsync (optional - to use shared mapper)
 File: src/Application/Services/StandingService.cs

 // Optionally update existing group method to use shared mapper
 // This further reduces duplication across codebase
 public async Task<List<GetGroupsWithDetailsResponseDTO>> GetGroupsWithDetailsAsync(long tournamentId)
 {
     var standings = await _standingRepository.GetGroupsWithMatchesAsync(tournamentId);

     var result = new List<GetGroupsWithDetailsResponseDTO>();

     foreach (var standing in standings)
     {
         var groupEntries = await _groupRepository.GetByStandingIdOrderedAsync(standing.Id);

         var dto = new GetGroupsWithDetailsResponseDTO
         {
             // ... team mapping ...

             // Use shared mapper instead of inline mapping
             Matches = standing.Matches
                 .Select(StandingDTOMapper.MapToGroupMatchDTO)
                 .ToList()
         };

         result.Add(dto);
     }

     return result;
 }

 Backend - Phase 4: Controller Layer

 4.1 Add endpoint to StandingsController
 File: src/webAPI/Controllers/StandingsController.cs

 [HttpGet("{tournamentId}/bracket")]
 public async Task<IActionResult> GetBracketWithDetails(long tournamentId)
 {
     var result = await _standingService.GetBracketWithDetailsAsync(tournamentId);

     if (result == null)
     {
         return NotFound(new { message = "Bracket not found for tournament" });
     }

     return Ok(result);
 }

 Endpoint: GET /api/standings/{tournamentId}/bracket

 Backend - Phase 5: Registration

 5.1 Register repository in Program.cs
 File: src/webAPI/Program.cs

 builder.Services.AddScoped<IMatchRepository, MatchRepository>();

 5.2 Update service injections
 Replace IRepository<Match> with IMatchRepository in MatchService and StandingService constructors.

 Frontend - Phase 6: TypeScript Models

 6.1 Update Match interface
 File: src/UI/src/app/models/match.ts

 export interface Match {
   id: number;
   standingId: number;
   round: number;
   seed: number;
   teamAId: number;
   teamBId: number;
   winnerId: number;
   loserId: number;
   bestOf: BestOf;

   // Backend-calculated fields
   teamAWins: number;
   teamBWins: number;

   games: Game[];
   result?: MatchResult;  // Optional - derived from teamAWins/teamBWins
 }

 Frontend - Phase 7: Service Layer

 7.1 Add method to StandingService
 File: src/UI/src/app/services/standing/standing.service.ts

 getBracketWithDetails(tournamentId: number): Observable<Standing | null> {
   return this.http.get<Standing>(`${this.apiUrl}/${tournamentId}/bracket`).pipe(
     catchError(err => {
       console.error('Error loading bracket with details:', err);
       return of(null);
     })
   );
 }

 Frontend - Phase 8: Bracket Component Refactor

 File: src/UI/src/app/components/standing/bracket/bracket.component.ts

 8.1 Update constructor
 constructor(
   private standingService: StandingService,  // ADD
   private matchService: MatchService,
   private bracketAdapter: BracketAdapterService,
   private cdr: ChangeDetectorRef
 ) {}

 8.2 Replace loadMatches() with loadBracket()

 REMOVE:
 - loadMatches() method
 - loadAllMatchScores() method
 - getMatchResults() method (frontend calculation)

 ADD:
 loadBracket(resetUpdatingFlag: number | null = null) {
   if (!this.tournament?.id) {
     this.isLoading = false;
     return;
   }

   this.isLoading = true;

   this.standingService.getBracketWithDetails(this.tournament.id).subscribe({
     next: (bracketStanding) => {
       if (bracketStanding) {
         this.matches = bracketStanding.matches;
         this.standingId = bracketStanding.id;

         // Map backend results to MatchResult structure
         this.matches.forEach(match => {
           match.result = {
             teamAId: match.teamAId,
             teamAWins: match.teamAWins,  // From backend
             teamBId: match.teamBId,
             teamBWins: match.teamBWins   // From backend
           };
         });
       } else {
         this.matches = [];
       }

       this.isLoading = false;

       if (resetUpdatingFlag !== null) {
         this.isUpdating[resetUpdatingFlag] = false;
       }

       if (this.viewInitialized) {
         this.renderBracket();
       }
     },
     error: (err) => {
       console.error('Error loading bracket:', err);
       this.isLoading = false;
       this.matches = [];
       if (resetUpdatingFlag !== null) {
         this.isUpdating[resetUpdatingFlag] = false;
       }
     }
   });
 }

 8.3 Update lifecycle hooks
 ngOnInit() {
   this.loadBracket();  // Changed from loadMatches()
 }

 ngOnChanges() {
   if (this.viewInitialized) {
     this.loadBracket();  // Changed from loadMatches()
   }
 }

 8.4 Simplify scoreGameForTeam()
 private scoreGameForTeam(matchId: number, teamId: number): void {
   const match = this.matches.find(m => m.id === matchId);
   if (!match || match.winnerId || !this.tournament) return;

   this.isUpdating[matchId] = true;

   // Find unfinished game (data already loaded)
   const gameToUpdate = match.games.find(game => !game.winnerId);

   if (!gameToUpdate) {
     console.warn('No unfinished game found for match', matchId);
     this.isUpdating[matchId] = false;
     return;
   }

   const gameResult = {
     gameId: gameToUpdate.id,
     winnerId: teamId,
     teamAScore: undefined,
     teamBScore: undefined,
     matchId: matchId,
     standingId: match.standingId,
     tournamentId: this.tournament.id
   };

   this.matchService.setGameResult(gameResult).subscribe({
     next: (result) => {
       if (result.matchFinished && result.matchWinnerId && result.matchLoserId) {
         match.winnerId = result.matchWinnerId;
         match.loserId = result.matchLoserId;

         this.matchFinished.emit({
           winnerId: result.matchWinnerId,
           loserId: result.matchLoserId,
           allGroupsFinished: result.allGroupsFinished || false,
           tournamentFinished: result.tournamentFinished || false,
           finalStandings: result.finalStandings
         });
       }

       // Single reload call
       this.loadBracket(matchId);
     },
     error: (err) => {
       console.error('Error scoring game:', err);
       this.isUpdating[matchId] = false;
     }
   });
 }

 Note: BracketAdapterService requires no changes - already uses match.result structure.

 Performance Gains

 Before:
 - Load: 1 + N API calls (matches + games per match)
 - Score game: 1 + 1 + 1 + N calls (find unfinished + score + reload matches + reload all games)
 - Total: 5-7 calls per game scored

 After:
 - Load: 1 API call (bracket with details)
 - Score game: 1 + 1 calls (score + reload bracket)
 - Total: 2 calls per game scored

 Reduction: ~70% fewer API calls

 Testing Checklist

 Backend:
 - Create IMatchRepository interface
 - Implement MatchRepository with eager loading
 - Update IStandingRepository.GetBracketWithMatchesAsync()
 - Create shared base DTOs (StandingMatchBaseDTO, StandingGameDTO)
 - Create BracketMatchDTO inheriting from base
 - Update GroupMatchDTO to inherit from base
 - Create StandingDTOMapper utility with shared logic
 - Create GetBracketWithDetailsResponseDTO
 - Add StandingService.GetBracketWithDetailsAsync() using shared mapper
 - (Optional) Update GetGroupsWithDetailsAsync to use shared mapper
 - Add StandingsController.GetBracketWithDetails() endpoint
 - Register IMatchRepository in Program.cs
 - Update service injections to use IMatchRepository

 Frontend:
 - Update Match interface with teamAWins/teamBWins
 - Add StandingService.getBracketWithDetails()
 - Refactor bracket.component.ts (replace loadMatches, remove calculations)
 - Update ngOnInit() and ngOnChanges()
 - Simplify scoreGameForTeam()
 - Test brackets-viewer integration

 Validation:
 - Verify single API call on bracket load
 - Verify 2 calls per game scored
 - Verify match results display correctly
 - Verify bracket renders correctly
 - Test match completion and winner propagation
 - Test tournament finish flow

 Critical Files

 Backend:
 - src/Application/Contracts/Repositories/IMatchRepository.cs - NEW
 - src/Infrastructure/Persistence/Repository/MatchRepository.cs - NEW
 - src/Application/Contracts/Repositories/IStandingRepository.cs - UPDATE
 - src/Infrastructure/Persistence/Repository/StandingRepository.cs - UPDATE
 - src/Application/DTOs/Standing/StandingMatchBaseDTO.cs - NEW (shared base)
 - src/Application/DTOs/Standing/StandingGameDTO.cs - NEW (shared game DTO)
 - src/Application/DTOs/Standing/BracketMatchDTO.cs - NEW (inherits base)
 - src/Application/DTOs/Standing/GroupMatchDTO.cs - UPDATE (inherit from base)
 - src/Application/DTOs/Standing/GetBracketWithDetailsResponseDTO.cs - NEW
 - src/Application/Utilities/StandingDTOMapper.cs - NEW (shared mapping logic)
 - src/Application/Services/StandingService.cs - UPDATE
 - src/webAPI/Controllers/StandingsController.cs - UPDATE
 - src/webAPI/Program.cs - UPDATE

 Frontend:
 - src/UI/src/app/models/match.ts - UPDATE
 - src/UI/src/app/services/standing/standing.service.ts - UPDATE
 - src/UI/src/app/components/standing/bracket/bracket.component.ts - REFACTOR

 Deployment Strategy

 1. Deploy backend (new endpoint, keeps old endpoints for rollback)
 2. Deploy frontend (uses new endpoint)
 3. Monitor for errors
 4. Deprecate old match endpoints after verification

 Scope Note

 Match component (src/UI/src/app/components/matches/) will be refactored separately. This plan focuses ONLY on bracket component refactor.

 Unresolved Questions

 1. Create separate BracketService or keep in StandingService? → Keep in StandingService for consistency
 2. Cache bracket data in frontend? → Add if performance issues arise
 3. Remove old match endpoints immediately? → Keep deprecated for 1 release cycle