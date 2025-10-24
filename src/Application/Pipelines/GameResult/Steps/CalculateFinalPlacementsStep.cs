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
        private readonly ITournamentService _tournamentService;

        public CalculateFinalPlacementsStep(
            ILogger<CalculateFinalPlacementsStep> logger,
            ITournamentService tournamentService) : base(logger)
        {
            _tournamentService = tournamentService;
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
            var placements = await _tournamentService.CalculateFinalPlacements(standingId);

            // Save final results to database
            await _tournamentService.SetFinalResults(tournamentId, placements);

            // Get final results for response
            var finalStandings = await _tournamentService.GetFinalResults(tournamentId);

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
