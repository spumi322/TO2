using Application.DTOs.Standing;
using Application.DTOs.Team;
using Application.DTOs.Tournament;
using Domain.AggregateRoots;
using Domain.Entities;

namespace Application.Contracts
{
    public interface IStandingService
    {
        // Setup
        Task InitializeStandingsForTournamentAsync(Tournament tournament, int? numberOfGroups);
        Task<List<Standing>> GetStandingsAsync(long tournamentId);

        // Standing progress
        /// <summary>
        /// Checks if any standings finished and marks them. Returns true if a standing was just marked finished.
        /// </summary>
        Task<bool> IsGroupFinished(long standingId);
        Task MarkStandingAsFinished(long standingId);
        Task FinalizeGroupTeams(long standingId);
        /// <summary>
        /// Checks if all groups are finished. Returns true if all groups finished.
        /// </summary>
        Task<bool> CheckAllGroupsAreFinished(long tournamentId);

        // Bracket entry
        /// <summary>
        /// Gets the teams entering the bracket, unordered (seeding is done by the bracket seeding strategy).
        /// BracketOnly: all registered teams.
        /// GroupsAndBracket: teams advancing from groups, with group id and placement; marks group entries Advanced/Eliminated.
        /// </summary>
        Task<List<BracketEntrant>> GetTeamsForBracketByFormat(long tournamentId);

        // Read models (UI)
        /// <summary>
        /// Gets all groups with teams and matches in optimized response.
        /// Teams pre-sorted by points descending.
        /// </summary>
        Task<List<GetGroupsWithDetailsResponseDTO>> GetGroupsWithDetailsAsync(long tournamentId);
        Task<GetGroupsWithDetailsResponseDTO?> GetGroupWithDetailsByStandingIdAsync(long standingId);
        Task<GetBracketWithDetailsResponseDTO?> GetBracketWithDetailsAsync(long tournamentId);
    }
}
