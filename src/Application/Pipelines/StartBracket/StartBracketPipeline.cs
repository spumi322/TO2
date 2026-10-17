using Application.Contracts;
using Application.DTOs.Tournament;
using Application.Pipelines.StartBracket.Contracts;
using Application.Pipelines.StartBracket.Steps;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartBracket
{
    /// <summary>
    /// Pipeline executor that orchestrates the bracket starting workflow.
    /// Runs all steps in sequence within a single transaction and returns the final result.
    /// </summary>
    public class StartBracketPipeline : IStartBracketPipeline
    {
        /// <summary>
        /// Step execution order. The constructor sorts the injected steps by it, so DI registration order doesn't matter.
        /// </summary>
        public static readonly Type[] StepOrder =
        {
            typeof(ValidateAndTransitionToSeedingBracketStep),
            typeof(ValidateBracketNotSeededStep),
            typeof(GetAdvancedTeamsStep),
            typeof(ValidateTeamCountStep),
            typeof(CalculateBracketStructureStep),
            typeof(GenerateBracketMatchesStep),
            typeof(MarkBracketAsSeededStep),
            typeof(TransitionToBracketInProgressStep),
            typeof(BuildResponseStep),
        };

        private readonly ILogger<StartBracketPipeline> _logger;
        private readonly IEnumerable<IStartBracketPipelineStep> _steps;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISignalRService _signalRService;
        private readonly ITenantService _tenantService;

        public StartBracketPipeline(
            ILogger<StartBracketPipeline> logger,
            IEnumerable<IStartBracketPipelineStep> steps,
            IUnitOfWork unitOfWork,
            ISignalRService signalRService,
            ITenantService tenantService)
        {
            _logger = logger;
            _steps = steps.OrderBy(s => Array.IndexOf(StepOrder, s.GetType())).ToList();
            _unitOfWork = unitOfWork;
            _signalRService = signalRService;
            _tenantService = tenantService;
        }

        /// <summary>
        /// Executes the pipeline for starting tournament bracket.
        /// All changes are committed as a single atomic transaction.
        /// </summary>
        /// <param name="tournamentId">The tournament to start bracket for</param>
        /// <returns>Result indicating success/failure and new tournament status</returns>
        public async Task<StartBracketResponseDTO> ExecuteAsync(long tournamentId)
        {
            _logger.LogInformation("Starting bracket pipeline for tournament {TournamentId}", tournamentId);

            // Initialize context
            var context = new StartBracketContext
            {
                TournamentId = tournamentId
            };

            // Begin transaction - all changes will be atomic
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Execute all steps in sequence
                foreach (var step in _steps)
                {
                    var shouldContinue = await step.ExecuteAsync(context);

                    if (!shouldContinue)
                    {
                        _logger.LogInformation("Pipeline stopped at step: {StepName}", step.GetType().Name);
                        break;
                    }
                }

                // Commit transaction - saves all changes atomically
                await _unitOfWork.CommitTransactionAsync();

                // Broadcast SignalR events AFTER transaction commit
                await BroadcastUpdatesAsync(context.TournamentId);

                // Return the result
                var result = new StartBracketResponseDTO(
                    context.Success,
                    context.Message,
                    context.NewStatus,
                    context.Tournament.Version
                );

                _logger.LogInformation("Pipeline completed successfully for tournament {TournamentId}. Success: {Success}",
                    tournamentId, result.Success);
                return result;
            }
            catch (Exception ex)
            {
                // Rollback transaction on any failure
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Pipeline execution failed, changes rolled back: {Message}", ex.Message);
                throw; // Let middleware handle the exception
            }
        }

        /// <summary>
        /// Broadcasts SignalR updates after transaction commits.
        /// Broadcast failure doesn't fail the request: the bracket is already started.
        /// </summary>
        private async Task BroadcastUpdatesAsync(long tournamentId)
        {
            try
            {
                var updatedBy = _tenantService.GetCurrentUserName();
                await _signalRService.BroadcastBracketStarted(tournamentId, updatedBy);
                await _signalRService.BroadcastTournamentUpdated(tournamentId, updatedBy);
                _logger.LogInformation("Broadcasted BracketStarted and TournamentUpdated for TournamentId: {TournamentId}", tournamentId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast BracketStarted for TournamentId: {TournamentId}. Users can refresh manually.", tournamentId);
            }
        }
    }
}
