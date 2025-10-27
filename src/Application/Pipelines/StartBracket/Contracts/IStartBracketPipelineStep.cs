namespace Application.Pipelines.StartBracket.Contracts
{
    /// <summary>
    /// Interface for a single step in the StartBracket pipeline.
    /// Each step performs one specific responsibility and can signal to continue or stop the pipeline.
    /// </summary>
    public interface IStartBracketPipelineStep
    {
        /// <summary>
        /// Executes the step logic.
        /// </summary>
        /// <param name="context">Shared context containing all data for the pipeline</param>
        /// <returns>True to continue to next step, False to stop pipeline execution</returns>
        Task<bool> ExecuteAsync(StartBracketContext context);
    }
}
