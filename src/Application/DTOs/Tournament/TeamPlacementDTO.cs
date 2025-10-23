using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record TeamPlacementDTO(
        long TeamId,
        string TeamName,
        int Placement,
        TeamStatus Status,
        int EliminatedInRound
    );
}
