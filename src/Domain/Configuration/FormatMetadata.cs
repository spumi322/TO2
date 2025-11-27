using Domain.Enums;

namespace Domain.Configuration
{
    /// <summary>
    /// Immutable metadata for tournament format configuration.
    /// Single source of truth for format-specific rules and constraints.
    /// </summary>
    public record FormatMetadata
    {
        public required Format Format { get; init; }
        public required string DisplayName { get; init; }
        public required string Description { get; init; }

        // Structure requirements
        public required bool RequiresGroups { get; init; }
        public required bool RequiresBracket { get; init; }

        // Team constraints
        public required int MinTeams { get; init; }
        public required int MaxTeams { get; init; }

        // Group configuration (nullable for formats without groups)
        public int? MinTeamsPerGroup { get; init; }
        public int? MaxTeamsPerGroup { get; init; }

        // Bracket configuration
        public required int MinTeamsPerBracket { get; init; }
        public required int MaxTeamsPerBracket { get; init; }
    }
}
