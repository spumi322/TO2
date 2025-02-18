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
    public class TournamentParticipants : EntityBase
    {
        private TournamentParticipants()
        {
        }

        public TournamentParticipants(long teamId, long tournamentId, long standingId,TeamStatus status, string teamName)
        {
            TeamId = teamId;
            TournamentId = tournamentId;
            StandingId = standingId;
            Status = status;
            TeamName = teamName;
        }

        public TournamentParticipants(long teamId, long tournamentId, string teamName)
        {
            TeamId = teamId;
            TournamentId = tournamentId;
            TeamName = teamName;
        }

        public long TeamId { get; set; }
        public Team Team { get; set; }

        public long TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public long? StandingId { get; set; }
        public Standing? Standing { get; set; }

        public TeamStatus Status { get; set; } = TeamStatus.SignedUp;
        public bool Eliminated { get; set; } = false;

        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public int Points { get; set; } = 0;

        public string TeamName { get; set; }
    }
}
