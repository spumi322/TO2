using Application.DTOs.Standing;
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
        Task InitializeStandingsForTournamentAsync(long tournamentId, Format format, int maxTeams, int? teamsPerGroup, int? teamsPerBracket);
        Task<List<Standing>> GetStandingsAsync(long tournamentId);
        /// <summary>
        /// Checks if any standings finished and marks them. Returns true if a standing was just marked finished.
        /// </summary>
        Task<bool> IsGroupFinished(long standingId);
        Task MarkGroupAsFinished(long standingId);
        Task FinalizeGroupTeams(long standingId);
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
        /// <summary>
        /// Gets all groups with teams and matches in optimized response.
        /// Teams pre-sorted by points descending.
        /// </summary>
        Task<List<GetGroupsWithDetailsResponseDTO>> GetGroupsWithDetailsAsync(long tournamentId);
        Task<List<(long TeamId, int Placement, int? EliminatedInRound)>> CalculateBracketPlacements(long standingId);
        Task<List<(long TeamId, int Placement, int? EliminatedInRound)>> CalculateGroupOnlyPlacements(long tournamentId);
        Task SetFinalResults(long tournamentId, List<(long TeamId, int Placement, int? EliminatedInRound)> placements);
        Task<GetBracketWithDetailsResponseDTO?> GetBracketWithDetailsAsync(long tournamentId);

    }
}
