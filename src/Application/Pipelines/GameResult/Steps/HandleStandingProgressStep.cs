using Application.Contracts;
using Application.Pipelines.Common;
using Application.Pipelines.GameResult.Contracts;
using Application.Pipelines.GameResult.Strategies;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.GameResult.Steps
{
    /// <summary>
    /// Step 4: Handles standing progression after match completion.
    /// Uses strategy pattern to handle Group vs Bracket logic differently.
    /// Strategy determines if standing/tournament is finished and controls pipeline flow.
    /// </summary>
    public class HandleStandingProgressStep : PipeLineBase<HandleStandingProgressStep>
    {
        private readonly IGenericRepository<Standing> _standingRepository;
        private readonly IEnumerable<IStandingProgressStrategy> _strategies;

        public HandleStandingProgressStep(
            IGenericRepository<Standing> standingRepository,
            ILogger<HandleStandingProgressStep> logger,
            IEnumerable<IStandingProgressStrategy> strategies) : base(logger)
        {
            _standingRepository = standingRepository;
            _strategies = strategies;
        }

        protected override async Task<bool> ExecuteStepAsync(GameResultContext context)
        {
            var tournamentId = context.GameResult.TournamentId;
            var standingId = context.GameResult.StandingId;
            var matchId = context.GameResult.MatchId;
            var winnerId = context.MatchWinnerId!.Value;

            // Get standing to determine type
            var standing = await _standingRepository.Get(standingId)
                ?? throw new Exception($"Standing {standingId} not found");

            context.StandingType = standing.StandingType;

            // Select appropriate strategy
            var strategy = _strategies.FirstOrDefault(s => s.StandingType == standing.StandingType)
                ?? throw new Exception($"No strategy found for standing type: {standing.StandingType}");

            // Execute strategy to progress the standing
            var result = await strategy.ProgressStandingAsync(tournamentId, standingId, matchId, winnerId);

            Logger.LogInformation("Processed {StandingType} progress for standing {StandingId}: {Message}",
                standing.StandingType, standingId, result.Message);

            // Map result to context
            context.StandingFinished = result.StandingFinished;
            context.AllGroupsFinished = result.AllGroupsFinished;
            context.TournamentFinished = result.TournamentFinished;
            context.NewTournamentStatus = result.NewTournamentStatus;
            context.Message = result.Message;

            // Return strategy's decision on whether to continue pipeline
            return result.ShouldContinuePipeline;
        }
    }
}
