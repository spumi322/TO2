using Application.Contracts;
using Application.Pipelines.StartGroups.Contracts;
using Domain.AggregateRoots;
using Domain.Enums;
using Domain.StateMachine;
using Microsoft.Extensions.Logging;

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
            var tournament = await _tournamentRepository.GetByIdAsync(context.TournamentId);
            if (tournament == null)
            {
                context.Success = false;
                context.Message = "Tournament not found";
                _logger.LogWarning("Tournament {TournamentId} not found", context.TournamentId);
                return false;
            }

            // Validate format supports groups
            var metadata = _formatService.GetFormatMetadata(tournament.Format);
            if (!metadata.RequiresGroups)
            {
                context.Success = false;
                context.Message = $"Cannot start groups for {metadata.DisplayName} format";
                _logger.LogWarning("Tournament {TournamentId} has format {Format} which does not use groups",
                    context.TournamentId, tournament.Format);
                return false;
            }

            try
            {
                // Validate and transition to SeedingGroups using format-aware validation
                _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.SeedingGroups, tournament.Format);
                tournament.Status = TournamentStatus.SeedingGroups;
                tournament.IsRegistrationOpen = false; // Close registration when starting groups

                // Store in context
                context.Tournament = tournament;
                context.NewStatus = TournamentStatus.SeedingGroups;

                _logger.LogInformation("Tournament {TournamentId} transitioned to SeedingGroups. Registration closed.",
                    context.TournamentId);

                return true; // Continue to next step
            }
            catch (InvalidOperationException ex)
            {
                context.Success = false;
                context.Message = $"Invalid state transition: {ex.Message}";
                _logger.LogWarning("Invalid state transition for tournament {TournamentId}: {Message}",
                    context.TournamentId, ex.Message);
                return false; // Stop pipeline
            }
        }
    }
}
