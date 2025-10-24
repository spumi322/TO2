using Application.Contracts;
using Application.Pipelines.Common;
using Application.Pipelines.GameResult.Contracts;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Pipelines.GameResult.Steps
{
    /// <summary>
    /// Step 5: Handles bracket progression (bracket-only logic).
    /// Advances winner to next round or marks tournament as finished if final match.
    /// Skips execution for Group standings.
    /// </summary>
    public class ProgressBracketStep : PipeLineBase<ProgressBracketStep>
    {
        private readonly IGenericRepository<Match> _matchRepository;
        private readonly IGameService _gameService;

        public ProgressBracketStep(
            ILogger<ProgressBracketStep> logger,
            IGenericRepository<Match> matchRepository,
            IGameService gameService) : base(logger)
        {
            _matchRepository = matchRepository;
            _gameService = gameService;
        }

        protected override async Task<bool> ExecuteStepAsync(GameResultContext context)
        {
            // Only execute for Bracket standings
            if (context.StandingType != StandingType.Bracket)
            {
                return true; // Skip this step for groups
            }

            var matchId = context.GameResult.MatchId;
            var standingId = context.GameResult.StandingId;
            var winnerId = context.MatchWinnerId!.Value;

            // Check if this was the final match
            var isFinal = await IsFinalMatch(matchId, standingId);

            if (isFinal)
            {
                // Tournament finished - mark for state transition
                context.TournamentFinished = true;
                context.NewTournamentStatus = TournamentStatus.Finished;

                Logger.LogInformation("Final match completed. Tournament will be marked as finished.");

                // Continue to TransitionTournamentStateStep and CalculateFinalPlacementsStep
                return true;
            }
            else
            {
                // Not the final match - advance winner to next round
                await AdvanceWinnerToNextRound(matchId, winnerId, standingId);

                context.Message = "Match completed. Winner advanced to next round.";

                // Stop pipeline (match processed successfully, no more work needed)
                return false;
            }
        }

        /// <summary>
        /// Checks if the given match is the final match of the bracket.
        /// </summary>
        private async Task<bool> IsFinalMatch(long matchId, long standingId)
        {
            var match = await _matchRepository.Get(matchId)
                ?? throw new Exception($"Match {matchId} not found");

            if (!match.Round.HasValue || !match.Seed.HasValue)
            {
                return false;
            }

            // Get all matches for this standing to determine total rounds
            var allMatches = await _matchRepository.GetAllByFK("StandingId", standingId);
            int totalRounds = allMatches.Max(m => m.Round ?? 0);

            // Final match is: last round, seed 1
            bool isFinal = match.Round.Value == totalRounds && match.Seed.Value == 1;

            if (isFinal)
            {
                Logger.LogInformation("Match {MatchId} is the FINAL match (R{Round}S{Seed})",
                    matchId, match.Round, match.Seed);
            }

            return isFinal;
        }

        /// <summary>
        /// Advances the match winner to the next round by populating the appropriate team slot.
        /// </summary>
        private async Task AdvanceWinnerToNextRound(long finishedMatchId, long winnerId, long standingId)
        {
            var finishedMatch = await _matchRepository.Get(finishedMatchId)
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

            Logger.LogInformation("Match R{CurrentRound}S{CurrentSeed} finished. Winner {WinnerId} advances to R{NextRound}S{NextSeed}",
                currentRound, currentSeed, winnerId, nextRound, nextSeed);

            // Find the next round match
            var allMatches = await _matchRepository.GetAllByFK("StandingId", standingId);
            var nextMatch = allMatches.FirstOrDefault(m =>
                m.Round == nextRound &&
                m.Seed == nextSeed);

            if (nextMatch == null)
            {
                Logger.LogInformation("No next round match found (final match completed)");
                return;
            }

            // Determine which team slot (A or B) based on seed parity
            // Odd seeds (1, 3, 5...) → TeamA
            // Even seeds (2, 4, 6...) → TeamB
            if (currentSeed % 2 == 1)
            {
                nextMatch.TeamAId = winnerId;
                Logger.LogInformation("Set R{NextRound}S{NextSeed} TeamA = {WinnerId}",
                    nextRound, nextSeed, winnerId);
            }
            else
            {
                nextMatch.TeamBId = winnerId;
                Logger.LogInformation("Set R{NextRound}S{NextSeed} TeamB = {WinnerId}",
                    nextRound, nextSeed, winnerId);
            }

            // Update all games in the next match with the new team IDs
            await _gameService.UpdateGamesTeamIds(nextMatch.Id, nextMatch.TeamAId, nextMatch.TeamBId);

            // Save the updated match
            await _matchRepository.Update(nextMatch);
            await _matchRepository.Save();

            Logger.LogInformation("Winner advanced to next round successfully");
        }
    }
}
