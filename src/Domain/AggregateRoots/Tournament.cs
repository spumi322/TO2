using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.AggregateRoots
{
    public class Tournament : AggregateRootBase
    {
        private readonly List<Standing> _standings = new();
        private readonly List<TeamsTournaments> _teamsTournaments = new();

        private Tournament()
        {
        }

        public Tournament(string name, string description, int maxTeams, DateTime startDate, DateTime endDate, Format format, TournamentStatus status)
        {
            Name = name;
            Description = description;
            MaxTeams = maxTeams;
            StartDate = startDate;
            EndDate = endDate;
            Format = format;
            Status = status;
        }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(250)]
        public string? Description { get; set; }

        [Range(2, 32)]
        public int MaxTeams { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        [EnumDataType(typeof(Format))]
        [Range(0, 2)]
        [DefaultValue(Format.BracketOnly)]
        public Format Format { get; set; }

        [Required]
        [EnumDataType(typeof(TournamentStatus))]
        [Range(0, 2)]
        [DefaultValue(TournamentStatus.Upcoming)]
        public TournamentStatus Status { get; set; }

        public List<Prize> PrizePool { get; set; }

        public IReadOnlyList<Standing> Standings => _standings;

        public IReadOnlyList<TeamsTournaments> TeamsTournaments => _teamsTournaments;
    }
}
