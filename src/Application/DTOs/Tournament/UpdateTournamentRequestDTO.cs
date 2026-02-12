using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record UpdateTournamentRequestDTO(string Name, string Description, TournamentStatus status, byte[]? RowVersion);
}
