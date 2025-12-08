using Domain.Enums;

namespace Application.DTOs.Tournament
{
    public record GetTournamentListResponseDTO
    {
        public long Id { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
        public int CurrentTeams { get; init; }
        public int MaxTeams { get; init; }
        public Format Format { get; init; }
        public TournamentStatus Status { get; init; }
    }
}
