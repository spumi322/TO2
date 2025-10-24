using Application.Pipelines.GameResult.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Application.Pipelines.Common
{
    /// <summary>
    /// Base class for all pipeline steps.
    /// Provides common functionality like logging and error handling.
    /// </summary>
    public abstract class PipeLineBase<TLogger> : IGameResultPipelineStep
    {
        protected readonly ILogger<TLogger> Logger;

        protected PipeLineBase(ILogger<TLogger> logger)
        {
            Logger = logger;
        }

        public async Task<bool> ExecuteAsync(GameResultContext context)
        {
            try
            {
                Logger.LogInformation("Executing step: {StepName}", GetType().Name);
                var result = await ExecuteStepAsync(context);
                Logger.LogInformation("Completed step: {StepName}. Continue: {Continue}", GetType().Name, result);
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in step: {StepName}. Error: {Message}", GetType().Name, ex.Message);
                context.Success = false;
                context.Message = $"Error in {GetType().Name}: {ex.Message}";
                return false; // Stop pipeline on error
            }
        }

        /// <summary>
        /// Implement the actual step logic in derived classes.
        /// </summary>
        /// <param name="context">The pipeline context</param>
        /// <returns>True to continue, False to stop pipeline</returns>
        protected abstract Task<bool> ExecuteStepAsync(GameResultContext context);
    }
}
