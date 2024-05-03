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
        public long MatchId { get; private set; }

        public Team? Winner { get; private set; }

        public int? TeamAScore { get; private set; }

        public int? TeamBScore { get; private set; }

        public TimeSpan? Duration { get; private set; }
    }
}
