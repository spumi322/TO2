using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record GetTournamentResponseDTO(long Id, string Name, string? Description, int MaxTeams, Format Format, TournamentStatus Status, bool IsRegistrationOpen, long Version,
        DateTime? CancelledAt, string? CancelledBy, string? CancellationReason,
        int CurrentTeams, int NumberOfGroups, int? AdvancingPerGroup, string CreatedBy, DateTime CreatedDate);
}
