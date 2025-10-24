using Application.Contracts;
using Application.Pipelines.Common;
using Application.Pipelines.GameResult.Contracts;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Application.Pipelines.GameResult.Steps
{
    /// <summary>
    /// Step 2: Checks if the match has been completed (enough games won by one team).
    /// If match not finished, stops the pipeline with success message.
    /// </summary>
    public class CheckMatchCompletionStep : PipeLineBase<CheckMatchCompletionStep>
    {
        private readonly IGameService _gameService;

        public CheckMatchCompletionStep(
            ILogger<CheckMatchCompletionStep> logger,
            IGameService gameService) : base(logger)
        {
            _gameService = gameService;
        }

        protected override async Task<bool> ExecuteStepAsync(GameResultContext context)
        {
            var matchId = context.GameResult.MatchId;

            var matchWinner = await _gameService.SetMatchWinner(matchId);

            if (matchWinner is null)
            {
                // Match still in progress
                context.MatchFinished = false;
                context.Message = "Game result recorded. Match still in progress.";

                Logger.LogInformation("Match {MatchId} still in progress (no winner yet)", matchId);

                // Stop pipeline (but with success = true)
                return false;
            }

            // Match finished - populate context
            context.MatchFinished = true;
            context.MatchWinnerId = matchWinner.WinnerId;
            context.MatchLoserId = matchWinner.LoserId;

            Logger.LogInformation("Match {MatchId} finished. Winner: {WinnerId}, Loser: {LoserId}",
                matchId, matchWinner.WinnerId, matchWinner.LoserId);

            // Continue to next step
            return true;
        }
    }
}
