using Application.DTOs.Game;
using Application.DTOs.Match;
using Application.DTOs.Orchestration;
using Application.DTOs.Standing;
using System.Threading.Tasks;

namespace Application.Contracts
{
    /// <summary>
    /// Manages explicit tournament lifecycle state transitions.
    /// Replaces domain events with synchronous, testable state machine pattern.
    /// </summary>
    public interface IOrchestrationService
    {
        /// <summary>
        /// Called when a match is completed. Checks if this triggers any lifecycle transitions
        /// (e.g., all groups finished -> seed bracket).
        /// </summary>
        /// <param name="matchId">The completed match ID</param>
        /// <param name="winnerId">Winner team ID</param>
        /// <param name="loserId">Loser team ID</param>
        /// <param name="tournamentId">Tournament ID</param>
        /// <returns>Enhanced MatchResultDTO with lifecycle transition information</returns>
        //Task<MatchResultDTO> OnMatchCompleted(long matchId, long winnerId, long loserId, long tournamentId);

        /// <summary>
        /// Explicitly seeds the bracket if all groups are finished and bracket hasn't been seeded yet.
        /// </summary>
        /// <param name="tournamentId">Tournament ID</param>
        /// <returns>Seeding result</returns>
        //Task<BracketSeedResponseDTO> SeedBracketIfReady(long tournamentId);

        Task<SeedGroupsResponseDTO> SeedGroups(long tournamentId);

        Task<GameProcessResultDTO> ProcessGameResult(SetGameResultDTO gameResult);
    }
}
