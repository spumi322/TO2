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
        private readonly IGenericRepository<Tournament> _tournamentRepository;
        private readonly ITournamentStateMachine _stateMachine;

        public ValidateAndTransitionToSeedingGroupsStep(
            ILogger<ValidateAndTransitionToSeedingGroupsStep> logger,
            IGenericRepository<Tournament> tournamentRepository,
            ITournamentStateMachine stateMachine)
        {
            _logger = logger;
            _tournamentRepository = tournamentRepository;
            _stateMachine = stateMachine;
        }

        public async Task<bool> ExecuteAsync(StartGroupsContext context)
        {
            _logger.LogInformation("Step 1: Validating and transitioning tournament {TournamentId} to SeedingGroups",
                context.TournamentId);

            // Load tournament
            var tournament = await _tournamentRepository.Get(context.TournamentId);
            if (tournament == null)
            {
                context.Success = false;
                context.Message = "Tournament not found";
                _logger.LogWarning("Tournament {TournamentId} not found", context.TournamentId);
                return false;
            }

            try
            {
                // Validate and transition to SeedingGroups
                _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.SeedingGroups);
                tournament.Status = TournamentStatus.SeedingGroups;
                tournament.IsRegistrationOpen = false; // Close registration when starting groups

                await _tournamentRepository.Update(tournament);

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
