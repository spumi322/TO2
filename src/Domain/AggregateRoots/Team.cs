using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;
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
        private readonly List<TeamsTournaments> _teamsTournaments = new();

        private Team()
        {
        }

        public string Name { get; private set; }

        public IReadOnlyList<Player> Players => _players;

        public IReadOnlyList<TeamsTournaments> TeamsTournaments => _teamsTournaments;
    }
}
