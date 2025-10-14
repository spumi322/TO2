using Application.Contracts;
using Application.DTOs.Match;
using Application.DTOs.Standing;
using Domain.Entities;
using Domain.Enums;
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
        private readonly IGenericRepository<Standing> _standingRepository;

        public TournamentLifecycleService(
            ILogger<TournamentLifecycleService> logger,
            IStandingService standingService,
            IMatchService matchService,
            IGenericRepository<Standing> standingRepository)
        {
            _logger = logger;
            _standingService = standingService;
            _matchService = matchService;
            _standingRepository = standingRepository;
        }

        public async Task<MatchResultDTO> OnMatchCompleted(long matchId, long winnerId, long loserId, long tournamentId)
        {
            _logger.LogInformation($"=== Tournament Lifecycle: Match {matchId} completed ===");
            _logger.LogInformation($"Winner: {winnerId}, Loser: {loserId}, Tournament: {tournamentId}");

            // 1. Check if this match completion causes any standing to finish
            bool standingJustFinished = await _standingService.CheckAndMarkStandingAsFinishedAsync(tournamentId);

            // 2. ONLY check if all groups finished when a standing just finished
            if (!standingJustFinished)
            {
                _logger.LogInformation("No standing finished with this match. Normal match completion.");
                return new MatchResultDTO(winnerId, loserId);
            }

            _logger.LogInformation("✓ A standing just finished! Checking if all groups are now complete...");

            // 3. Check if ALL groups are finished
            bool allGroupsFinished = await _standingService.CheckAndMarkAllGroupsAreFinishedAsync(tournamentId);

            if (!allGroupsFinished)
            {
                _logger.LogInformation("Not all groups finished yet. No bracket seeding.");
                return new MatchResultDTO(winnerId, loserId);
            }

            _logger.LogInformation("✓✓✓ ALL GROUPS FINISHED! Seeding bracket now...");

            // 4. If all groups finished, seed bracket
            var seedingResult = await SeedBracketIfReady(tournamentId);

            // 5. Return enriched DTO with lifecycle state - ONLY when bracket seeded!
            return new MatchResultDTO(
                WinnerId: winnerId,
                LoserId: loserId,
                AllGroupsFinished: true,
                BracketSeeded: seedingResult.Success,
                BracketSeedMessage: seedingResult.Message
            );
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
