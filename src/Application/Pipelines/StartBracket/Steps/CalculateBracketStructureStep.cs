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

            var paddedTeams = BracketSeedingUtility.PadToPowerOfTwo(context.AdvancedTeams);
            int totalRounds = (int)Math.Log2(paddedTeams.Count);
            int byeCount = paddedTeams.Count - context.AdvancedTeams.Count;

            var seededPairs = BracketSeedingUtility.CreateSingleEliminationPairs(paddedTeams, _logger);

            context.TotalRounds = totalRounds;
            context.SeededPairs = seededPairs;

            _logger.LogInformation("Bracket: {TeamCount} teams → size {BracketSize} ({ByeCount} BYEs), {TotalRounds} rounds, {PairCount} R1 pairings for tournament {TournamentId}",
                context.AdvancedTeams.Count, paddedTeams.Count, byeCount, totalRounds, seededPairs.Count, context.TournamentId);

            return Task.FromResult(true); // Continue to next step
        }
    }
}
