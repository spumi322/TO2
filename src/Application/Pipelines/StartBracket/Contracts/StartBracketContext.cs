using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;

namespace Application.Pipelines.StartBracket.Contracts
{
    /// <summary>
    /// Context object that holds all data passed between pipeline steps.
    /// Acts as a shared state for the entire bracket seeding and starting pipeline.
    /// </summary>
    public class StartBracketContext
    {
        // Input data
        public long TournamentId { get; set; }

        // Loaded entities
        public Tournament Tournament { get; set; } = null!;
        public List<Standing> Standings { get; set; } = new();
        public Standing BracketStanding { get; set; } = null!;
        public List<Team> AdvancedTeams { get; set; } = new();

        // Computed data
        public List<(Team teamA, Team teamB)> SeededPairs { get; set; } = new();
        public int TotalRounds { get; set; }

        // Output
        public TournamentStatus NewStatus { get; set; }
        public string Message { get; set; } = string.Empty;

        // Control flags
        public bool Success { get; set; } = true;
        public bool ShouldContinue { get; set; } = true;
    }
}
