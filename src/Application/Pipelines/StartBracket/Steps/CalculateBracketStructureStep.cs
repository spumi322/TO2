using Application.Pipelines.StartBracket.Contracts;
using Application.Pipelines.StartBracket.Utilities;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartBracket.Steps
{
    /// <summary>
    /// Step 5: Calculates bracket structure and creates seeding pairs.
    /// Determines rounds and applies single elimination seeding algorithm.
    /// </summary>
    public class CalculateBracketStructureStep : IStartBracketPipelineStep
    {
        private readonly ILogger<CalculateBracketStructureStep> _logger;

        public CalculateBracketStructureStep(ILogger<CalculateBracketStructureStep> logger)
        {
            _logger = logger;
        }

        public Task<bool> ExecuteAsync(StartBracketContext context)
        {
            _logger.LogInformation("Step 5: Calculating bracket structure for tournament {TournamentId}",
                context.TournamentId);

            int teamCount = context.AdvancedTeams.Count;
            int totalRounds = (int)Math.Log2(teamCount);

            // Create seeding pairs using bracket seeding utility
            var seededPairs = BracketSeedingUtility.CreateSingleEliminationPairs(
                context.AdvancedTeams,
                _logger);

            // Store in context
            context.TotalRounds = totalRounds;
            context.SeededPairs = seededPairs;

            _logger.LogInformation("Created bracket with {TotalRounds} rounds and {PairCount} first-round pairings for tournament {TournamentId}",
                totalRounds, seededPairs.Count, context.TournamentId);

            return Task.FromResult(true); // Continue to next step
        }
    }
}
