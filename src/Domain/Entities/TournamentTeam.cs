using Domain.AggregateRoots;
using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class TournamentTeam : EntityBase
    {
        private TournamentTeam() { }

        public TournamentTeam(long tournamentId, long teamId)
        {
            TournamentId = tournamentId;
            TeamId = teamId;
        }

        public long TournamentId { get; private set; }
        public Tournament Tournament { get; private set; }

        public long TeamId { get; private set; }
        public Team Team { get; private set; }

        // Final results (populated when tournament finishes)
        public int? FinalPlacement { get; set; }
        public int? EliminatedInRound { get; set; }
        public DateTime? ResultFinalizedAt { get; set; }
    }
}
