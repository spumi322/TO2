namespace Application.Utilities
{
    /// <summary>
    /// Fewest registered teams a tournament can start with. Creation plans groups of at least 3;
    /// at start only what can be played is required: every group needs 2 teams (one match, so it can finish),
    /// a bracket needs 2 teams (one final). Used by the start flags and the start pipelines alike.
    /// </summary>
    public static class StartRules
    {
        public const int MinTeamsPerGroup = 2;
        public const int MinTeamsForBracket = 2;

        public static int MinTeamsToStart(int groupCount) =>
            groupCount > 0 ? groupCount * MinTeamsPerGroup : MinTeamsForBracket;

        public static string NotEnoughTeamsMessage(int groupCount, int registered) =>
            groupCount > 0
                ? $"{groupCount} {(groupCount == 1 ? "group needs" : "groups need")} at least {MinTeamsToStart(groupCount)} teams ({MinTeamsPerGroup} per group); {registered} registered."
                : $"At least {MinTeamsForBracket} teams are needed to start; {registered} registered.";
    }
}
