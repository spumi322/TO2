using Application.Contracts;
using Application.DTOs.Match;
using Application.DTOs.Standing;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using Domain.StateMachine;
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
    public class TournamentLifecycleService : ITournamentLifecycleService
    {
        private readonly ILogger<TournamentLifecycleService> _logger;
        private readonly IStandingService _standingService;
        private readonly IMatchService _matchService;
        private readonly IGenericRepository<Tournament> _tournamentRepository;
        private readonly ITournamentStateMachine _stateMachine;

        public TournamentLifecycleService(
            ILogger<TournamentLifecycleService> logger,
            IStandingService standingService,
            IMatchService matchService,
            IGenericRepository<Tournament> tournamentRepository,
            ITournamentStateMachine stateMachine
            )
        {
            _logger = logger;
            _standingService = standingService;
            _matchService = matchService;
            _tournamentRepository = tournamentRepository;
            _stateMachine = stateMachine;
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
    }
}
