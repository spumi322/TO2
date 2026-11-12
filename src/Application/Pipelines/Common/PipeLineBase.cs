using Application.Pipelines.MatchResult.Contracts;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.Common
{
    /// <summary>
    /// Base class for all pipeline steps.
    /// Provides step logging. Exceptions propagate to the pipeline, which rolls back the transaction
    /// and lets GlobalExceptionHandler map them to an HTTP status.
    /// </summary>
    public abstract class PipeLineBase<TLogger> : IMatchResultPipelineStep
    {
        protected readonly ILogger<TLogger> Logger;

        protected PipeLineBase(ILogger<TLogger> logger)
        {
            Logger = logger;
        }

        public async Task<bool> ExecuteAsync(MatchResultContext context)
        {
            Logger.LogInformation("Executing step: {StepName}", GetType().Name);
            var result = await ExecuteStepAsync(context);
            Logger.LogInformation("Completed step: {StepName}. Continue: {Continue}", GetType().Name, result);
            return result;
        }

        /// <summary>
        /// Implement the actual step logic in derived classes.
        /// </summary>
        /// <param name="context">The pipeline context</param>
        /// <returns>True to continue, False to stop pipeline</returns>
        protected abstract Task<bool> ExecuteStepAsync(MatchResultContext context);
    }
}
