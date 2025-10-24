using Application.Contracts;
using Application.DTOs.Game;
using Application.DTOs.Orchestration;
using Application.Pipelines.GameResult.Contracts;
using Domain.AggregateRoots;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Pipelines.GameResult
{
    /// <summary>
    /// Pipeline executor that orchestrates the game result processing workflow.
    /// Runs all steps in sequence and returns the final result.
    /// </summary>
    public class GameResultPipeline
    {
        private readonly ILogger<GameResultPipeline> _logger;
        private readonly IGenericRepository<Tournament> _tournamentRepository;
        private readonly IEnumerable<IGameResultPipelineStep> _steps;

        public GameResultPipeline(
            ILogger<GameResultPipeline> logger,
            IGenericRepository<Tournament> tournamentRepository,
            IEnumerable<IGameResultPipelineStep> steps)
        {
            _logger = logger;
            _tournamentRepository = tournamentRepository;
            _steps = steps;
        }

        /// <summary>
        /// Executes the pipeline for processing a game result.
        /// </summary>
        /// <param name="gameResult">The game result input data</param>
        /// <returns>The processed result DTO</returns>
        public async Task<GameProcessResultDTO> ExecuteAsync(SetGameResultDTO gameResult)
        {
            _logger.LogInformation("Starting game result pipeline for GameId: {GameId}", gameResult.gameId);

            try
            {
                // Initialize context
                var context = new GameResultContext
                {
                    GameResult = gameResult
                };

                // Load tournament (required for all steps)
                context.Tournament = await _tournamentRepository.Get(gameResult.TournamentId)
                    ?? throw new Exception($"Tournament with id: {gameResult.TournamentId} was not found!");

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

                _logger.LogInformation("Pipeline completed. Success: {Success}", result.Success);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pipeline execution failed: {Message}", ex.Message);
                return new GameProcessResultDTO(
                    Success: false,
                    MatchFinished: false,
                    Message: $"Pipeline failed: {ex.Message}"
                );
            }
        }
    }
}
