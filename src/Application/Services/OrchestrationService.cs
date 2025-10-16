using Application.Contracts;
using Application.DTOs.Match;
using Application.DTOs.Standing;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using Domain.StateMachine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    /// <summary>
    /// Explicit state machine for tournament lifecycle management.
    /// Replaces domain event-based implicit state transitions with clear, testable methods.
    /// </summary>
    public class OrchestrationService : IOrchestrationService
    {
        private readonly ILogger<OrchestrationService> _logger;
        private readonly IStandingService _standingService;
        private readonly IMatchService _matchService;
        private readonly IGenericRepository<Tournament> _tournamentRepository;
        private readonly ITournamentStateMachine _stateMachine;
        private readonly ITO2DbContext _dbContext;

        public OrchestrationService(
            ILogger<OrchestrationService> logger,
            IStandingService standingService,
            IMatchService matchService,
            IGenericRepository<Tournament> tournamentRepository,
            ITournamentStateMachine stateMachine,
            ITO2DbContext dbContext
            )
        {
            _logger = logger;
            _standingService = standingService;
            _matchService = matchService;
            _tournamentRepository = tournamentRepository;
            _stateMachine = stateMachine;
            _dbContext = dbContext;
        }

        public async Task<MatchResultDTO> OnMatchCompleted(long matchId, long winnerId, long loserId, long tournamentId)
        {
            var tournament = await _tournamentRepository.Get(tournamentId)
                ?? throw new Exception("Tournament not found");

            bool standingJustFinished = await _standingService.CheckAndMarkStandingAsFinishedAsync(tournamentId);

            if (standingJustFinished && tournament.Status == TournamentStatus.GroupsInProgress)
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
                        BracketSeeded: false
                    );
                }
            }

            return new MatchResultDTO(winnerId, loserId);
        }

        public async Task<BracketSeedResponseDTO> SeedBracketIfReady(long tournamentId)
        {
            _logger.LogInformation($"=== Tournament Lifecycle: Checking bracket seeding for tournament {tournamentId} ===");

            try
            {
                // 1. Get standings
                var standings = await _standingService.GetStandingsAsync(tournamentId);
                var bracket = standings.FirstOrDefault(s => s.StandingType == StandingType.Bracket);

                if (bracket == null)
                {
                    _logger.LogWarning("No bracket standing found!");
                    return new BracketSeedResponseDTO("Bracket standing not found", false);
                }

                // 2. Check if already seeded (prevent duplicate seeding)
                if (bracket.IsSeeded)
                {
                    _logger.LogInformation("Bracket already seeded. Skipping.");
                    return new BracketSeedResponseDTO("Bracket already seeded", true);
                }

                // 3. Prepare teams from groups
                _logger.LogInformation("Preparing teams for bracket from group standings...");
                var advancingTeams = await _standingService.PrepareTeamsForBracket(tournamentId);

                _logger.LogInformation($"Teams advancing to bracket: {advancingTeams.Count}");
                foreach (var team in advancingTeams)
                {
                    _logger.LogInformation($"  - Team ID {team.TeamId} from Group {team.GroupId} (Placement: {team.Placement})");
                }

                // 4. Seed the bracket
                _logger.LogInformation("Seeding bracket matches...");
                var result = await _matchService.SeedBracket(tournamentId, advancingTeams);

                _logger.LogInformation($"✓ Bracket seeding completed: {result.Message}, Success: {result.Success}");
                _logger.LogInformation("=== Tournament Lifecycle: Bracket seeding finished ===");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error seeding bracket for tournament {tournamentId}: {ex.Message}");
                return new BracketSeedResponseDTO($"Bracket seeding failed: {ex.Message}", false);
            }
        }

        public async Task<SeedGroupsResponseDTO> SeedGroups(long tournamentId)
        {
            _logger.LogInformation($"=== Starting group seeding for tournament {tournamentId} ===");

            try
            {
                // 1. Validate tournament and check if standings are already seeded
                var tournament = await _tournamentRepository.Get(tournamentId);
                if (tournament == null)
                {
                    _logger.LogWarning($"Tournament {tournamentId} not found");
                    return new SeedGroupsResponseDTO(false, "Tournament not found");
                }

                var standings = await _standingService.GetStandingsAsync(tournamentId);
                if (standings.Any(standing => standing.IsSeeded))
                {
                    _logger.LogWarning("Standings are already seeded");
                    return new SeedGroupsResponseDTO(false, "Standings are already seeded!");
                }

                // 2. Split teams evenly into groups
                var groupStandings = standings.Where(s => s.StandingType == StandingType.Group).ToList();
                if (groupStandings.Count == 0)
                {
                    _logger.LogWarning("No group standings found");
                    return new SeedGroupsResponseDTO(false, "No group standings found for this tournament");
                }

                var teams = await _dbContext.TournamentTeams
                    .Where(tt => tt.TournamentId == tournamentId)
                    .Select(tt => tt.Team)
                    .ToListAsync();

                if (teams.Count < groupStandings.Count)
                {
                    _logger.LogWarning($"Not enough teams ({teams.Count}) for {groupStandings.Count} groups");
                    return new SeedGroupsResponseDTO(false, "Not enough teams to seed the groups!");
                }

                var groupAssignments = ShuffleTeamsIntoEqualGroups(teams, groupStandings);
                _logger.LogInformation($"Distributed {teams.Count} teams across {groupStandings.Count} groups");

                // 3. Create GroupEntry records for each team
                foreach (var (standing, teamsInGroup) in groupAssignments)
                {
                    foreach (var team in teamsInGroup)
                    {
                        try
                        {
                            var existingEntry = await _dbContext.GroupEntries
                                .FirstOrDefaultAsync(ge => ge.TeamId == team.Id && ge.TournamentId == tournamentId);

                            if (existingEntry != null)
                            {
                                existingEntry.StandingId = standing.Id;
                                existingEntry.Status = TeamStatus.Competing;
                                _logger.LogInformation($"Updated GroupEntry for team {team.Name} in {standing.Name}");
                            }
                            else
                            {
                                var groupEntry = new Group(tournamentId, standing.Id, team.Id, team.Name)
                                {
                                    Status = TeamStatus.Competing
                                };
                                await _dbContext.GroupEntries.AddAsync(groupEntry);
                                _logger.LogInformation($"Created GroupEntry for team {team.Name} in {standing.Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to create/update GroupEntry for team {team.Id}: {ex.Message}");
                            return new SeedGroupsResponseDTO(false, $"Failed to create group entry for team {team.Name}");
                        }
                    }
                }

                // 4. Generate matches and games for each group (round-robin)
                bool allMatchesGenerated = true;
                foreach (var (standing, teamsInGroup) in groupAssignments)
                {
                    _logger.LogInformation($"Generating round-robin matches for {standing.Name} with {teamsInGroup.Count} teams");

                    for (int i = 0; i < teamsInGroup.Count; i++)
                    {
                        for (int j = i + 1; j < teamsInGroup.Count; j++)
                        {
                            try
                            {
                                var result = await _matchService.GenerateMatch(
                                    teamsInGroup[i],
                                    teamsInGroup[j],
                                    round: i + 1,
                                    seed: j,
                                    standingId: standing.Id
                                );

                                if (!result.Success)
                                {
                                    _logger.LogError($"Failed to generate match: {result.Message}");
                                    allMatchesGenerated = false;
                                }
                                else
                                {
                                    _logger.LogInformation($"Generated match: {teamsInGroup[i].Name} vs {teamsInGroup[j].Name}");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error generating match between teams {teamsInGroup[i].Id} and {teamsInGroup[j].Id}: {ex.Message}");
                                allMatchesGenerated = false;
                            }
                        }
                    }

                    // 5. Mark standing as seeded if all matches were generated
                    if (allMatchesGenerated)
                    {
                        standing.IsSeeded = true;
                        _logger.LogInformation($"Marked {standing.Name} as seeded");
                    }
                }

                if (!allMatchesGenerated)
                {
                    _logger.LogWarning("Some matches failed to generate");
                    return new SeedGroupsResponseDTO(false, "Some matches failed to generate");
                }

                // 6. Save all changes
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("✓ Group seeding completed successfully");

                return new SeedGroupsResponseDTO(true, "Groups seeded successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error seeding groups for tournament {tournamentId}: {ex.Message}");
                return new SeedGroupsResponseDTO(false, $"Group seeding failed: {ex.Message}");
            }
        }

        private Dictionary<Standing, List<Team>> ShuffleTeamsIntoEqualGroups(List<Team> teams, List<Standing> groupStandings)
        {
            // Shuffle teams randomly
            var shuffledTeams = teams.OrderBy(t => Guid.NewGuid()).ToList();

            int teamsPerGroup = shuffledTeams.Count / groupStandings.Count;
            int remainingTeams = shuffledTeams.Count % groupStandings.Count;
            int teamIndex = 0;

            var assignments = new Dictionary<Standing, List<Team>>();

            for (int i = 0; i < groupStandings.Count; i++)
            {
                int groupSize = teamsPerGroup + (i < remainingTeams ? 1 : 0);
                var teamsForGroup = shuffledTeams.GetRange(teamIndex, groupSize);
                assignments[groupStandings[i]] = teamsForGroup;
                teamIndex += groupSize;

                _logger.LogInformation($"Assigned {groupSize} teams to {groupStandings[i].Name}");
            }

            return assignments;
        }
    }
}
