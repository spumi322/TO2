using Application.Pipelines.Common;
using Application.Pipelines.GameResult.Contracts;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.GameResult.Steps
{
    /// <summary>
    /// Step 8: Final step that ensures response data is properly set.
    /// This step always executes and ensures we have a message for the response.
    /// The actual DTO is built by the pipeline executor from the context.
    /// </summary>
    public class BuildResponseStep : PipeLineBase<BuildResponseStep>
    {
        public BuildResponseStep(ILogger<BuildResponseStep> logger) : base(logger)
        {
        }

        protected override Task<bool> ExecuteStepAsync(GameResultContext context)
        {
            // Ensure we have a message
            if (string.IsNullOrEmpty(context.Message))
            {
                context.Message = "Game result processed successfully.";
            }

            Logger.LogInformation("Response prepared: {Message}", context.Message);

            // Always return true (this is the final step)
            return Task.FromResult(true);
        }
    }
}
