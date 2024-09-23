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
    public class Standing : EntityBase
    {
        private readonly List<Match> _matches = new();

        private Standing()
        {
        }

        public Standing(string name, StandingType type, DateTime startDate, DateTime endDate, int? maxTeams)
        {
            Name = name;
            Type = type;
            StartDate = startDate;
            EndDate = endDate;
            MaxTeams = (int)maxTeams;
            CanSetMatchScore = false;
            IsFinished = false;
        }

        public long TournamentId { get; set; }

        public string Name { get; set; }

        public StandingType Type { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int MaxTeams { get; set; }

        public bool CanSetMatchScore { get; set; }

        public bool IsFinished { get; set; }

        public IReadOnlyList<Match> Matches => _matches;
    }
}
