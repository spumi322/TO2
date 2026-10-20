using Domain.AggregateRoots;
using Domain.Common;

namespace Domain.Entities
{
    public class TournamentTeam : EntityBase
    {
        private TournamentTeam() { }

        public TournamentTeam(long tournamentId, long teamId)
        {
            TournamentId = tournamentId;
            TeamId = teamId;
        }

        // For a team created in the same save: EF sets TeamId from the navigation
        public TournamentTeam(long tournamentId, Team team)
        {
            TournamentId = tournamentId;
            Team = team;
        }

        public long TournamentId { get; private set; }
        public Tournament Tournament { get; private set; }

        public long TeamId { get; private set; }
        public Team Team { get; private set; }

        public long TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        // Final results (populated when tournament finishes)
        public int? FinalPlacement { get; set; }
        public int? EliminatedInRound { get; set; }
        public DateTime? ResultFinalizedAt { get; set; }
    }
}
