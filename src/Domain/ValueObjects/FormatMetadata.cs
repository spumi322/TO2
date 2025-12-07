using Domain.Enums;

namespace Domain.ValueObjects
{
    public record FormatMetadata
    {
        public Format Format { get; init; }
        public string DisplayName { get; init; }
        public string Description { get; init; }
        public bool RequiresGroups { get; init; }
        public bool RequiresBracket { get; init; }
        public int MinTeams { get; init; }
        public int MaxTeams { get; init; }
        public int? MinTeamsPerGroup { get; init; }
        public int? MaxTeamsPerGroup { get; init; }
        public int? MinTeamsPerBracket { get; init; }
        public int? MaxTeamsPerBracket { get; init; }

        private FormatMetadata() { } // private constructor ensures controlled creation

        // Static instances (the “typed constants”)
        public static readonly FormatMetadata GroupsOnly = new FormatMetadata
        {
            Format = Format.GroupsOnly,
            DisplayName = "Groups Only",
            Description = "Round-robin groups only",
            RequiresGroups = true,
            RequiresBracket = false,
            MinTeams = 4,
            MaxTeams = 32,
            MinTeamsPerGroup = 4,
            MaxTeamsPerGroup = 32
        };

        public static readonly FormatMetadata BracketOnly = new FormatMetadata
        {
            Format = Format.BracketOnly,
            DisplayName = "Bracket Only",
            Description = "Single elimination bracket",
            RequiresGroups = false,
            RequiresBracket = true,
            MinTeams = 4,
            MaxTeams = 32,
            MinTeamsPerBracket = 4,
            MaxTeamsPerBracket = 32
        };

        public static readonly FormatMetadata GroupsAndBracket = new FormatMetadata
        {
            Format = Format.GroupsAndBracket,
            DisplayName = "Groups + Bracket",
            Description = "Round-robin groups then playoff bracket",
            RequiresGroups = true,
            RequiresBracket = true,
            MinTeams = 4,
            MaxTeams = 32,
            MinTeamsPerGroup = 4,
            MaxTeamsPerGroup = 32,
            MinTeamsPerBracket = 4,
            MaxTeamsPerBracket = 32
        };

        public static IEnumerable<FormatMetadata> All
        {
            get
            {
                yield return GroupsOnly;
                yield return BracketOnly;
                yield return GroupsAndBracket;
            }
        }
    }
}
