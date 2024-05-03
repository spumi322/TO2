using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.AggregateRoots
{
    public class Tournament : AggregateRootBase
    {
        private readonly List<Standing> _standings = new();
        private readonly List<Team> _teams = new();

        private Tournament()
        {
        }

        public Tournament(string name, string description, int maxTeams, DateTime startDate, DateTime endDate, Format format)
        {
            Name = name;
            Description = description;
            MaxTeams = maxTeams;
            StartDate = startDate;
            EndDate = endDate;
            Format = format;
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public int MaxTeams { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public Format Format { get; set; }

        public TournamentStatus Status { get; set; }

        public List<Prize> PrizePool { get; set; }

        public IReadOnlyList<Team> Teams => _teams;

        public IReadOnlyList<Standing> Standings => _standings;
    }
}
