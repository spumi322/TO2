using Application.Contracts;
using Application.DTOs.Tournament;
using Application.Pipelines.StartGroups.Contracts;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartGroups
{
    /// <summary>
    /// Pipeline executor that orchestrates the group starting workflow.
    /// Runs all steps in sequence within a single transaction and returns the final result.
    /// </summary>
    public class StartGroupsPipeline : IStartGroupsPipeline
    {
        private readonly ILogger<StartGroupsPipeline> _logger;
        private readonly IEnumerable<IStartGroupsPipelineStep> _steps;
        private readonly IUnitOfWork _unitOfWork;

        public StartGroupsPipeline(
            ILogger<StartGroupsPipeline> logger,
            IEnumerable<IStartGroupsPipelineStep> steps,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _steps = steps;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Executes the pipeline for starting tournament groups.
        /// All changes are committed as a single atomic transaction.
        /// </summary>
        /// <param name="tournamentId">The tournament to start groups for</param>
        /// <returns>Result indicating success/failure and new tournament status</returns>
        public async Task<StartGroupsResponseDTO> ExecuteAsync(long tournamentId)
        {
            _logger.LogInformation("Starting groups pipeline for tournament {TournamentId}", tournamentId);

            // Initialize context
            var context = new StartGroupsContext
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

                    if (!shouldContinue || !context.Success)
                    {
                        _logger.LogInformation("Pipeline stopped at step: {StepName}", step.GetType().Name);
                        break;
                    }
                }

                // Commit transaction - saves all changes atomically
                await _unitOfWork.CommitTransactionAsync();

                // Return the result
                var result = new StartGroupsResponseDTO(
                    context.Success,
                    context.Message,
                    context.NewStatus,
                    context.Tournament.RowVersion
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
    }
}
