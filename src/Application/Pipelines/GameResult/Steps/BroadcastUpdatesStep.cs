using Application.Contracts;
using Application.Pipelines.Common;
using Application.Pipelines.GameResult.Contracts;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.GameResult.Steps
{
    /// <summary>
    /// Step 8: Broadcasts SignalR updates to connected clients based on pipeline results.
    /// This step executes after BuildResponseStep to notify users of game scoring events.
    /// </summary>
    public class BroadcastUpdatesStep : PipeLineBase<BroadcastUpdatesStep>
    {
        private readonly ISignalRService _signalRService;
        private readonly ITenantService _tenantService;

        public BroadcastUpdatesStep(
            ILogger<BroadcastUpdatesStep> logger,
            ISignalRService signalRService,
            ITenantService tenantService) : base(logger)
        {
            _signalRService = signalRService;
            _tenantService = tenantService;
        }

        protected override async Task<bool> ExecuteStepAsync(GameResultContext context)
        {
            var updatedBy = _tenantService.GetCurrentUserName();
            var tournamentId = context.GameResult.TournamentId;

            // Always broadcast game update
            await _signalRService.BroadcastGameUpdated(
                tournamentId,
                context.GameResult.gameId,
                updatedBy);

            Logger.LogInformation("Broadcasted GameUpdated for GameId: {GameId}", context.GameResult.gameId);

            // Broadcast match update if match is finished
            if (context.MatchFinished)
            {
                await _signalRService.BroadcastMatchUpdated(
                    tournamentId,
                    context.GameResult.MatchId,
                    updatedBy);

                Logger.LogInformation("Broadcasted MatchUpdated for MatchId: {MatchId}", context.GameResult.MatchId);
            }

            // Broadcast standing update if standing is finished
            if (context.StandingFinished)
            {
                await _signalRService.BroadcastStandingUpdated(
                    tournamentId,
                    context.GameResult.StandingId,
                    updatedBy);

                Logger.LogInformation("Broadcasted StandingUpdated for StandingId: {StandingId}", context.GameResult.StandingId);
            }

            // Broadcast tournament update if all groups finished or tournament finished
            if (context.AllGroupsFinished || context.TournamentFinished)
            {
                await _signalRService.BroadcastTournamentUpdated(
                    tournamentId,
                    updatedBy);

                Logger.LogInformation("Broadcasted TournamentUpdated for TournamentId: {TournamentId}", tournamentId);
            }

            // Always return true to continue to next step (BuildResponseStep)
            return true;
        }
    }
}
