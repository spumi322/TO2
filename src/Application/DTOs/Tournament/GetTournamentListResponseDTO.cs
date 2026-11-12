using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record GetTournamentListResponseDTO
    {
        public long Id { get; init; }
        public string Name { get; init; }
        public string? Description { get; init; }
        public int CurrentTeams { get; init; }
        public int MaxTeams { get; init; }
        public Format Format { get; init; }
        public TournamentStatus Status { get; init; }
        public long Version { get; init; }
        public string? WinnerName { get; init; }
        public int GroupsFinished { get; init; }
        public int GroupsTotal { get; init; }
        public int RoundsFinished { get; init; }
        public int RoundsTotal { get; init; }
        public DateTime? FinishedAt { get; init; }
        public DateTime? CancelledAt { get; init; }
        public string? CancellationReason { get; init; }
    }
}
