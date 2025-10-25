using Application.Contracts;
using Application.Pipelines.GameResult.Contracts;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Application.Pipelines.GameResult.Strategies
{
    /// <summary>
    /// Strategy for progressing group standings.
    /// Checks if group finished and if all groups are finished.
    /// </summary>
    public class GroupProgressStrategy : IStandingProgressStrategy
    {
        private readonly IStandingService _standingService;
        private readonly ILogger<GroupProgressStrategy> _logger;

        public StandingType StandingType => StandingType.Group;

        public GroupProgressStrategy(
            IStandingService standingService,
            ILogger<GroupProgressStrategy> logger)
        {
            _standingService = standingService;
            _logger = logger;
        }

        public async Task<StandingProgressResult> ProgressStandingAsync(long tournamentId, long standingId, long matchId, long winnerId)
        {
            // Check if this match finishing caused the standing to finish
            var IsGroupJustFinished = await _standingService.IsGroupFinished(standingId);

            if (!IsGroupJustFinished)
            {
                // Standing not finished yet - stop pipeline (success, but standing still in progress)
                return new StandingProgressResult(
                    shouldContinuePipeline: false,
                    message: "Match completed.",
                    standingFinished: false
                );
            }

            _logger.LogInformation("Standing {StandingId} finished", standingId);

            // Mark the group as finished
            await _standingService.MarkGroupAsFinished(standingId);

            // Check if ALL groups are finished
            var allGroupsFinished = await _standingService.CheckAllGroupsAreFinished(tournamentId);

            if (!allGroupsFinished)
            {
                // Stop pipeline (success, but other groups ongoing)
                return new StandingProgressResult(
                    shouldContinuePipeline: false,
                    message: "Match completed and standing finished. Other groups still in progress.",
                    standingFinished: true,
                    allGroupsFinished: false
                );
            }

            _logger.LogInformation("All groups finished for tournament {TournamentId}", tournamentId);

            // Continue to TransitionTournamentStateStep (will transition to GroupsCompleted)
            return new StandingProgressResult(
                shouldContinuePipeline: true,
                message: "All groups finished! Tournament transitioned to GroupsCompleted status. Admin can now seed bracket.",
                standingFinished: true,
                allGroupsFinished: true
            );
        }
    }
}
