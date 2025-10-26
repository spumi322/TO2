using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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
        public string Name { get; private set; }

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
        [DefaultValue(TournamentStatus.Setup)]
        public TournamentStatus Status { get; set; } = TournamentStatus.Setup;

        public bool IsRegistrationOpen { get; set; } = true;

        public List<Prize> PrizePool { get; set; }

        public ICollection<TournamentTeam> TournamentTeams { get; private set; } = new List<TournamentTeam>();

        public IReadOnlyList<Standing> Standings => _standings;

        public IReadOnlyList<Group> TournamentParticipants => _tournamentParticipants;
    }
}
