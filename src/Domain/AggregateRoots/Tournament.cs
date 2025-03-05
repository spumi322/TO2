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
        private readonly List<Group> _tournamentParticipants = new();

        private Tournament() { }

        public Tournament(string name, string description, int maxTeams, Format format)
        {
            Name = name;
            Description = description;
            MaxTeams = maxTeams;
            Format = format;
        }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(250)]
        public string? Description { get; set; }

        [Range(2, 32)]
        public int MaxTeams { get; set; }

        [Required]
        [EnumDataType(typeof(Format))]
        [Range(0, 2)]
        [DefaultValue(Format.BracketOnly)]
        public Format Format { get; set; }

        [Required]
        [EnumDataType(typeof(TournamentStatus))]
        [Range(0, 2)]
        [DefaultValue(TournamentStatus.Upcoming)]
        public TournamentStatus Status { get; set; } = TournamentStatus.Upcoming;

        public bool IsRegistrationOpen { get; set; } = false;

        public List<Prize> PrizePool { get; set; }

        public ICollection<TournamentTeam> TournamentTeams { get; private set; } = new List<TournamentTeam>();

        public IReadOnlyList<Standing> Standings => _standings;

        public IReadOnlyList<Group> TournamentParticipants => _tournamentParticipants;
    }
}
