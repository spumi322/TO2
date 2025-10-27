using Application.Contracts;
using Application.Pipelines.StartBracket.Contracts;
using Domain.AggregateRoots;
using Domain.Enums;
using Domain.StateMachine;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartBracket.Steps
{
    /// <summary>
    /// Step 1: Validates tournament state and transitions to SeedingBracket.
    /// Loads tournament into context.
    /// </summary>
    public class ValidateAndTransitionToSeedingBracketStep : IStartBracketPipelineStep
    {
        private readonly ILogger<ValidateAndTransitionToSeedingBracketStep> _logger;
        private readonly IGenericRepository<Tournament> _tournamentRepository;
        private readonly ITournamentStateMachine _stateMachine;

        public ValidateAndTransitionToSeedingBracketStep(
            ILogger<ValidateAndTransitionToSeedingBracketStep> logger,
            IGenericRepository<Tournament> tournamentRepository,
            ITournamentStateMachine stateMachine)
        {
            _logger = logger;
            _tournamentRepository = tournamentRepository;
            _stateMachine = stateMachine;
        }

        public async Task<bool> ExecuteAsync(StartBracketContext context)
        {
            _logger.LogInformation("Step 1: Validating and transitioning tournament {TournamentId} to SeedingBracket",
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
                // Validate and transition to SeedingBracket
                _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.SeedingBracket);
                tournament.Status = TournamentStatus.SeedingBracket;

                await _tournamentRepository.Update(tournament);

                // Store in context
                context.Tournament = tournament;
                context.NewStatus = TournamentStatus.SeedingBracket;

                _logger.LogInformation("Tournament {TournamentId} transitioned to SeedingBracket",
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
