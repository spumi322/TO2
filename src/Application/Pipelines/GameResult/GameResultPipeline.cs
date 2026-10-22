using Application.Contracts;
using Application.DTOs.Game;
using Application.DTOs.Orchestration;
using Application.DTOs.SignalR;
using Application.DTOs.Standing;
using Application.Pipelines.GameResult.Contracts;
using Application.Pipelines.GameResult.Steps;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.StateMachine;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.GameResult
{
    /// <summary>
    /// Pipeline executor that orchestrates the game result processing workflow.
    /// Runs all steps in sequence within a single transaction and returns the final result.
    /// </summary>
    public class GameResultPipeline : IGameResultPipeline
    {
        /// <summary>
        /// Step execution order. The constructor sorts the injected steps by it, so DI registration order doesn't matter.
        /// </summary>
        public static readonly Type[] StepOrder =
        {
            typeof(ScoreGameStep),
            typeof(CheckMatchCompletionStep),
            typeof(UpdateGroupStatsStep),
            typeof(HandleStandingProgressStep),
            typeof(TransitionTournamentStateStep),
            typeof(CalculateFinalPlacementsStep),
            typeof(BuildResponseStep),
        };

        private readonly ILogger<GameResultPipeline> _logger;
        private readonly IRepository<Tournament> _tournamentRepository;
        private readonly IRepository<Standing> _standingRepository;
        private readonly IRepository<Match> _matchRepository;
        private readonly IRepository<Game> _gameRepository;
        private readonly ITournamentStateMachine _stateMachine;
        private readonly IEnumerable<IGameResultPipelineStep> _steps;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISignalRService _signalRService;
        private readonly ITenantService _tenantService;
        private readonly IStandingService _standingService;

        public GameResultPipeline(
            ILogger<GameResultPipeline> logger,
            IRepository<Tournament> tournamentRepository,
            IRepository<Standing> standingRepository,
            IRepository<Match> matchRepository,
            IRepository<Game> gameRepository,
            ITournamentStateMachine stateMachine,
            IEnumerable<IGameResultPipelineStep> steps,
            IUnitOfWork unitOfWork,
            ISignalRService signalRService,
            ITenantService tenantService,
            IStandingService standingService)
        {
            _logger = logger;
            _tournamentRepository = tournamentRepository;
            _standingRepository = standingRepository;
            _matchRepository = matchRepository;
            _gameRepository = gameRepository;
            _stateMachine = stateMachine;
            _steps = steps.OrderBy(s => Array.IndexOf(StepOrder, s.GetType())).ToList();
            _unitOfWork = unitOfWork;
            _signalRService = signalRService;
            _tenantService = tenantService;
            _standingService = standingService;
        }

        /// <summary>
        /// Executes the pipeline for processing a game result.
        /// All changes are committed as a single atomic transaction.
        /// </summary>
        /// <param name="gameResult">The game result input data</param>
        /// <returns>The processed result DTO</returns>
        public async Task<GameProcessResultDTO> ExecuteAsync(SetGameResultDTO gameResult)
        {
            // Initialize context
            var context = new GameResultContext
            {
                GameResult = gameResult
            };

            // Load tournament (required for all steps)
            context.Tournament = await _tournamentRepository.GetByIdAsync(gameResult.TournamentId)
                ?? throw new NotFoundException("Tournament", gameResult.TournamentId);

            await ValidateCanScoreAsync(gameResult, context.Tournament);

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
                await BroadcastUpdatesAsync(context);

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

        /// <summary>
        /// Broadcasts SignalR updates after transaction commits.
        /// This ensures clients only receive notifications when data is persisted.
        /// Fallback to no broadcast on error (doesn't affect game result success).
        /// </summary>
        private async Task BroadcastUpdatesAsync(GameResultContext context)
        {
            try
            {
                var eventPayload = await BuildGameUpdatedEventAsync(context);
                await _signalRService.BroadcastGameUpdated(eventPayload);
                _logger.LogInformation("Broadcasted comprehensive GameUpdated event for GameId: {GameId}", context.GameResult.gameId);

                // A status change, a finished group or bracket round also concerns the tournament list (its stage track fills)
                if (context.NewTournamentStatus is not null || context.StandingFinished || context.RoundFinished)
                    await _signalRService.BroadcastTournamentUpdated(context.Tournament.Id, _tenantService.GetCurrentUserName());
            }
            catch (Exception ex)
            {
                // Broadcast failure doesn't affect game result success
                // Users can still refresh manually
                _logger.LogWarning(ex, "Failed to broadcast GameUpdated event for GameId: {GameId}. Users can refresh manually.", context.GameResult.gameId);
            }
        }

        /// <summary>
        /// Builds a comprehensive GameUpdatedEvent with all relevant data.
        /// Conditionally includes full group/bracket details when standing finishes.
        /// </summary>
        private async Task<GameUpdatedEvent> BuildGameUpdatedEventAsync(GameResultContext context)
        {
            // Fetch game and match with details from StandingService
            // Since game was just scored, we need to get the latest state with all games + calculated wins
            GetGroupsWithDetailsResponseDTO? updatedGroup = null;
            GetBracketWithDetailsResponseDTO? updatedBracket = null;

            // Fetch standing to determine type (context.StandingType may be null if match not finished)
            var standing = await _standingRepository.GetByIdAsync(context.GameResult.StandingId);
            if (standing == null)
            {
                throw new InvalidOperationException($"Standing {context.GameResult.StandingId} not found");
            }

            // Fetch the complete match details (with all games and calculated wins)
            // We'll use standing service to get properly mapped DTOs
            if (standing.StandingType == StandingType.Group)
            {
                updatedGroup = await _standingService.GetGroupWithDetailsByStandingIdAsync(context.GameResult.StandingId);
            }
            else
            {
                updatedBracket = await _standingService.GetBracketWithDetailsAsync(context.Tournament.Id);
            }

            // Extract game and match from the fetched standing
            StandingMatchDTO? match = null;
            StandingGameDTO? game = null;

            if (updatedGroup != null)
            {
                match = updatedGroup.Matches.FirstOrDefault(m => m.Id == context.GameResult.MatchId);
                if (match != null)
                {
                    game = match.Games.FirstOrDefault(g => g.Id == context.GameResult.gameId);
                }
            }
            else if (updatedBracket != null)
            {
                match = updatedBracket.Matches.FirstOrDefault(m => m.Id == context.GameResult.MatchId);
                if (match != null)
                {
                    game = match.Games.FirstOrDefault(g => g.Id == context.GameResult.gameId);
                }
            }

            // Always send full group/bracket to ensure team standings update
            // Payload is larger (~5-10KB) but UX is much better

            return new GameUpdatedEvent(
                TournamentId: context.Tournament.Id,
                GameId: context.GameResult.gameId,
                MatchId: context.GameResult.MatchId,
                StandingId: context.GameResult.StandingId,
                UpdatedBy: _tenantService.GetCurrentUserName(),
                Game: game,
                Match: match,
                MatchFinished: context.MatchFinished,
                StandingFinished: context.StandingFinished,
                AllGroupsFinished: context.AllGroupsFinished,
                TournamentFinished: context.TournamentFinished,
                UpdatedGroup: updatedGroup,
                UpdatedBracket: updatedBracket,
                FinalStandings: context.FinalStandings,
                NewTournamentStatus: context.NewTournamentStatus
            );
        }

        /// <summary>
        /// Rejects scoring when the body IDs don't match the game's real match/standing/tournament
        /// (otherwise a client could pair an active tournament's ID with another tournament's game),
        /// when the tournament's state doesn't allow scoring (e.g. Cancelled, Finished),
        /// or when the client's game Version is stale (someone scored the game since the client loaded it).
        /// </summary>
        private async Task ValidateCanScoreAsync(SetGameResultDTO gameResult, Tournament tournament)
        {
            var game = await _gameRepository.GetByIdAsync(gameResult.gameId)
                ?? throw new NotFoundException("Game", gameResult.gameId);
            var match = await _matchRepository.GetByIdAsync(game.MatchId)
                ?? throw new NotFoundException("Match", game.MatchId);
            var standing = await _standingRepository.GetByIdAsync(match.StandingId)
                ?? throw new NotFoundException("Standing", match.StandingId);

            if (match.Id != gameResult.MatchId
                || standing.Id != gameResult.StandingId
                || standing.TournamentId != gameResult.TournamentId)
                throw new ValidationException("Game does not belong to the given match, standing and tournament.");

            if (!_stateMachine.CanScoreMatches(tournament.Status))
                throw new ConflictException($"Matches cannot be scored while the tournament is {tournament.Status}.");

            if (game.Version != gameResult.Version)
                throw new ConflictException("Game was modified by another user. Reload and try again.");
        }
    }
}
