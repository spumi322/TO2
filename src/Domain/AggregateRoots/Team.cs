using Domain.Common;
using Domain.Entities;

namespace Domain.AggregateRoots
{
    public class Team : AggregateRootBase
    {
        private readonly List<Player> _players = new();
        private readonly List<Group> _tournamentParticipants = new();

        private Team() { }

        public Team(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public long TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public ICollection<TournamentTeam> TournamentParticipations { get; private set; } = new List<TournamentTeam>();

        public IReadOnlyList<Player> Players => _players;

        public IReadOnlyList<Group> TournamentParticipants => _tournamentParticipants;
    }
}
