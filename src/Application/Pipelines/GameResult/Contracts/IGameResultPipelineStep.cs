using System.Threading.Tasks;

namespace Application.Pipelines.GameResult.Contracts
{
    /// <summary>
    /// Interface for a single step in the game result processing pipeline.
    /// Each step performs one specific responsibility and can signal to continue or stop the pipeline.
    /// </summary>
    public interface IGameResultPipelineStep
    {
        /// <summary>
        /// Executes the step logic.
        /// </summary>
        /// <param name="context">Shared context containing all data for the pipeline</param>
        /// <returns>True to continue to next step, False to stop pipeline execution</returns>
        Task<bool> ExecuteAsync(GameResultContext context);
    }
}
