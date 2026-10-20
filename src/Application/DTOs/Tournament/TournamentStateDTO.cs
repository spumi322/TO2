using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record TournamentStateDTO(
        TournamentStatus CurrentStatus,
        bool IsTransitionState,
        bool IsActiveState,
        bool CanScoreMatches,
        bool CanModifyTeams,
        bool CanCancel,
        bool CanDelete,
        bool CanStartGroups,
        bool CanStartBracket,
        long Version
    );
}
