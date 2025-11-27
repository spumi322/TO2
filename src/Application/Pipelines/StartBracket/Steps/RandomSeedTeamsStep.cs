using Application.Pipelines.StartBracket.Contracts;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartBracket.Steps
{
    /// <summary>
    /// Step 3b: Randomly seeds teams for BracketOnly format.
    /// Inserted between GetAdvancedTeamsStep and ValidateTeamCountStep.
    /// Only executes for BracketOnly format.
    /// </summary>
    public class RandomSeedTeamsStep : IStartBracketPipelineStep
    {
        private readonly ILogger<RandomSeedTeamsStep> _logger;

        public RandomSeedTeamsStep(ILogger<RandomSeedTeamsStep> logger)
        {
            _logger = logger;
        }

        public Task<bool> ExecuteAsync(StartBracketContext context)
        {
            // Only execute for BracketOnly format
            if (context.Tournament.Format != Format.BracketOnly)
            {
                _logger.LogInformation("Skipping random seeding for {Format} format", context.Tournament.Format);
                return Task.FromResult(true); // Continue to next step
            }

            _logger.LogInformation("Step 3b: Randomly seeding {TeamCount} teams for BracketOnly tournament {TournamentId}",
                context.AdvancedTeams.Count, context.TournamentId);

            // Shuffle teams randomly
            var random = new Random();
            var shuffledTeams = context.AdvancedTeams.OrderBy(x => random.Next()).ToList();

            // Replace context teams with shuffled version
            context.AdvancedTeams = shuffledTeams;

            _logger.LogInformation("Teams randomly seeded for BracketOnly tournament {TournamentId}",
                context.TournamentId);

            return Task.FromResult(true); // Continue to next step
        }
    }
}
