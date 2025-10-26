using Application.Contracts;
using Application.Pipelines.Common;
using Application.Pipelines.GameResult.Contracts;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.GameResult.Steps
{
    /// <summary>
    /// Step 1: Scores the individual game by setting the winner and scores.
    /// </summary>
    public class ScoreGameStep : PipeLineBase<ScoreGameStep>
    {
        private readonly IGameService _gameService;

        public ScoreGameStep(
            ILogger<ScoreGameStep> logger,
            IGameService gameService) : base(logger)
        {
            _gameService = gameService;
        }

        protected override async Task<bool> ExecuteStepAsync(GameResultContext context)
        {
            var gameResult = context.GameResult;

            await _gameService.SetGameResult(
                gameResult.gameId,
                gameResult.WinnerId,
                gameResult.TeamAScore,
                gameResult.TeamBScore
            );

            Logger.LogInformation("Game {GameId} done. Winner: {WinnerId}",
                gameResult.gameId, gameResult.WinnerId);

            // Always continue to next step
            return true;
        }
    }
}
