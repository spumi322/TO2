namespace Application.DTOs.Standing
{
    public record GroupTeamDTO
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Wins { get; init; }
        public int Losses { get; init; }
        public int Points { get; init; }
        public int Status { get; init; }
    }
}
