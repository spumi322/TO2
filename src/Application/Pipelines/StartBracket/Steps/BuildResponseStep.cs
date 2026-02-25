using Application.Pipelines.StartBracket.Contracts;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartBracket.Steps
{
    /// <summary>
    /// Step 9: Builds the final response message and broadcasts updates.
    /// This step always executes and ensures we have a message for the response.
    /// </summary>
    public class BuildResponseStep : IStartBracketPipelineStep
    {
        private readonly ILogger<BuildResponseStep> _logger;

        public BuildResponseStep(
            ILogger<BuildResponseStep> logger)
        {
            _logger = logger;
        }

        public async Task<bool> ExecuteAsync(StartBracketContext context)
        {
            _logger.LogInformation("Step 9: Building response for tournament {TournamentId}",
                context.TournamentId);

            // Ensure we have a message
            if (string.IsNullOrEmpty(context.Message))
            {
                context.Message = $"Bracket stage started successfully with {context.AdvancedTeams.Count} teams across {context.TotalRounds} rounds";
            }

            _logger.LogInformation("Response prepared: {Message}", context.Message);

            // Broadcasting moved to pipeline executor after transaction commit
            await Task.CompletedTask;

            // Always return true (this is the final step)
            return true;
        }
    }
}
