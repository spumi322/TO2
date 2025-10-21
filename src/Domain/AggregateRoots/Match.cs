using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.AggregateRoots
{
    public class Match : AggregateRootBase
    {
        private readonly List<Game> _games = new();

        public Match() { }  // Public for EF Core and TBD match creation

        public Match(Team teamA, Team teamB, BestOf bestOf)
        {
            TeamAId = teamA.Id;
            TeamBId = teamB.Id;
            BestOf = bestOf;
        }

        public long StandingId { get; set; }

        public int? Round { get; set; }

        public int? Seed { get; set; }

        public long TeamAId { get; set; }

        public long TeamBId { get; set; }

        public long? WinnerId { get; set; }

        public long? LoserId { get; set; }

        public BestOf BestOf { get; set; }

        public IReadOnlyList<Game> Games => _games;
    }
}
