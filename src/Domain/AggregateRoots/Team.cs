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

        public string Name { get; private set; }

        public string LogoUrl { get; private set; }

        public IReadOnlyList<Player> Players => _players;
    }
}
