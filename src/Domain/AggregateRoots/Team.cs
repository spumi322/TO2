using Domain.Common;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.AggregateRoots
{
    public class Team : AggregateRootBase
    {
        private readonly List<Player> _players = new ();
        private readonly List<Group> _tournamentParticipants = new();

        private Team() { }

        public Team(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public ICollection<TournamentTeam> TournamentParticipations { get; private set; } = new List<TournamentTeam>();

        public IReadOnlyList<Player> Players => _players;

        public IReadOnlyList<Group> TournamentParticipants => _tournamentParticipants;
    }
}
