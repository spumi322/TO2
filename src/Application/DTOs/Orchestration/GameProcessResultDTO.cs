using Application.DTOs.Tournament;
using Domain.Enums;

namespace Application.DTOs.Orchestration
{
    public record GameProcessResultDTO(
        bool Success,
        bool MatchFinished,
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
