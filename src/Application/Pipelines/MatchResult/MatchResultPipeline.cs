using Application.Contracts;
using Application.DTOs.Game;
using Application.DTOs.Match;
using Application.DTOs.Orchestration;
using Application.DTOs.SignalR;
using Application.Pipelines.GameResult;
using Application.Pipelines.GameResult.Contracts;
using Application.Pipelines.GameResult.Steps;
using Application.Pipelines.MatchResult.Contracts;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.StateMachine;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.MatchResult
{
    /// <summary>
    /// Saves a confirmed match result in one transaction: writes all its games (the loser's wins first,
    /// the deciding game last), then runs the game result pipeline's steps after ScoreGameStep unchanged
    /// (match winner, group stats, standing progress, state change, placements).
    /// Until Confirm, the result is only a draft shared through the hub.
    /// </summary>
    public class MatchResultPipeline : IMatchResultPipeline
    {
        private readonly ILogger<MatchResultPipeline> _logger;
        private readonly IRepository<Tournament> _tournamentRepository;
        private readonly IRepository<Standing> _standingRepository;
        private readonly IRepository<Match> _matchRepository;
        private readonly IRepository<Game> _gameRepository;
        private readonly ITournamentStateMachine _stateMachine;
        private readonly IFormatService _formatService;
        private readonly IReadOnlyList<IGameResultPipelineStep> _steps;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISignalRService _signalRService;
        private readonly ITenantService _tenantService;
        private readonly IStandingService _standingService;

        public MatchResultPipeline(
            ILogger<MatchResultPipeline> logger,
            IRepository<Tournament> tournamentRepository,
            IRepository<Standing> standingRepository,
            IRepository<Match> matchRepository,
            IRepository<Game> gameRepository,
            ITournamentStateMachine stateMachine,
            IFormatService formatService,
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
            _formatService = formatService;
            // Same steps and order as the game result pipeline, without ScoreGameStep: this pipeline writes the games itself
            _steps = steps
                .Where(step => step is not ScoreGameStep)
                .OrderBy(step => Array.IndexOf(GameResultPipeline.StepOrder, step.GetType()))
                .ToList();
            _unitOfWork = unitOfWork;
            _signalRService = signalRService;
            _tenantService = tenantService;
            _standingService = standingService;
        }

        public async Task<GameProcessResultDTO> ExecuteAsync(SetMatchResultDTO request)
        {
            var tournament = await _tournamentRepository.GetByIdAsync(request.TournamentId)
                ?? throw new NotFoundException("Tournament", request.TournamentId);
            var match = await _matchRepository.GetByIdAsync(request.MatchId)
                ?? throw new NotFoundException("Match", request.MatchId);
            var standing = await _standingRepository.GetByIdAsync(match.StandingId)
                ?? throw new NotFoundException("Standing", match.StandingId);

            Validate(request, tournament, match, standing);

            var teamAId = match.TeamAId!.Value;
            var teamBId = match.TeamBId!.Value;
            var aWon = request.TeamAWins > request.TeamBWins;
            var winnerId = aWon ? teamAId : teamBId;
            var loserId = aWon ? teamBId : teamAId;
            var winnerWins = Math.Max(request.TeamAWins, request.TeamBWins);
            var loserWins = Math.Min(request.TeamAWins, request.TeamBWins);

            var games = (await _gameRepository.FindAllAsync(g => g.MatchId == match.Id)).OrderBy(g => g.Id).ToList();
            if (games.Count < winnerWins + loserWins)
                throw new InvalidOperationException($"Match {match.Id} has {games.Count} games, the result needs {winnerWins + loserWins}");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var decidingGame = WriteGames(games, winnerId, loserId, winnerWins, loserWins);

                var context = new GameResultContext
                {
                    GameResult = new SetGameResultDTO(decidingGame.Id, winnerId, null, null, match.Id, standing.Id, tournament.Id, decidingGame.Version),
                    Tournament = tournament
                };

                foreach (var step in _steps)
                {
                    if (!await step.ExecuteAsync(context))
                    {
                        _logger.LogInformation("Match result pipeline stopped at step: {StepName}", step.GetType().Name);
                        break;
                    }
                }

                await _unitOfWork.CommitTransactionAsync();
                await BroadcastUpdatesAsync(context);

                return new GameProcessResultDTO(
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
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Match result pipeline failed, changes rolled back: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Body ids must belong together; the tournament must allow scoring; the match must be open, unchanged since
        /// the client loaded it, with both teams known; the result must end the match (one team has the wins needed).
        /// </summary>
        private void Validate(SetMatchResultDTO request, Tournament tournament, Match match, Standing standing)
        {
            if (standing.Id != request.StandingId || standing.TournamentId != request.TournamentId)
                throw new ValidationException("Match does not belong to the given standing and tournament.");

            if (!_stateMachine.CanScoreMatches(tournament.Status))
                throw new ConflictException($"Matches cannot be scored while the tournament is {tournament.Status}.");

            if (match.WinnerId.HasValue)
                throw new ConflictException("Match is already confirmed.");

            if (match.Version != request.Version)
                throw new ConflictException("Match was modified by another user. Reload and try again.");

            if (match.TeamAId is null or 0 || match.TeamBId is null or 0)
                throw new ConflictException("Both teams must be known before the match can be confirmed.");

            var gamesToWin = _formatService.GetGamesToWin(match.BestOf);
            var endsMatch =
                (request.TeamAWins == gamesToWin && request.TeamBWins >= 0 && request.TeamBWins < gamesToWin) ||
                (request.TeamBWins == gamesToWin && request.TeamAWins >= 0 && request.TeamAWins < gamesToWin);
            if (!endsMatch)
                throw new ValidationException($"A best of {(int)match.BestOf} match ends when one team has {gamesToWin} wins.");
        }

        // Loser's wins first, then the winner's, so the last played game decides the match; unplayed games stay empty
        private static Game WriteGames(IReadOnlyList<Game> games, long winnerId, long loserId, int winnerWins, int loserWins)
        {
            var winners = Enumerable.Repeat(loserId, loserWins).Concat(Enumerable.Repeat(winnerId, winnerWins)).ToList();
            for (var i = 0; i < games.Count; i++)
                games[i].WinnerId = i < winners.Count ? winners[i] : null;

            return games[winners.Count - 1];
        }

        // After commit; a failed broadcast is logged and doesn't fail the request (clients can reload)
        private async Task BroadcastUpdatesAsync(GameResultContext context)
        {
            try
            {
                await _signalRService.BroadcastGameUpdated(await BuildGameUpdatedEventAsync(context));

                // Same rule as the game result pipeline: the tournament list's stage track moves
                if (context.NewTournamentStatus is not null || context.StandingFinished || context.RoundFinished)
                    await _signalRService.BroadcastTournamentUpdated(context.Tournament.Id, _tenantService.GetCurrentUserName());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast the result of match {MatchId}. Users can refresh manually.", context.GameResult.MatchId);
            }
        }

        // Same payload as the game result pipeline: the changed group or bracket with the match and its deciding game
        private async Task<GameUpdatedEvent> BuildGameUpdatedEventAsync(GameResultContext context)
        {
            var standing = await _standingRepository.GetByIdAsync(context.GameResult.StandingId)
                ?? throw new InvalidOperationException($"Standing {context.GameResult.StandingId} not found");

            var updatedGroup = standing.StandingType == StandingType.Group
                ? await _standingService.GetGroupWithDetailsByStandingIdAsync(standing.Id)
                : null;
            var updatedBracket = standing.StandingType == StandingType.Group
                ? null
                : await _standingService.GetBracketWithDetailsAsync(context.Tournament.Id);

            var match = (updatedGroup?.Matches ?? updatedBracket?.Matches)?.FirstOrDefault(m => m.Id == context.GameResult.MatchId);
            var game = match?.Games.FirstOrDefault(g => g.Id == context.GameResult.gameId);

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
    }
}
