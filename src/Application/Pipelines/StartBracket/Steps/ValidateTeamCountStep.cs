using Application.Pipelines.StartBracket.Contracts;
using Application.Pipelines.StartBracket.Utilities;
using Microsoft.Extensions.Logging;
using Domain.Exceptions;

namespace Application.Pipelines.StartBracket.Steps
{
    /// <summary>
    /// Step 4: Validates that at least 2 teams advance.
    /// Non-power-of-2 counts are fine: the bracket is padded with BYEs.
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

            if (teamCount < 2)
                throw new ConflictException($"At least 2 teams are required to start a bracket. Got {teamCount}");

            _logger.LogInformation("Team count {TeamCount} validated for tournament {TournamentId} (bracket size will be {BracketSize})",
                teamCount, context.TournamentId, BracketSeedingUtility.NextPowerOfTwo(teamCount));

            return Task.FromResult(true); // Continue to next step
        }
    }
}
