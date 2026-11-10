using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record StartGroupsResponseDTO(string Message, TournamentStatus TournamentStatus, long Version);
}
