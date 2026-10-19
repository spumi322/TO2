using Application.Contracts;
using Application.Pipelines.Common;
using Application.Pipelines.GameResult.Contracts;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.GameResult.Steps
{
    /// <summary>
    /// Step 6: Calculates and saves final tournament placements.
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

        protected override async Task<bool> ExecuteStepAsync(GameResultContext context)
        {
            // Only execute if tournament finished
            if (!context.TournamentFinished)
            {
                return true; // Skip this step
            }

            var tournamentId = context.Tournament.Id;
            var standingId = context.GameResult.StandingId;

            // Calculate final placements based on format
            var placements = context.Tournament.Format == Domain.Enums.Format.GroupsOnly
                ? await _placementService.CalculateGroupOnlyPlacements(tournamentId)
                : await _placementService.CalculateBracketPlacements(standingId);

            // Save final results to database
            await _placementService.SetFinalResults(tournamentId, placements);

            // Get final results for response
            var finalStandings = await _placementService.GetFinalResultsAsync(tournamentId);

            // Store in context
            context.FinalStandings = finalStandings;

            Logger.LogInformation("Final placements calculated and saved for tournament {TournamentId}",
                tournamentId);

            // Update message
            if (context.MatchWinnerId.HasValue)
            {
                context.Message = $"TOURNAMENT FINISHED! Champion: Team {context.MatchWinnerId.Value}";
            }

            // Continue to BuildResponseStep
            return true;
        }
    }
}
