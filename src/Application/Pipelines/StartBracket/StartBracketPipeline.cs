using Application.Contracts;
using Application.DTOs.Tournament;
using Application.Pipelines.StartBracket.Contracts;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartBracket
{
    /// <summary>
    /// Pipeline executor that orchestrates the bracket starting workflow.
    /// Runs all steps in sequence within a single transaction and returns the final result.
    /// </summary>
    public class StartBracketPipeline : IStartBracketPipeline
    {
        private readonly ILogger<StartBracketPipeline> _logger;
        private readonly IEnumerable<IStartBracketPipelineStep> _steps;
        private readonly IUnitOfWork _unitOfWork;

        public StartBracketPipeline(
            ILogger<StartBracketPipeline> logger,
            IEnumerable<IStartBracketPipelineStep> steps,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _steps = steps;
            _unitOfWork = unitOfWork;
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

                    if (!shouldContinue || !context.Success)
                    {
                        _logger.LogInformation("Pipeline stopped at step: {StepName}", step.GetType().Name);
                        break;
                    }
                }

                // Commit transaction - saves all changes atomically
                await _unitOfWork.CommitTransactionAsync();

                // Return the result
                var result = new StartBracketResponseDTO(
                    context.Success,
                    context.Message,
                    context.NewStatus
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
