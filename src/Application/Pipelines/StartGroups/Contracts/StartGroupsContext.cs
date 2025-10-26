using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;

namespace Application.Pipelines.StartGroups.Contracts
{
    /// <summary>
    /// Context object that holds all data passed between pipeline steps.
    /// Acts as a shared state for the entire group seeding and starting pipeline.
    /// </summary>
    public class StartGroupsContext
    {
        // Input data
        public long TournamentId { get; set; }

        // Loaded entities
        public Tournament Tournament { get; set; } = null!;
        public List<Standing> Standings { get; set; } = new();
        public List<Standing> GroupStandings { get; set; } = new();
        public List<Team> Teams { get; set; } = new();

        // Computed data
        public Dictionary<Standing, List<Team>> GroupAssignments { get; set; } = new();

        // Output
        public TournamentStatus NewStatus { get; set; }
        public string Message { get; set; } = string.Empty;

        // Control flags
        public bool Success { get; set; } = true;
        public bool ShouldContinue { get; set; } = true;
    }
}
