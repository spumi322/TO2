using Application.Contracts;
using Application.Pipelines.StartGroups.Contracts;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartGroups.Steps
{
    /// <summary>
    /// Step 2: Validates that group standings exist and are not already seeded.
    /// Loads standings into context.
    /// </summary>
    public class ValidateStandingsNotSeededStep : IStartGroupsPipelineStep
    {
        private readonly ILogger<ValidateStandingsNotSeededStep> _logger;
        private readonly IStandingService _standingService;

        public ValidateStandingsNotSeededStep(
            ILogger<ValidateStandingsNotSeededStep> logger,
            IStandingService standingService)
        {
            _logger = logger;
            _standingService = standingService;
        }

        public async Task<bool> ExecuteAsync(StartGroupsContext context)
        {
            _logger.LogInformation("Step 2: Validating standings for tournament {TournamentId}",
                context.TournamentId);

            // Get all standings
            var standings = await _standingService.GetStandingsAsync(context.TournamentId);

            // Check if any standings are already seeded
            if (standings.Any(standing => standing.IsSeeded))
            {
                context.Success = false;
                context.Message = "Standings are already seeded!";
                _logger.LogWarning("Standings for tournament {TournamentId} are already seeded",
                    context.TournamentId);
                return false;
            }

            // Filter to group standings
            var groupStandings = standings.Where(s => s.StandingType == StandingType.Group).ToList();

            if (groupStandings.Count == 0)
            {
                context.Success = false;
                context.Message = "No group standings found for this tournament";
                _logger.LogWarning("No group standings found for tournament {TournamentId}",
                    context.TournamentId);
                return false;
            }

            // Store in context
            context.Standings = standings.ToList();
            context.GroupStandings = groupStandings;

            _logger.LogInformation("Found {Count} group standings for tournament {TournamentId}",
                groupStandings.Count, context.TournamentId);

            return true; // Continue to next step
        }
    }
}
