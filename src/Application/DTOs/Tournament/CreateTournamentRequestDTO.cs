using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record CreateTournamentRequestDTO(string Name, string Description, int MaxTeams, Format Format, int? TeamsPerGroup, int? TeamsPerBracket);
}
