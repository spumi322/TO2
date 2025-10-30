using Domain.Common;
using Domain.Entities;
using Domain.Enums;

namespace Domain.AggregateRoots
{
    public class Match : AggregateRootBase
    {
        private readonly List<Game> _games = new();

        public Match() { }

        public Match(Team teamA, Team teamB, BestOf bestOf)
        {
            TeamAId = teamA.Id;
            TeamBId = teamB.Id;
            BestOf = bestOf;
        }

        public long StandingId { get; set; }

        public long TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public int? Round { get; set; }

        public int? Seed { get; set; }

        public long? TeamAId { get; set; }

        public long? TeamBId { get; set; }

        public long? WinnerId { get; set; }

        public long? LoserId { get; set; }

        public BestOf BestOf { get; set; }

        public IReadOnlyList<Game> Games => _games;
    }
}
