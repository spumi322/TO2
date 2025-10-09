using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record FinalStandingDTO(
        long TeamId,
        string TeamName,
        int Placement,
        TeamStatus Status,
        int EliminatedInRound
    );
}
