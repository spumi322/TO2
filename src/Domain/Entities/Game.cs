using Domain.AggregateRoots;
using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Game : EntityBase
    {
        private Game() { }

        public Game(Match match, long teamAId, long teamBId)
        {
            MatchId = match.Id;
            TeamAScore = 0;
            TeamBScore = 0;
            TeamAId = teamAId;
            TeamBId = teamBId;
        }

        public long MatchId { get; private set; }

        public long? WinnerId { get; set; }

        public long TeamAId { get; private set; }

        public int? TeamAScore { get; set; }

        public long TeamBId { get; private set; }

        public int? TeamBScore { get; set; }

        public TimeSpan? Duration { get; set; }
    }
}
