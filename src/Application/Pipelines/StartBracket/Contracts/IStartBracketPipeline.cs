using Application.DTOs.Tournament;

namespace Application.Pipelines.StartBracket.Contracts
{
    /// <summary>
    /// Pipeline for starting tournament bracket stage.
    /// Orchestrates team advancement from groups, bracket seeding, match generation, and state transitions.
    /// All operations execute within a single atomic transaction.
    /// </summary>
    public interface IStartBracketPipeline
    {
        /// <summary>
        /// Executes the complete bracket starting workflow.
        /// </summary>
        /// <param name="tournamentId">The tournament to start bracket for</param>
        /// <returns>Result indicating success/failure and new tournament status</returns>
        Task<StartBracketResponseDTO> ExecuteAsync(long tournamentId);
    }
}
