namespace Application.DTOs.Standing
{
    // Shared game DTO (used by both groups and brackets)
    public record StandingGameDTO
    {
        public long Id { get; init; }
        public long MatchId { get; init; }
        public long? TeamAId { get; init; }
        public long? TeamBId { get; init; }
        public long? WinnerId { get; init; }
        public long Version { get; init; }
    }
}
