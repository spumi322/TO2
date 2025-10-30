using Domain.AggregateRoots;
using Domain.Common;

namespace Domain.Entities
{
    public class Game : EntityBase
    {
        private Game() { }

        public Game(Match match, long? teamAId, long? teamBId)
        {
            Match = match;
            MatchId = match.Id;
            TeamAScore = 0;
            TeamBScore = 0;
            TeamAId = teamAId;
            TeamBId = teamBId;
        }

        public long MatchId { get; private set; }
        public Match Match { get; set; }

        public long TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public long? WinnerId { get; set; }

        public long? TeamAId { get; set; }

        public int? TeamAScore { get; set; }

        public long? TeamBId { get; set; }

        public int? TeamBScore { get; set; }

        public TimeSpan? Duration { get; set; }
    }
}
