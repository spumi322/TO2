using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record StartBracketResponseDTO(string Message, TournamentStatus TournamentStatus, long Version);
}
