using Application.DTOs.Standing;
using Domain.AggregateRoots;
using Domain.Entities;

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
        public Standing BracketStanding { get; set; } = null!;
        public List<BracketEntrant> AdvancedTeams { get; set; } = new();

        // Computed data
        public List<(Team? teamA, Team? teamB)> SeededPairs { get; set; } = new();
        public int TotalRounds { get; set; }
    }
}
