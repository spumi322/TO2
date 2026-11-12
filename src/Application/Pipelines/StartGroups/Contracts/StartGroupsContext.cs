using Domain.AggregateRoots;
using Domain.Entities;

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
        public List<Standing> GroupStandings { get; set; } = new();
        public List<Team> Teams { get; set; } = new();

        // Computed data
        public Dictionary<Standing, List<Team>> GroupAssignments { get; set; } = new();
    }
}
