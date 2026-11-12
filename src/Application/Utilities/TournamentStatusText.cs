using Domain.Enums;

namespace Application.Utilities
{
    /// <summary>
    /// A tournament status in words for messages users read ("... while the tournament is playing its groups"),
    /// so no enum name reaches the UI.
    /// </summary>
    public static class TournamentStatusText
    {
        public static string Describe(TournamentStatus status) => status switch
        {
            TournamentStatus.Setup => "open for registration",
            TournamentStatus.SeedingGroups => "drawing its groups",
            TournamentStatus.GroupsInProgress => "playing its groups",
            TournamentStatus.GroupsCompleted => "done with its groups",
            TournamentStatus.SeedingBracket => "seeding its bracket",
            TournamentStatus.BracketInProgress => "playing its bracket",
            TournamentStatus.Finished => "finished",
            TournamentStatus.Cancelled => "cancelled",
            _ => "in another stage",
        };
    }
}
