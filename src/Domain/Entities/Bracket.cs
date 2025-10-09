using Domain.AggregateRoots;
using Domain.Common;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Bracket : EntityBase
    {
        private Bracket() { }

        public Bracket(long tournamentId, long standingId, Team team)
        {
            TournamentId = tournamentId;
            StandingId = standingId;
            TeamId = team.Id;
            TeamName = team.Name;
        }

        public long TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public long StandingId { get; set; }
        public Standing Standing { get; set; }

        public long TeamId { get; set; }
        public string TeamName { get; set; }
        public TeamStatus Status { get; set; } = TeamStatus.Competing;
        public bool Eliminated { get; set; } = false;
        public int CurrentRound { get; set; } = 1;
        public Team Team { get; set; }
    }
}
