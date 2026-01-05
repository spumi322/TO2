using Application.Contracts;
using Application.Pipelines.StartGroups.Contracts;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartGroups.Steps
{
    /// <summary>
    /// Step 8: Builds the final response message and broadcasts updates.
    /// This step always executes and ensures we have a message for the response.
    /// </summary>
    public class BuildResponseStep : IStartGroupsPipelineStep
    {
        private readonly ILogger<BuildResponseStep> _logger;
        private readonly ISignalRService _signalRService;
        private readonly ITenantService _tenantService;

        public BuildResponseStep(
            ILogger<BuildResponseStep> logger,
            ISignalRService signalRService,
            ITenantService tenantService)
        {
            _logger = logger;
            _signalRService = signalRService;
            _tenantService = tenantService;
        }

        public async Task<bool> ExecuteAsync(StartGroupsContext context)
        {
            _logger.LogInformation("Step 8: Building response for tournament {TournamentId}",
                context.TournamentId);

            // Ensure we have a message
            if (string.IsNullOrEmpty(context.Message))
            {
                context.Message = "Group stage started successfully";
            }

            _logger.LogInformation("Response prepared: {Message}", context.Message);

            // Broadcasting moved to pipeline executor after transaction commit
            await Task.CompletedTask;

            // Always return true (this is the final step)
            return true;
        }
    }
}
