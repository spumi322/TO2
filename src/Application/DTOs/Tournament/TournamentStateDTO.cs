using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record TournamentStateDTO(
        TournamentStatus CurrentStatus,
        bool IsTransitionState,
        bool IsActiveState,
        bool CanScoreMatches,
        bool CanModifyTeams,
        string StatusDisplayName,
        string StatusDescription
    );
}
