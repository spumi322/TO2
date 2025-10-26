using Application.Pipelines.StartGroups.Contracts;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartGroups.Steps
{
    /// <summary>
    /// Step 8: Builds the final response message.
    /// This step always executes and ensures we have a message for the response.
    /// </summary>
    public class BuildResponseStep : IStartGroupsPipelineStep
    {
        private readonly ILogger<BuildResponseStep> _logger;

        public BuildResponseStep(ILogger<BuildResponseStep> logger)
        {
            _logger = logger;
        }

        public Task<bool> ExecuteAsync(StartGroupsContext context)
        {
            _logger.LogInformation("Step 8: Building response for tournament {TournamentId}",
                context.TournamentId);

            // Ensure we have a message
            if (string.IsNullOrEmpty(context.Message))
            {
                context.Message = "Group stage started successfully";
            }

            _logger.LogInformation("Response prepared: {Message}", context.Message);

            // Always return true (this is the final step)
            return Task.FromResult(true);
        }
    }
}
