using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record StartGroupsResponseDTO(bool Success, string Message, TournamentStatus TournamentStatus);
}
