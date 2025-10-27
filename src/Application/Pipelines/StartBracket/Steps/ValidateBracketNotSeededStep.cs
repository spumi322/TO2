using Application.Contracts;
using Application.Pipelines.StartBracket.Contracts;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartBracket.Steps
{
    /// <summary>
    /// Step 2: Validates that bracket standing exists and is not already seeded.
    /// Loads standings into context.
    /// </summary>
    public class ValidateBracketNotSeededStep : IStartBracketPipelineStep
    {
        private readonly ILogger<ValidateBracketNotSeededStep> _logger;
        private readonly IGenericRepository<Standing> _standingRepository;

        public ValidateBracketNotSeededStep(
            ILogger<ValidateBracketNotSeededStep> logger,
            IGenericRepository<Standing> standingRepository)
        {
            _logger = logger;
            _standingRepository = standingRepository;
        }

        public async Task<bool> ExecuteAsync(StartBracketContext context)
        {
            _logger.LogInformation("Step 2: Validating bracket standing for tournament {TournamentId}",
                context.TournamentId);

            // Get all standings
            var standings = await _standingRepository.GetAllByFK("TournamentId", context.TournamentId);
            var bracketStandings = standings.Where(s => s.StandingType == StandingType.Bracket).ToList();

            if (bracketStandings.Count == 0)
            {
                context.Success = false;
                context.Message = "No bracket standing found for this tournament";
                _logger.LogWarning("No bracket standing found for tournament {TournamentId}",
                    context.TournamentId);
                return false;
            }

            var bracket = bracketStandings.First();

            // Check if bracket is already seeded
            if (bracket.IsSeeded)
            {
                context.Success = false;
                context.Message = "Bracket is already seeded!";
                _logger.LogWarning("Bracket for tournament {TournamentId} is already seeded",
                    context.TournamentId);
                return false;
            }

            // Store in context
            context.Standings = standings.ToList();
            context.BracketStanding = bracket;

            _logger.LogInformation("Found bracket standing '{BracketName}' for tournament {TournamentId}",
                bracket.Name, context.TournamentId);

            return true; // Continue to next step
        }
    }
}
