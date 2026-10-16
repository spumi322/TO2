using Application.Contracts;
using Application.Pipelines.StartGroups.Contracts;
using Domain.AggregateRoots;
using Domain.Enums;
using Domain.StateMachine;
using Microsoft.Extensions.Logging;
using Domain.Exceptions;

namespace Application.Pipelines.StartGroups.Steps
{
    /// <summary>
    /// Step 1: Validates tournament state and transitions to SeedingGroups.
    /// Closes registration and loads tournament into context.
    /// </summary>
    public class ValidateAndTransitionToSeedingGroupsStep : IStartGroupsPipelineStep
    {
        private readonly ILogger<ValidateAndTransitionToSeedingGroupsStep> _logger;
        private readonly IRepository<Tournament> _tournamentRepository;
        private readonly ITournamentStateMachine _stateMachine;
        private readonly IFormatService _formatService;

        public ValidateAndTransitionToSeedingGroupsStep(
            ILogger<ValidateAndTransitionToSeedingGroupsStep> logger,
            IRepository<Tournament> tournamentRepository,
            ITournamentStateMachine stateMachine,
            IFormatService formatService)
        {
            _logger = logger;
            _tournamentRepository = tournamentRepository;
            _stateMachine = stateMachine;
            _formatService = formatService;
        }

        public async Task<bool> ExecuteAsync(StartGroupsContext context)
        {
            _logger.LogInformation("Step 1: Validating and transitioning tournament {TournamentId} to SeedingGroups",
                context.TournamentId);

            // Load tournament
            var tournament = await _tournamentRepository.GetByIdAsync(context.TournamentId)
                ?? throw new NotFoundException("Tournament", context.TournamentId);

            // Validate format supports groups
            var metadata = _formatService.GetFormatMetadata(tournament.Format);
            if (!metadata.RequiresGroups)
                throw new ConflictException($"Cannot start groups for {metadata.DisplayName} format");

            if (!_stateMachine.IsTransitionValid(tournament.Status, TournamentStatus.SeedingGroups, tournament.Format))
                throw new ConflictException($"Groups cannot be started while the tournament is {tournament.Status}.");

            tournament.Status = TournamentStatus.SeedingGroups;
            tournament.IsRegistrationOpen = false; // Close registration when starting groups

            // Store in context
            context.Tournament = tournament;
            context.NewStatus = TournamentStatus.SeedingGroups;

            _logger.LogInformation("Tournament {TournamentId} transitioned to SeedingGroups. Registration closed.",
                context.TournamentId);

            return true; // Continue to next step
        }
    }
}
