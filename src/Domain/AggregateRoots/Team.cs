using Domain.Common;
using Domain.Entities;

namespace Domain.AggregateRoots
{
    // A name registered for one tournament: created with it, deleted with it, never reused
    public class Team : AggregateRootBase
    {
        private Team() { }

        public Team(string name, long tournamentId)
        {
            Name = name.Trim();
            TournamentId = tournamentId;
        }

        public string Name { get; private set; }

        public long TournamentId { get; private set; }
        public Tournament Tournament { get; private set; } = null!;

        public long TenantId { get; private set; }
        public Tenant Tenant { get; set; } = null!;

        // Final result, set when the tournament finishes
        public int? FinalPlacement { get; private set; }
        public int? EliminatedInRound { get; private set; }

        public void SetFinalResult(int placement, int? eliminatedInRound)
        {
            FinalPlacement = placement;
            EliminatedInRound = eliminatedInRound;
        }
    }
}
