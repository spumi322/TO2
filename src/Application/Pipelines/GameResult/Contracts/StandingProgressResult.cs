using Domain.Enums;

namespace Application.Pipelines.GameResult.Contracts
{
    /// <summary>
    /// Result returned by standing progress strategies.
    /// Communicates what happened during standing progression without coupling to context.
    /// </summary>
    public class StandingProgressResult
    {
        public bool ShouldContinuePipeline { get; set; }
        public bool StandingFinished { get; set; }
        public bool AllGroupsFinished { get; set; }
        public bool TournamentFinished { get; set; }
        public TournamentStatus? NewTournamentStatus { get; set; }
        public string Message { get; set; } = string.Empty;

        public StandingProgressResult()
        {
        }

        public StandingProgressResult(
            bool shouldContinuePipeline,
            string message,
            bool standingFinished = false,
            bool allGroupsFinished = false,
            bool tournamentFinished = false,
            TournamentStatus? newTournamentStatus = null)
        {
            ShouldContinuePipeline = shouldContinuePipeline;
            Message = message;
            StandingFinished = standingFinished;
            AllGroupsFinished = allGroupsFinished;
            TournamentFinished = tournamentFinished;
            NewTournamentStatus = newTournamentStatus;
        }
    }
}
