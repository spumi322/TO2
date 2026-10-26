using Application.Contracts;
using Application.Pipelines.Common;
using Application.Pipelines.MatchResult.Contracts;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.MatchResult.Steps
{
    /// <summary>
    /// Step 4: Calculates and saves final tournament placements.
    /// Only executes if tournament has finished.
    /// </summary>
    public class CalculateFinalPlacementsStep : PipeLineBase<CalculateFinalPlacementsStep>
    {
        private readonly IPlacementService _placementService;

        public CalculateFinalPlacementsStep(
            ILogger<CalculateFinalPlacementsStep> logger,
            IPlacementService placementService) : base(logger)
        {
            _placementService = placementService;
        }

        protected override async Task<bool> ExecuteStepAsync(MatchResultContext context)
        {
            // Only execute if tournament finished
            if (!context.TournamentFinished)
            {
                return true; // Skip this step
            }

            var tournamentId = context.Tournament.Id;
            var standingId = context.StandingId;

            // Calculate final placements based on format
            var placements = context.Tournament.Format == Domain.Enums.Format.GroupsOnly
                ? await _placementService.CalculateGroupOnlyPlacements(tournamentId)
                : await _placementService.CalculateBracketPlacements(standingId);

            // GroupsAndBracket: teams knocked out in the groups are placed below the bracket teams
            if (context.Tournament.Format == Domain.Enums.Format.GroupsAndBracket)
            {
                placements.AddRange(await _placementService.CalculateGroupStageKnockoutPlacements(tournamentId));
            }

            // Save final results to database
            await _placementService.SetFinalResults(tournamentId, placements);

            // Get final results for response
            var finalStandings = await _placementService.GetFinalResultsAsync(tournamentId);

            // Store in context
            context.FinalStandings = finalStandings;

            Logger.LogInformation("Stored {PlacementCount} final placements for tournament {TournamentId}",
                placements.Count, tournamentId);

            context.Message = $"TOURNAMENT FINISHED! Champion: Team {context.WinnerId}";

            return true;
        }
    }
}
