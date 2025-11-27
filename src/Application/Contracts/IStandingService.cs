using Application.DTOs.Team;
using Application.DTOs.Tournament;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;

namespace Application.Contracts
{
    public interface IStandingService
    {
        Task GenerateStanding(long tournamentId, string name, StandingType type, int? teamsPerStanding);
        Task<List<Standing>> GetStandingsAsync(long tournamentId);
        /// <summary>
        /// Checks if any standings finished and marks them. Returns true if a standing was just marked finished.
        /// </summary>
        Task<bool> IsGroupFinished(long standingId);
        Task MarkGroupAsFinished(long standingId);
        /// <summary>
        /// Checks if all groups are finished. Returns true if all groups finished.
        /// </summary>
        Task<bool> CheckAllGroupsAreFinished(long tournamentId);
        Task<List<Team>> GetTeamsForBracket(long tournamentId);
        /// <summary>
        /// Gets teams for bracket based on tournament format.
        /// BracketOnly: Returns all registered teams.
        /// BracketAndGroup: Returns teams advancing from groups.
        /// </summary>
        Task<List<Team>> GetTeamsForBracketByFormat(long tournamentId);
        /// <summary>
        /// Advances the match winner to the next round by populating the appropriate team slot.
        /// </summary>
        Task<List<TeamPlacementDTO>> GetFinalResultsAsync(long tournamentId);
        Task<List<(long TeamId, int Placement, int? EliminatedInRound)>> CalculateFinalPlacements(long standingId);
        Task SetFinalResults(long tournamentId, List<(long TeamId, int Placement, int? EliminatedInRound)> placements);
        Task<List<GetTeamWithStatsResponseDTO>> GetTeamsWithStatsAsync(long standingId);

    }
}
