using Application.Pipelines.StartBracket.Contracts;
using Application.Pipelines.StartBracket.Strategies;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartBracket.Steps
{
    /// <summary>
    /// Step 5: Calculates bracket structure and creates round 1 pairs.
    /// Pairing is delegated to the seeding strategy of the tournament format.
    /// </summary>
    public class CalculateBracketStructureStep : IStartBracketPipelineStep
    {
        private readonly ILogger<CalculateBracketStructureStep> _logger;
        private readonly IEnumerable<IBracketSeedingStrategy> _strategies;

        public CalculateBracketStructureStep(
            ILogger<CalculateBracketStructureStep> logger,
            IEnumerable<IBracketSeedingStrategy> strategies)
        {
            _logger = logger;
            _strategies = strategies;
        }

        public Task<bool> ExecuteAsync(StartBracketContext context)
        {
            _logger.LogInformation("Step 5: Calculating bracket structure for tournament {TournamentId}",
                context.TournamentId);

            var format = context.Tournament.Format;
            var strategy = _strategies.FirstOrDefault(s => s.Format == format)
                ?? throw new InvalidOperationException($"No bracket seeding strategy found for format: {format}");

            var seededPairs = strategy.CreateFirstRoundPairs(context.AdvancedTeams);

            int bracketSize = seededPairs.Count * 2;
            int totalRounds = (int)Math.Log2(bracketSize);
            int byeCount = bracketSize - context.AdvancedTeams.Count;

            context.TotalRounds = totalRounds;
            context.SeededPairs = seededPairs;

            _logger.LogInformation("Bracket: {TeamCount} teams → size {BracketSize} ({ByeCount} BYEs), {TotalRounds} rounds, {PairCount} R1 pairings for tournament {TournamentId}",
                context.AdvancedTeams.Count, bracketSize, byeCount, totalRounds, seededPairs.Count, context.TournamentId);

            return Task.FromResult(true); // Continue to next step
        }
    }
}
