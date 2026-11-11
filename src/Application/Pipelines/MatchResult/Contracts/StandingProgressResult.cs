namespace Application.Pipelines.MatchResult.Contracts
{
    /// <summary>
    /// Result returned by standing progress strategies.
    /// Communicates what happened during standing progression without coupling to context.
    /// </summary>
    public class StandingProgressResult
    {
        public bool ShouldContinuePipeline { get; set; }
        public bool StandingFinished { get; set; }
        // Bracket: every match of the finished match's round has a winner
        public bool RoundFinished { get; set; }
        public bool AllGroupsFinished { get; set; }
        public bool TournamentFinished { get; set; }

        public StandingProgressResult(
            bool shouldContinuePipeline,
            bool standingFinished = false,
            bool allGroupsFinished = false,
            bool tournamentFinished = false)
        {
            ShouldContinuePipeline = shouldContinuePipeline;
            StandingFinished = standingFinished;
            AllGroupsFinished = allGroupsFinished;
            TournamentFinished = tournamentFinished;
        }
    }
}
