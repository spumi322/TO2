using Domain.AggregateRoots;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    public class Group : EntityBase
    {
        private Group() { }

        public Group(long tournamentId, long standingId, Team team)
        {
            TournamentId = tournamentId;
            StandingId = standingId;
            TeamId = team.Id;
            TeamName = team.Name;
            Team = team;
        }

        // Constructor without Team entity to avoid EF Core tracking conflicts
        public Group(long tournamentId, long standingId, long teamId, string teamName)
        {
            TournamentId = tournamentId;
            StandingId = standingId;
            TeamId = teamId;
            TeamName = teamName;
            // Don't set Team navigation property - let EF Core handle it
        }

        public long TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public long StandingId { get; set; }
        public Standing Standing { get; set; }

        public long TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public long TeamId { get; set; }
        public string TeamName { get; set; }
        public TeamStatus Status { get; set; } = TeamStatus.Competing;
        public bool Eliminated { get; set; } = false;
        public Team Team { get; set; }

        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public int Points { get; set; } = 0;
    }
}
