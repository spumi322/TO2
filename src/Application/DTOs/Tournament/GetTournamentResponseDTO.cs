using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record GetTournamentResponseDTO(long Id, string Name, string Description, int MaxTeams, Format Format, bool IsRegistrationOpen, byte[]? RowVersion);
}
