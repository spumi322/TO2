using Application.Contracts;
using Application.Pipelines.StartBracket.Contracts;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Domain.Exceptions;

namespace Application.Pipelines.StartBracket.Steps
{
    /// <summary>
    /// Step 2: Validates that bracket standing exists and is not already seeded.
    /// Loads standings into context.
    /// </summary>
    public class ValidateBracketNotSeededStep : IStartBracketPipelineStep
    {
        private readonly ILogger<ValidateBracketNotSeededStep> _logger;
        private readonly IRepository<Standing> _standingRepository;

        public ValidateBracketNotSeededStep(
            ILogger<ValidateBracketNotSeededStep> logger,
            IRepository<Standing> standingRepository)
        {
            _logger = logger;
            _standingRepository = standingRepository;
        }

        public async Task<bool> ExecuteAsync(StartBracketContext context)
        {
            _logger.LogInformation("Step 2: Validating bracket standing for tournament {TournamentId}",
                context.TournamentId);

            // Get all standings
            var standings = await _standingRepository.FindAllAsync(s => s.TournamentId == context.TournamentId);
            var bracketStandings = standings.Where(s => s.StandingType == StandingType.Bracket).ToList();

            // The bracket standing is created with the tournament, so none means broken data (500)
            if (bracketStandings.Count == 0)
                throw new InvalidOperationException($"No bracket standing found for tournament {context.TournamentId}");

            var bracket = bracketStandings.First();

            // Check if bracket is already seeded
            if (bracket.IsSeeded)
                throw new ConflictException("Bracket is already seeded.");

            // Store in context
            context.Standings = standings.ToList();
            context.BracketStanding = bracket;

            _logger.LogInformation("Found bracket standing '{BracketName}' for tournament {TournamentId}",
                bracket.Name, context.TournamentId);

            return true; // Continue to next step
        }
    }
}
