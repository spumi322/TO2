using Domain.AggregateRoots;
using Domain.Common;
using Domain.DomainEvents;
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

        private Standing() { }

        public Standing(string name, int? maxTeams, StandingType standingType)
        {
            Name = name;
            MaxTeams = (int)maxTeams;
            StandingType = standingType;
        }

        public long TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public string Name { get; set; }

        public int MaxTeams { get; set; }

        public StandingType StandingType { get; set; }

        public bool IsFinished { get; set; } = false;

        public bool IsSeeded { get; set; } = false;

        public IReadOnlyList<Match> Matches => _matches;
    }
}

