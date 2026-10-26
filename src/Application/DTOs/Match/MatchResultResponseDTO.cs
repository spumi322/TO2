using Application.DTOs.Tournament;
using Domain.Enums;

namespace Application.DTOs.Match
{
    // Response of a confirmed match result: what the result changed (match, standing, tournament)
    public record MatchResultResponseDTO(
        long? MatchWinnerId = null,
        long? MatchLoserId = null,
        bool StandingFinished = false,
        bool AllGroupsFinished = false,
        bool TournamentFinished = false,
        TournamentStatus? NewTournamentStatus = null,
        List<TeamPlacementDTO>? FinalStandings = null,
        string? Message = null
    );
}
