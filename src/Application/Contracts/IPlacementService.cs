using Application.DTOs.Tournament;

namespace Application.Contracts
{
    /// <summary>
    /// Final placements of a finished tournament: calculates them, stores them on TournamentTeam, reads them back.
    /// </summary>
    public interface IPlacementService
    {
        Task<List<(long TeamId, int Placement, int? EliminatedInRound)>> CalculateBracketPlacements(long standingId);
        Task<List<(long TeamId, int Placement, int? EliminatedInRound)>> CalculateGroupOnlyPlacements(long tournamentId);
        /// <summary>
        /// GroupsAndBracket: placements of the teams knocked out in the group stage, ranked below all bracket teams.
        /// </summary>
        Task<List<(long TeamId, int Placement, int? EliminatedInRound)>> CalculateGroupStageKnockoutPlacements(long tournamentId);
        Task SetFinalResults(long tournamentId, List<(long TeamId, int Placement, int? EliminatedInRound)> placements);
        /// <summary>
        /// Gets the final placements of a finished tournament (empty list if not finished).
        /// </summary>
        Task<List<TeamPlacementDTO>> GetFinalResultsAsync(long tournamentId);
    }
}
