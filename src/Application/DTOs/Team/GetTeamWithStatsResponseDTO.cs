namespace Application.DTOs.Team
{
    public record GetTeamWithStatsResponseDTO
    {
        public long Id { get; init; }
        public string Name { get; init; }
        public int Wins { get; init; }
        public int Losses { get; init; }
        public int Points { get; init; }
    }
}
