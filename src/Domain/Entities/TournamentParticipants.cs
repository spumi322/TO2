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

        public TournamentParticipants(long teamId, long tournamentId, long? standingId, TeamStatus status, bool eliminated, int wins, int losses, int points, string teamName)
        {
            TeamId = teamId;
            TournamentId = tournamentId;
            StandingId = standingId;
            Status = TeamStatus.SignedUp;
            Eliminated = false;
            Wins = 0;
            Losses = 0;
            Points = 0;
            TeamName = teamName;
        }

        public long TeamId { get; set; }
        public Team Team { get; set; }

        public long TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public long? StandingId { get; set; }
        public Standing? Standing { get; set; }

        public TeamStatus Status { get; set; }
        public bool Eliminated { get; set; }

        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Points { get; set; }

        public string TeamName { get; set; }
    }
}
