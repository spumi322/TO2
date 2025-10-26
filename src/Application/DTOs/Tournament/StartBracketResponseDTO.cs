using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record StartBracketResponseDTO(bool Success, string Message, TournamentStatus TournamentStatus);
}
