using Application.Contracts;
using Application.Pipelines.Common;
using Application.Pipelines.GameResult.Contracts;
using Application.Pipelines.GameResult.Strategies;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Pipelines.GameResult.Steps
{
    /// <summary>
    /// Step 3: Updates standing statistics (Group or Bracket) using the Strategy pattern.
    /// Selects the appropriate strategy based on standing type.
    /// </summary>
    public class UpdateStandingStatsStep : PipeLineBase<UpdateStandingStatsStep>
    {
        private readonly IGenericRepository<Standing> _standingRepository;
        private readonly IEnumerable<IStandingStatsStrategy> _strategies;

        public UpdateStandingStatsStep(
            ILogger<UpdateStandingStatsStep> logger,
            IGenericRepository<Standing> standingRepository,
            IEnumerable<IStandingStatsStrategy> strategies) : base(logger)
        {
            _standingRepository = standingRepository;
            _strategies = strategies;
        }

        protected override async Task<bool> ExecuteStepAsync(GameResultContext context)
        {
            var standingId = context.GameResult.StandingId;
            var winnerId = context.MatchWinnerId!.Value;
            var loserId = context.MatchLoserId!.Value;

            // Get standing type
            var standing = await _standingRepository.Get(standingId)
                ?? throw new Exception($"Standing {standingId} not found");

            context.StandingType = standing.StandingType;

            // Select appropriate strategy
            var strategy = _strategies.FirstOrDefault(s => s.StandingType == standing.StandingType)
                ?? throw new Exception($"No strategy found for standing type: {standing.StandingType}");

            // Execute strategy
            await strategy.UpdateStatsAsync(standingId, winnerId, loserId);

            Logger.LogInformation("Updated {StandingType} stats for standing {StandingId}",
                standing.StandingType, standingId);

            // Always continue
            return true;
        }
    }
}
