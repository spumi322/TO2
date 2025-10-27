using Application.Contracts;
using Application.Pipelines.GameResult.Contracts;
using Domain.AggregateRoots;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.GameResult.Strategies
{
    /// <summary>
    /// Strategy for progressing bracket standings.
    /// Handles winner advancement to next round or tournament completion if final match.
    /// </summary>
    public class BracketProgressStrategy : IStandingProgressStrategy
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<Match> _matchRepository;
        private readonly IGameService _gameService;
        private readonly ILogger<BracketProgressStrategy> _logger;

        public StandingType StandingType => StandingType.Bracket;

        public BracketProgressStrategy(
            IRepository<Match> matchRepository,
            IUnitOfWork unitOfWork,
            IGameService gameService,
            ILogger<BracketProgressStrategy> logger)
        {
            _matchRepository = matchRepository;
            _gameService = gameService;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<StandingProgressResult> ProgressStandingAsync(long tournamentId, long standingId, long matchId, long winnerId)
        {
            // Check if this was the final match
            var isFinal = await IsFinalMatch(matchId, standingId);

            if (isFinal)
            {
                // Tournament finished - return result to trigger state transition
                _logger.LogInformation("Final match completed. Tournament will be marked as finished.");

                return new StandingProgressResult(
                    shouldContinuePipeline: true,
                    message: "Tournament finished!",
                    tournamentFinished: true,
                    newTournamentStatus: TournamentStatus.Finished
                );
            }
            else
            {
                // Not the final match - advance winner to next round
                await AdvanceWinnerToNextRound(matchId, winnerId, standingId);

                // Stop pipeline (match processed successfully, no more work needed)
                return new StandingProgressResult(
                    shouldContinuePipeline: false,
                    message: "Match completed. Winner advanced to next round."
                );
            }
        }

        /// <summary>
        /// Checks if the given match is the final match of the bracket.
        /// </summary>
        private async Task<bool> IsFinalMatch(long matchId, long standingId)
        {
            var match = await _matchRepository.GetByIdAsync(matchId)
                ?? throw new Exception($"Match {matchId} not found");

            if (!match.Round.HasValue || !match.Seed.HasValue)
            {
                return false;
            }

            // Get all matches for this standing to determine total rounds
            var allMatches = await _matchRepository.FindAllAsync(m => m.StandingId == standingId);
            int totalRounds = allMatches.Max(m => m.Round ?? 0);

            // Final match is: last round, seed 1
            bool isFinal = match.Round.Value == totalRounds && match.Seed.Value == 1;

            if (isFinal)
            {
                _logger.LogInformation("Match {MatchId} is the FINAL match (R{Round}S{Seed})",
                    matchId, match.Round, match.Seed);
            }

            return isFinal;
        }

        /// <summary>
        /// Advances the match winner to the next round by populating the appropriate team slot.
        /// </summary>
        private async Task AdvanceWinnerToNextRound(long finishedMatchId, long winnerId, long standingId)
        {
            var finishedMatch = await _matchRepository.GetByIdAsync(finishedMatchId)
                ?? throw new Exception($"Match {finishedMatchId} not found");

            if (!finishedMatch.Round.HasValue || !finishedMatch.Seed.HasValue)
            {
                throw new Exception($"Match {finishedMatchId} missing Round or Seed information");
            }

            int currentRound = finishedMatch.Round.Value;
            int currentSeed = finishedMatch.Seed.Value;

            // Calculate next round match position
            int nextRound = currentRound + 1;
            int nextSeed = (int)Math.Ceiling(currentSeed / 2.0);

            _logger.LogInformation("Match R{CurrentRound}S{CurrentSeed} finished. Winner {WinnerId} advances to R{NextRound}S{NextSeed}",
                currentRound, currentSeed, winnerId, nextRound, nextSeed);

            // Find the next round match
            var allMatches = await _matchRepository.FindAllAsync(m => m.StandingId == standingId);
            var nextMatch = allMatches.FirstOrDefault(m =>
                m.Round == nextRound &&
                m.Seed == nextSeed);

            if (nextMatch == null)
            {
                _logger.LogInformation("No next round match found (final match completed)");
                return;
            }

            // Determine which team slot (A or B) based on seed parity
            // Odd seeds (1, 3, 5...) → TeamA
            // Even seeds (2, 4, 6...) → TeamB
            if (currentSeed % 2 == 1)
            {
                nextMatch.TeamAId = winnerId;
                _logger.LogInformation("Set R{NextRound}S{NextSeed} TeamA = {WinnerId}",
                    nextRound, nextSeed, winnerId);
            }
            else
            {
                nextMatch.TeamBId = winnerId;
                _logger.LogInformation("Set R{NextRound}S{NextSeed} TeamB = {WinnerId}",
                    nextRound, nextSeed, winnerId);
            }

            // Update all games in the next match with the new team IDs
            await _gameService.UpdateGamesTeamIds(nextMatch.Id, nextMatch.TeamAId, nextMatch.TeamBId);

            // Save the updated match
            await _matchRepository.UpdateAsync(nextMatch);

            _logger.LogInformation("Winner advanced to next round successfully");
        }
    }
}
