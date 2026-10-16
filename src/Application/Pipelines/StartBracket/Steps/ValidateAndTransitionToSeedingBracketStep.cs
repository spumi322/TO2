using Application.Contracts;
using Application.Pipelines.StartBracket.Contracts;
using Domain.AggregateRoots;
using Domain.Enums;
using Domain.StateMachine;
using Microsoft.Extensions.Logging;
using Domain.Exceptions;

namespace Application.Pipelines.StartBracket.Steps
{
    /// <summary>
    /// Step 1: Validates tournament state and transitions to SeedingBracket.
    /// Loads tournament into context.
    /// </summary>
    public class ValidateAndTransitionToSeedingBracketStep : IStartBracketPipelineStep
    {
        private readonly ILogger<ValidateAndTransitionToSeedingBracketStep> _logger;
        private readonly IRepository<Tournament> _tournamentRepository;
        private readonly ITournamentStateMachine _stateMachine;
        private readonly IFormatService _formatService;

        public ValidateAndTransitionToSeedingBracketStep(
            ILogger<ValidateAndTransitionToSeedingBracketStep> logger,
            IRepository<Tournament> tournamentRepository,
            ITournamentStateMachine stateMachine,
            IFormatService formatService)
        {
            _logger = logger;
            _tournamentRepository = tournamentRepository;
            _stateMachine = stateMachine;
            _formatService = formatService;
        }

        public async Task<bool> ExecuteAsync(StartBracketContext context)
        {
            _logger.LogInformation("Step 1: Validating and transitioning tournament {TournamentId} to SeedingBracket",
                context.TournamentId);

            // Load tournament
            var tournament = await _tournamentRepository.GetByIdAsync(context.TournamentId)
                ?? throw new NotFoundException("Tournament", context.TournamentId);

            // Validate format supports bracket
            var metadata = _formatService.GetFormatMetadata(tournament.Format);
            if (!metadata.RequiresBracket)
                throw new ConflictException($"Cannot start bracket for {metadata.DisplayName} format");

            if (!_stateMachine.IsTransitionValid(tournament.Status, TournamentStatus.SeedingBracket, tournament.Format))
                throw new ConflictException($"Bracket cannot be started while the tournament is {tournament.Status}.");

            tournament.Status = TournamentStatus.SeedingBracket;
            tournament.IsRegistrationOpen = false;

            // Store in context
            context.Tournament = tournament;
            context.NewStatus = TournamentStatus.SeedingBracket;

            _logger.LogInformation("Tournament {TournamentId} transitioned to SeedingBracket",
                context.TournamentId);

            return true; // Continue to next step
        }
    }
}
