using Application.Contracts;
using Application.Pipelines.StartGroups.Contracts;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Domain.Exceptions;

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
                throw new ConflictException("The groups are already drawn.");

            // Filter to group standings
            var groupStandings = standings.Where(s => s.StandingType == StandingType.Group).ToList();

            // Group standings are created with the tournament, so none means broken data (500)
            if (groupStandings.Count == 0)
                throw new InvalidOperationException($"No group standings found for tournament {context.TournamentId}");

            // Store in context
            context.Standings = standings.ToList();
            context.GroupStandings = groupStandings;

            _logger.LogInformation("Found {Count} group standings for tournament {TournamentId}",
                groupStandings.Count, context.TournamentId);

            return true; // Continue to next step
        }
    }
}
