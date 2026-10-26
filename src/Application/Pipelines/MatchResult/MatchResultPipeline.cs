using Application.Utilities;
using Application.Contracts;
using Application.DTOs.Match;
using Application.DTOs.SignalR;
using Application.Pipelines.MatchResult.Contracts;
using Application.Pipelines.MatchResult.Steps;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.StateMachine;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.MatchResult
{
    /// <summary>
    /// Saves a confirmed match result in one transaction: writes its games (the loser's wins first, the deciding
    /// game last) and the match winner, then runs the steps (group stats, standing progress, state change, placements).
    /// Until Confirm, the result is only a draft shared through the hub.
    /// </summary>
    public class MatchResultPipeline : IMatchResultPipeline
    {
        /// <summary>
        /// Step execution order. The constructor sorts the injected steps by it, so DI registration order doesn't matter.
        /// </summary>
        public static readonly Type[] StepOrder =
        {
            typeof(UpdateGroupStatsStep),
            typeof(HandleStandingProgressStep),
            typeof(TransitionTournamentStateStep),
            typeof(CalculateFinalPlacementsStep),
        };

        private readonly ILogger<MatchResultPipeline> _logger;
        private readonly IRepository<Tournament> _tournamentRepository;
        private readonly IRepository<Standing> _standingRepository;
        private readonly IRepository<Match> _matchRepository;
        private readonly IRepository<Game> _gameRepository;
        private readonly ITournamentStateMachine _stateMachine;
        private readonly IFormatService _formatService;
        private readonly IReadOnlyList<IMatchResultPipelineStep> _steps;
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
            IEnumerable<IMatchResultPipelineStep> steps,
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
            _steps = steps.OrderBy(step => Array.IndexOf(StepOrder, step.GetType())).ToList();
            _unitOfWork = unitOfWork;
            _signalRService = signalRService;
            _tenantService = tenantService;
            _standingService = standingService;
        }

        public async Task<MatchResultResponseDTO> ExecuteAsync(SetMatchResultDTO request)
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
                match.WinnerId = winnerId;
                match.LoserId = loserId;

                var context = new MatchResultContext
                {
                    Tournament = tournament,
                    StandingId = standing.Id,
                    MatchId = match.Id,
                    WinnerId = winnerId,
                    LoserId = loserId,
                    DecidingGameId = decidingGame.Id
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

                return new MatchResultResponseDTO(
                    MatchWinnerId: winnerId,
                    MatchLoserId: loserId,
                    StandingFinished: context.StandingFinished,
                    AllGroupsFinished: context.AllGroupsFinished,
                    TournamentFinished: context.TournamentFinished,
                    NewTournamentStatus: context.NewTournamentStatus,
                    FinalStandings: context.FinalStandings,
                    Message: context.Message ?? "Match result saved."
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
                throw new ConflictException($"Matches cannot be scored while the tournament is {TournamentStatusText.Describe(tournament.Status)}.");

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
        private async Task BroadcastUpdatesAsync(MatchResultContext context)
        {
            try
            {
                await _signalRService.BroadcastGameUpdated(await BuildGameUpdatedEventAsync(context));

                // The tournament list's stage track moves: tell the tenant group too
                if (context.NewTournamentStatus is not null || context.StandingFinished || context.RoundFinished)
                    await _signalRService.BroadcastTournamentUpdated(context.Tournament.Id, _tenantService.GetCurrentUserName());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast the result of match {MatchId}. Users can refresh manually.", context.MatchId);
            }
        }

        // The changed group or bracket with the match and its deciding game
        private async Task<GameUpdatedEvent> BuildGameUpdatedEventAsync(MatchResultContext context)
        {
            var standing = await _standingRepository.GetByIdAsync(context.StandingId)
                ?? throw new InvalidOperationException($"Standing {context.StandingId} not found");

            var updatedGroup = standing.StandingType == StandingType.Group
                ? await _standingService.GetGroupWithDetailsByStandingIdAsync(standing.Id)
                : null;
            var updatedBracket = standing.StandingType == StandingType.Group
                ? null
                : await _standingService.GetBracketWithDetailsAsync(context.Tournament.Id);

            var match = (updatedGroup?.Matches ?? updatedBracket?.Matches)?.FirstOrDefault(m => m.Id == context.MatchId);
            var game = match?.Games.FirstOrDefault(g => g.Id == context.DecidingGameId);

            return new GameUpdatedEvent(
                TournamentId: context.Tournament.Id,
                GameId: context.DecidingGameId,
                MatchId: context.MatchId,
                StandingId: context.StandingId,
                UpdatedBy: _tenantService.GetCurrentUserName(),
                Game: game,
                Match: match,
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
