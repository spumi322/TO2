using Application.Contracts;
using Application.Pipelines.StartBracket.Contracts;
using Domain.AggregateRoots;
using Domain.Configuration;
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
        private readonly IRepository<Tournament> _tournamentRepository;
        private readonly ITournamentStateMachine _stateMachine;
        private readonly ITournamentFormatConfiguration _formatConfig;

        public ValidateAndTransitionToSeedingBracketStep(
            ILogger<ValidateAndTransitionToSeedingBracketStep> logger,
            IRepository<Tournament> tournamentRepository,
            ITournamentStateMachine stateMachine,
            ITournamentFormatConfiguration formatConfig)
        {
            _logger = logger;
            _tournamentRepository = tournamentRepository;
            _stateMachine = stateMachine;
            _formatConfig = formatConfig;
        }

        public async Task<bool> ExecuteAsync(StartBracketContext context)
        {
            _logger.LogInformation("Step 1: Validating and transitioning tournament {TournamentId} to SeedingBracket",
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

            // Validate format supports bracket
            var metadata = _formatConfig.GetFormatMetadata(tournament.Format);
            if (!metadata.RequiresBracket)
            {
                context.Success = false;
                context.Message = $"Cannot start bracket for {metadata.DisplayName} format";
                _logger.LogWarning("Tournament {TournamentId} has format {Format} which does not use bracket",
                    context.TournamentId, tournament.Format);
                return false;
            }

            try
            {
                // Validate and transition to SeedingBracket using format-aware validation
                _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.SeedingBracket, tournament.Format);
                tournament.Status = TournamentStatus.SeedingBracket;

                await _tournamentRepository.UpdateAsync(tournament);

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
