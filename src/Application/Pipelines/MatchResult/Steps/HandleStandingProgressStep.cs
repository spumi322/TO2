using Application.Contracts;
using Application.Pipelines.Common;
using Application.Pipelines.MatchResult.Contracts;
using Application.Pipelines.MatchResult.Strategies;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.MatchResult.Steps
{
    /// <summary>
    /// Step 2: Handles standing progression after match completion.
    /// Uses strategy pattern to handle Group vs Bracket logic differently.
    /// Strategy determines if standing/tournament is finished and controls pipeline flow.
    /// </summary>
    public class HandleStandingProgressStep : PipeLineBase<HandleStandingProgressStep>
    {
        private readonly IRepository<Standing> _standingRepository;
        private readonly IEnumerable<IStandingProgressStrategy> _strategies;

        public HandleStandingProgressStep(
            IRepository<Standing> standingRepository,
            ILogger<HandleStandingProgressStep> logger,
            IEnumerable<IStandingProgressStrategy> strategies) : base(logger)
        {
            _standingRepository = standingRepository;
            _strategies = strategies;
        }

        protected override async Task<bool> ExecuteStepAsync(MatchResultContext context)
        {
            var tournamentId = context.Tournament.Id;
            var standingId = context.StandingId;
            var matchId = context.MatchId;
            var winnerId = context.WinnerId;

            // Get standing to determine type
            var standing = await _standingRepository.GetByIdAsync(standingId)
                ?? throw new InvalidOperationException($"Standing {standingId} not found");

            context.StandingType = standing.StandingType;

            // Select appropriate strategy
            var strategy = _strategies.FirstOrDefault(s => s.StandingType == standing.StandingType)
                ?? throw new InvalidOperationException($"No strategy found for standing type: {standing.StandingType}");

            // Execute strategy to progress the standing
            var result = await strategy.ProgressStandingAsync(tournamentId, standingId, matchId, winnerId);

            Logger.LogInformation("Processed {StandingType} progress for standing {StandingId}: {Message}",
                standing.StandingType, standingId, result.Message);

            // Map result to context
            context.StandingFinished = result.StandingFinished;
            context.RoundFinished = result.RoundFinished;
            context.AllGroupsFinished = result.AllGroupsFinished;
            context.TournamentFinished = result.TournamentFinished;
            context.NewTournamentStatus = result.NewTournamentStatus;
            context.Message = result.Message;

            // Return strategy's decision on whether to continue pipeline
            return result.ShouldContinuePipeline;
        }
    }
}
