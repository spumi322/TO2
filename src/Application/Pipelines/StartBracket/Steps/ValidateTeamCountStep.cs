using Application.Pipelines.StartBracket.Contracts;
using Application.Pipelines.StartBracket.Utilities;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartBracket.Steps
{
    /// <summary>
    /// Step 4: Validates that team count is a power of 2.
    /// Required for single elimination bracket structure.
    /// </summary>
    public class ValidateTeamCountStep : IStartBracketPipelineStep
    {
        private readonly ILogger<ValidateTeamCountStep> _logger;

        public ValidateTeamCountStep(ILogger<ValidateTeamCountStep> logger)
        {
            _logger = logger;
        }

        public Task<bool> ExecuteAsync(StartBracketContext context)
        {
            _logger.LogInformation("Step 4: Validating team count for tournament {TournamentId}",
                context.TournamentId);

            int teamCount = context.AdvancedTeams.Count;

            if (!BracketSeedingUtility.IsPowerOfTwo(teamCount))
            {
                context.Success = false;
                context.Message = $"Team count must be power of 2 for single elimination. Got {teamCount} teams";
                _logger.LogWarning("Invalid team count {TeamCount} for tournament {TournamentId}. Must be power of 2.",
                    teamCount, context.TournamentId);
                return Task.FromResult(false);
            }

            _logger.LogInformation("Team count {TeamCount} validated for tournament {TournamentId}",
                teamCount, context.TournamentId);

            return Task.FromResult(true); // Continue to next step
        }
    }
}
