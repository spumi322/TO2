using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record GetAllTournamentsResponseDTO(long Id, string Name, string Description, int MaxTeams, TournamentStatus Status);
}
