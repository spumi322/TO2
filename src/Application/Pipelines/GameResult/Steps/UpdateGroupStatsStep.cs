using Application.Contracts;
using Application.Pipelines.Common;
using Application.Pipelines.GameResult.Contracts;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.GameResult.Steps
{
    public class UpdateGroupStatsStep : PipeLineBase<UpdateGroupStatsStep>
    {
        private readonly IGenericRepository<Group> _groupRepository;

        public UpdateGroupStatsStep(
            ILogger<UpdateGroupStatsStep> logger,
            IGenericRepository<Group> groupRepository) : base(logger)
        {
            _groupRepository = groupRepository;
        }

        protected override async Task<bool> ExecuteStepAsync(GameResultContext context)
        {
            var groupEntries = await _groupRepository.GetAllByFK("TournamentId", context.Tournament.Id);

            if (groupEntries.Any())
            {
                var winner = groupEntries.First(ge => ge.TeamId == context.MatchWinnerId);
                var loser = groupEntries.First(ge => ge.TeamId == context.MatchLoserId);

                winner.Wins++;
                winner.Points += 3;
                loser.Losses++;

                await _groupRepository.Update(winner);
                await _groupRepository.Update(loser);
                await _groupRepository.Save();

                Logger.LogInformation("Wins and points handed out for {winnerId}. Lose recorded for {loserId}. Group stats updated.", winner.Id, loser.Id);

                return true;
            }

            Logger.LogInformation("Match is not a Group match.");

            // Continues pipeline, because match is not a group match.
            return true;
        }
    }
}
