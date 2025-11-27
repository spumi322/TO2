using Domain.Enums;

namespace Domain.Configuration
{
    /// <summary>
    /// Domain service interface for tournament format configuration and validation.
    /// Single source of truth for all format-related rules and constants.
    /// Implemented in Domain layer, injected via Application layer.
    /// </summary>
    public interface ITournamentFormatConfiguration
    {
        // BestOf Configuration

        /// <summary>
        /// Gets the total number of games in a series for the given BestOf format.
        /// </summary>
        int GetTotalGames(BestOf bestOf);

        /// <summary>
        /// Gets the number of games a team must win to win the match.
        /// </summary>
        int GetGamesToWin(BestOf bestOf);

        /// <summary>
        /// Gets the default BestOf format for matches when not specified.
        /// </summary>
        BestOf GetDefaultBestOf();

        /// <summary>
        /// Gets all valid BestOf values that are supported.
        /// </summary>
        IEnumerable<BestOf> GetValidBestOfValues();

        /// <summary>
        /// Checks if a BestOf value is valid and supported.
        /// </summary>
        bool IsValidBestOf(BestOf bestOf);

        // Format Configuration

        /// <summary>
        /// Gets the display name for a tournament format.
        /// </summary>
        string GetFormatDisplayName(Format format);

        /// <summary>
        /// Checks if the format requires group stage.
        /// </summary>
        bool RequiresGroups(Format format);

        /// <summary>
        /// Checks if the format requires bracket stage.
        /// </summary>
        bool RequiresBracket(Format format);

        /// <summary>
        /// Gets all valid Format values that are supported.
        /// </summary>
        IEnumerable<Format> GetValidFormatValues();

        // Format Metadata & Validation

        /// <summary>
        /// Gets the complete metadata for a tournament format.
        /// </summary>
        FormatMetadata GetFormatMetadata(Format format);

        /// <summary>
        /// Validates if the team configuration is valid for the given format.
        /// </summary>
        bool ValidateTeamConfiguration(Format format, int maxTeams, int? teamsPerGroup, int? teamsPerBracket);

        /// <summary>
        /// Gets a detailed validation error message for invalid team configuration.
        /// </summary>
        string GetValidationErrorMessage(Format format, int maxTeams, int? teamsPerGroup, int? teamsPerBracket);

        /// <summary>
        /// Calculates the number of groups needed for the given format and team counts.
        /// </summary>
        int CalculateNumberOfGroups(Format format, int maxTeams, int teamsPerGroup);
    }
}
