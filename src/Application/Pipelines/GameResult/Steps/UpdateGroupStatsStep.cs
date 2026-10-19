using Application.Contracts;
using Application.Pipelines.Common;
using Application.Pipelines.GameResult.Contracts;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.GameResult.Steps
{
    public class UpdateGroupStatsStep : PipeLineBase<UpdateGroupStatsStep>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<GroupEntry> _groupRepository;
        private readonly IRepository<Game> _gameRepository;

        public UpdateGroupStatsStep(
            ILogger<UpdateGroupStatsStep> logger,
            IUnitOfWork unitOfWork,
            IRepository<GroupEntry> groupRepository,
            IRepository<Game> gameRepository) : base(logger)
        {
            _groupRepository = groupRepository;
            _unitOfWork = unitOfWork;
            _gameRepository = gameRepository;
        }

        protected override async Task<bool> ExecuteStepAsync(GameResultContext context)
        {
            // Entries of this match's standing only: a bracket standing has none, so bracket matches don't change group stats
            var groupEntries = await _groupRepository.FindAllAsync(ge => ge.StandingId == context.GameResult.StandingId);

            if (groupEntries.Any())
            {
                var winner = groupEntries.First(ge => ge.TeamId == context.MatchWinnerId);
                var loser = groupEntries.First(ge => ge.TeamId == context.MatchLoserId);

                // Game score of the finished match, e.g. 2-1
                var games = await _gameRepository.FindAllAsync(g => g.MatchId == context.GameResult.MatchId);
                int winnerGames = games.Count(g => g.WinnerId == winner.TeamId);
                int loserGames = games.Count(g => g.WinnerId == loser.TeamId);

                winner.Wins++;
                winner.Points += 3;
                winner.GameWins += winnerGames;
                winner.GameLosses += loserGames;
                loser.Losses++;
                loser.GameWins += loserGames;
                loser.GameLosses += winnerGames;

                Logger.LogInformation("Wins and points handed out for {winnerId}. Lose recorded for {loserId}. Group stats updated.", winner.Id, loser.Id);

                return true;
            }

            Logger.LogInformation("Match is not a Group match.");

            // Continues pipeline, because match is not a group match.
            return true;
        }
    }
}
