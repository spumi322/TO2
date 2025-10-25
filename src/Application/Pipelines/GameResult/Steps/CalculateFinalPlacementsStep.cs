using Application.Contracts;
using Application.Pipelines.Common;
using Application.Pipelines.GameResult.Contracts;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Application.Pipelines.GameResult.Steps
{
    /// <summary>
    /// Step 7: Calculates and saves final tournament placements.
    /// Only executes if tournament has finished.
    /// </summary>
    public class CalculateFinalPlacementsStep : PipeLineBase<CalculateFinalPlacementsStep>
    {
        private readonly IStandingService _standingService ;

        public CalculateFinalPlacementsStep(
            ILogger<CalculateFinalPlacementsStep> logger,
            IStandingService standingService) : base(logger)
        {
            _standingService = standingService;
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

            // Calculate final placements based on bracket results
            var placements = await _standingService.CalculateFinalPlacements(standingId);

            // Save final results to database
            await _standingService.SetFinalResults(tournamentId, placements);

            // Get final results for response
            var finalStandings = await _standingService.GetFinalResultsAsync(tournamentId);

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
