using Application.Contracts;
using Application.Pipelines.StartBracket.Contracts;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartBracket.Steps
{
    /// <summary>
    /// Step 3: Gets teams advancing from groups to bracket.
    /// Calls StandingService.GetTeamsForBracket which also updates GroupEntry statuses.
    /// </summary>
    public class GetAdvancedTeamsStep : IStartBracketPipelineStep
    {
        private readonly ILogger<GetAdvancedTeamsStep> _logger;
        private readonly IStandingService _standingService;

        public GetAdvancedTeamsStep(
            ILogger<GetAdvancedTeamsStep> logger,
            IStandingService standingService)
        {
            _logger = logger;
            _standingService = standingService;
        }

        public async Task<bool> ExecuteAsync(StartBracketContext context)
        {
            _logger.LogInformation("Step 3: Getting teams advancing to bracket for tournament {TournamentId}",
                context.TournamentId);

            try
            {
                // This call also updates GroupEntry statuses (Advanced/Eliminated)
                var teams = await _standingService.GetTeamsForBracket(context.TournamentId);

                if (teams == null || teams.Count == 0)
                {
                    context.Success = false;
                    context.Message = "No teams available to advance to bracket";
                    _logger.LogWarning("No teams available for bracket in tournament {TournamentId}",
                        context.TournamentId);
                    return false;
                }

                // Store in context
                context.AdvancedTeams = teams;

                _logger.LogInformation("{TeamCount} teams advanced to bracket for tournament {TournamentId}",
                    teams.Count, context.TournamentId);

                return true; // Continue to next step
            }
            catch (Exception ex)
            {
                context.Success = false;
                context.Message = $"Failed to get advancing teams: {ex.Message}";
                _logger.LogError(ex, "Error getting advancing teams for tournament {TournamentId}: {Message}",
                    context.TournamentId, ex.Message);
                return false;
            }
        }
    }
}
