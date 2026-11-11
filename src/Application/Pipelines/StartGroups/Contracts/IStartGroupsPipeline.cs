namespace Application.Pipelines.StartGroups.Contracts
{
    /// <summary>
    /// Pipeline for starting tournament group stage.
    /// Orchestrates team distribution, group seeding, match generation, and state transitions.
    /// All operations execute within a single atomic transaction.
    /// </summary>
    public interface IStartGroupsPipeline
    {
        /// <summary>
        /// Executes the complete group starting workflow.
        /// </summary>
        /// <param name="tournamentId">The tournament to start groups for</param>
        Task ExecuteAsync(long tournamentId);
    }
}
