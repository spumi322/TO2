using Domain.Enums;

namespace Domain.Configuration
{
    /// <summary>
    /// Domain service that provides tournament format configuration and validation.
    /// Single source of truth for all format-related rules, constants, and behavior.
    /// Based on dictionary-driven configuration pattern.
    /// </summary>
    public class TournamentFormatConfiguration : ITournamentFormatConfiguration
    {
        private readonly Dictionary<BestOf, int> _bestOfToTotalGames = new()
        {
            { BestOf.Bo1, 1 },
            { BestOf.Bo3, 3 },
            { BestOf.Bo5, 5 }
        };

        private readonly Dictionary<BestOf, int> _bestOfToGamesToWin = new()
        {
            { BestOf.Bo1, 1 },
            { BestOf.Bo3, 2 }, // Need 2 out of 3
            { BestOf.Bo5, 3 }  // Need 3 out of 5
        };

        private readonly Dictionary<Format, string> _formatDisplayNames = new()
        {
            { Format.BracketOnly, "Bracket Only" },
            { Format.GroupsAndBracket, "Groups + Bracket" },
            { Format.GroupsOnly, "Groups Only" }
        };

        private readonly Dictionary<Format, bool> _formatRequiresGroups = new()
        {
            { Format.BracketOnly, false },
            { Format.GroupsAndBracket, true },
            { Format.GroupsOnly, true }
        };

        private readonly Dictionary<Format, bool> _formatRequiresBracket = new()
        {
            { Format.BracketOnly, true },
            { Format.GroupsAndBracket, true },
            { Format.GroupsOnly, false }
        };

        private readonly Dictionary<Format, FormatMetadata> _formatMetadata = new()
        {
            { Format.BracketOnly, new FormatMetadata
            {
                Format = Format.BracketOnly,
                DisplayName = "Bracket Only",
                Description = "Single elimination bracket - teams must be power of 2",
                RequiresGroups = false,
                RequiresBracket = true,
                MinTeams = 2,
                MaxTeams = 32,
                MinTeamsPerGroup = null,
                MaxTeamsPerGroup = null,
                MinTeamsPerBracket = 2,
                MaxTeamsPerBracket = 32
            }},
            { Format.GroupsAndBracket, new FormatMetadata
            {
                Format = Format.GroupsAndBracket,
                DisplayName = "Groups + Bracket",
                Description = "Round-robin groups followed by playoff bracket",
                RequiresGroups = true,
                RequiresBracket = true,
                MinTeams = 2,
                MaxTeams = 32,
                MinTeamsPerGroup = 2,
                MaxTeamsPerGroup = 16,
                MinTeamsPerBracket = 2,
                MaxTeamsPerBracket = 32
            }},
            { Format.GroupsOnly, new FormatMetadata
            {
                Format = Format.GroupsOnly,
                DisplayName = "Groups Only",
                Description = "Round-robin groups only, no playoff bracket",
                RequiresGroups = true,
                RequiresBracket = false,
                MinTeams = 2,
                MaxTeams = 32,
                MinTeamsPerGroup = 2,
                MaxTeamsPerGroup = 16,
                MinTeamsPerBracket = null,
                MaxTeamsPerBracket = null
            }}
        };

        private const BestOf DefaultBestOf = BestOf.Bo3;

        // BestOf Configuration

        public int GetTotalGames(BestOf bestOf)
        {
            if (!_bestOfToTotalGames.ContainsKey(bestOf))
                throw new ArgumentOutOfRangeException(nameof(bestOf), $"Invalid BestOf value: {bestOf}");

            return _bestOfToTotalGames[bestOf];
        }

        public int GetGamesToWin(BestOf bestOf)
        {
            if (!_bestOfToGamesToWin.ContainsKey(bestOf))
                throw new ArgumentOutOfRangeException(nameof(bestOf), $"Invalid BestOf value: {bestOf}");

            return _bestOfToGamesToWin[bestOf];
        }

        public BestOf GetDefaultBestOf() => DefaultBestOf;

        public IEnumerable<BestOf> GetValidBestOfValues() => _bestOfToTotalGames.Keys;

        public bool IsValidBestOf(BestOf bestOf) => _bestOfToTotalGames.ContainsKey(bestOf);

        // Format Configuration

        public string GetFormatDisplayName(Format format)
        {
            if (!_formatDisplayNames.ContainsKey(format))
                throw new ArgumentOutOfRangeException(nameof(format), $"Invalid Format value: {format}");

            return _formatDisplayNames[format];
        }

        public bool RequiresGroups(Format format)
        {
            if (!_formatRequiresGroups.ContainsKey(format))
                throw new ArgumentOutOfRangeException(nameof(format), $"Invalid Format value: {format}");

            return _formatRequiresGroups[format];
        }

        public bool RequiresBracket(Format format)
        {
            if (!_formatRequiresBracket.ContainsKey(format))
                throw new ArgumentOutOfRangeException(nameof(format), $"Invalid Format value: {format}");

            return _formatRequiresBracket[format];
        }

        public IEnumerable<Format> GetValidFormatValues() => _formatDisplayNames.Keys;

        // Format Metadata & Validation

        public FormatMetadata GetFormatMetadata(Format format)
        {
            if (!_formatMetadata.ContainsKey(format))
                throw new ArgumentOutOfRangeException(nameof(format), $"Invalid format: {format}");

            return _formatMetadata[format];
        }

        private bool IsPowerOfTwo(int n)
        {
            return n > 0 && (n & (n - 1)) == 0;
        }

        public bool ValidateTeamConfiguration(Format format, int maxTeams, int? teamsPerGroup, int? teamsPerBracket)
        {
            var metadata = GetFormatMetadata(format);

            // Check team count range
            if (maxTeams < metadata.MinTeams || maxTeams > metadata.MaxTeams)
                return false;

            if (format == Format.BracketOnly)
            {
                // BracketOnly: maxTeams must be power of 2 and equal teamsPerBracket, no groups
                return teamsPerGroup == null
                    && teamsPerBracket.HasValue
                    && IsPowerOfTwo(maxTeams)
                    && maxTeams == teamsPerBracket.Value;
            }
            else if (format == Format.GroupsAndBracket)
            {
                // GroupsAndBracket: groups required, maxTeams divisible by teamsPerGroup
                return teamsPerGroup.HasValue
                    && teamsPerGroup.Value >= metadata.MinTeamsPerGroup
                    && teamsPerGroup.Value <= metadata.MaxTeamsPerGroup
                    && maxTeams % teamsPerGroup.Value == 0;
            }
            else if (format == Format.GroupsOnly)
            {
                // GroupsOnly: groups required, maxTeams divisible by teamsPerGroup, no bracket
                return teamsPerGroup.HasValue
                    && teamsPerGroup.Value >= metadata.MinTeamsPerGroup
                    && teamsPerGroup.Value <= metadata.MaxTeamsPerGroup
                    && maxTeams % teamsPerGroup.Value == 0
                    && teamsPerBracket == null;
            }

            return false;
        }

        public string GetValidationErrorMessage(Format format, int maxTeams, int? teamsPerGroup, int? teamsPerBracket)
        {
            var metadata = GetFormatMetadata(format);

            if (maxTeams < metadata.MinTeams || maxTeams > metadata.MaxTeams)
                return $"MaxTeams must be between {metadata.MinTeams} and {metadata.MaxTeams}";

            if (format == Format.BracketOnly)
            {
                if (teamsPerGroup.HasValue)
                    return "TeamsPerGroup should not be set for BracketOnly format";
                if (!IsPowerOfTwo(maxTeams))
                    return "For BracketOnly, MaxTeams must be a power of 2 (2, 4, 8, 16, 32)";
                if (!teamsPerBracket.HasValue || maxTeams != teamsPerBracket.Value)
                    return "For BracketOnly, MaxTeams must equal TeamsPerBracket";
            }
            else if (format == Format.GroupsAndBracket)
            {
                if (!teamsPerGroup.HasValue)
                    return "TeamsPerGroup is required for GroupsAndBracket format";
                if (teamsPerGroup.Value < metadata.MinTeamsPerGroup || teamsPerGroup.Value > metadata.MaxTeamsPerGroup)
                    return $"TeamsPerGroup must be between {metadata.MinTeamsPerGroup} and {metadata.MaxTeamsPerGroup}";
                if (maxTeams % teamsPerGroup.Value != 0)
                    return "MaxTeams must be divisible by TeamsPerGroup";
            }
            else if (format == Format.GroupsOnly)
            {
                if (!teamsPerGroup.HasValue)
                    return "TeamsPerGroup is required for GroupsOnly format";
                if (teamsPerGroup.Value < metadata.MinTeamsPerGroup || teamsPerGroup.Value > metadata.MaxTeamsPerGroup)
                    return $"TeamsPerGroup must be between {metadata.MinTeamsPerGroup} and {metadata.MaxTeamsPerGroup}";
                if (maxTeams % teamsPerGroup.Value != 0)
                    return "MaxTeams must be divisible by TeamsPerGroup";
                if (teamsPerBracket.HasValue)
                    return "TeamsPerBracket should not be set for GroupsOnly format";
            }

            return string.Empty;
        }

        public int CalculateNumberOfGroups(Format format, int maxTeams, int teamsPerGroup)
        {
            var metadata = GetFormatMetadata(format);
            if (!metadata.RequiresGroups)
                throw new InvalidOperationException($"Format {format} does not use groups");

            return maxTeams / teamsPerGroup;
        }
    }
}
