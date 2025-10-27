using Application.Contracts;
using Application.DTOs.Game;
using Application.DTOs.Orchestration;
using Application.Pipelines.GameResult.Contracts;
using Domain.AggregateRoots;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.GameResult
{
    /// <summary>
    /// Pipeline executor that orchestrates the game result processing workflow.
    /// Runs all steps in sequence within a single transaction and returns the final result.
    /// </summary>
    public class GameResultPipeline : IGameResultPipeline
    {
        private readonly ILogger<GameResultPipeline> _logger;
        private readonly IRepository<Tournament> _tournamentRepository;
        private readonly IEnumerable<IGameResultPipelineStep> _steps;
        private readonly IUnitOfWork _unitOfWork;

        public GameResultPipeline(
            ILogger<GameResultPipeline> logger,
            IRepository<Tournament> tournamentRepository,
            IEnumerable<IGameResultPipelineStep> steps,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _tournamentRepository = tournamentRepository;
            _steps = steps;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Executes the pipeline for processing a game result.
        /// All changes are committed as a single atomic transaction.
        /// </summary>
        /// <param name="gameResult">The game result input data</param>
        /// <returns>The processed result DTO</returns>
        public async Task<GameProcessResultDTO> ExecuteAsync(SetGameResultDTO gameResult)
        {
            _logger.LogInformation("Starting game result pipeline for GameId: {GameId}", gameResult.gameId);

            // Initialize context
            var context = new GameResultContext
            {
                GameResult = gameResult
            };

            // Load tournament (required for all steps)
            context.Tournament = await _tournamentRepository.GetByIdAsync(gameResult.TournamentId)
                ?? throw new NotFoundException("Tournament", gameResult.TournamentId);

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

                // Return the result (BuildResponseStep should have populated this)
                var result = new GameProcessResultDTO(
                    Success: context.Success,
                    MatchFinished: context.MatchFinished,
                    MatchWinnerId: context.MatchWinnerId,
                    MatchLoserId: context.MatchLoserId,
                    StandingFinished: context.StandingFinished,
                    AllGroupsFinished: context.AllGroupsFinished,
                    TournamentFinished: context.TournamentFinished,
                    NewTournamentStatus: context.NewTournamentStatus,
                    FinalStandings: context.FinalStandings,
                    Message: context.Message
                );

                _logger.LogInformation("Pipeline completed successfully. Success: {Success}", result.Success);
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
