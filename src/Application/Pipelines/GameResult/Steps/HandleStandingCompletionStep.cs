using Application.Contracts;
using Application.Pipelines.Common;
using Application.Pipelines.GameResult.Contracts;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Application.Pipelines.GameResult.Steps
{
    /// <summary>
    /// Step 4: Checks if the standing is complete (all matches finished).
    /// Only applies to Group standings. Bracket completion is handled separately.
    /// If standing not finished, stops pipeline with success message.
    /// If all groups finished, sets flag for state transition.
    /// </summary>
    public class HandleStandingCompletionStep : PipeLineBase<HandleStandingCompletionStep>
    {
        private readonly IStandingService _standingService;

        public HandleStandingCompletionStep(
            ILogger<HandleStandingCompletionStep> logger,
            IStandingService standingService) : base(logger)
        {
            _standingService = standingService;
        }

        protected override async Task<bool> ExecuteStepAsync(GameResultContext context)
        {
            // Only check standing completion for Group standings
            // Bracket completion is handled in ProgressBracketStep
            if (context.StandingType != StandingType.Group)
            {
                context.Message = "Match completed.";
                return true; // Continue to next step (bracket-specific logic)
            }

            // Check if this match finishing caused the standing to finish
            var standingJustFinished = await _standingService.CheckAndMarkStandingAsFinished(
                context.GameResult.StandingId);

            if (!standingJustFinished)
            {
                // Standing not finished yet
                context.StandingFinished = false;
                context.Message = "Match completed.";
                return false; // Stop pipeline (success, but standing still in progress)
            }

            context.StandingFinished = true;
            Logger.LogInformation("Standing {StandingId} finished", context.GameResult.StandingId);

            // Check if ALL groups are finished
            var allGroupsFinished = await _standingService.CheckAllGroupsAreFinished(
                context.GameResult.TournamentId);

            context.AllGroupsFinished = allGroupsFinished;

            if (!allGroupsFinished)
            {
                context.Message = "Match completed and standing finished. Other groups still in progress.";
                return false; // Stop pipeline (success, but other groups ongoing)
            }

            Logger.LogInformation("All groups finished for tournament {TournamentId}",
                context.GameResult.TournamentId);

            context.Message = "All groups finished! Tournament transitioned to GroupsCompleted status. Admin can now seed bracket.";

            // Continue to TransitionTournamentStateStep
            return true;
        }
    }
}
