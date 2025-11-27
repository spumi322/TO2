using Application.Contracts;
using Application.Pipelines.StartBracket.Contracts;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartBracket.Steps
{
    /// <summary>
    /// Step 3: Gets teams for bracket based on format.
    /// - BracketOnly: All registered teams
    /// - BracketAndGroup: Teams advancing from groups (also updates GroupEntry statuses)
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
            _logger.LogInformation("Step 3: Getting teams for bracket in tournament {TournamentId}",
                context.TournamentId);

            try
            {
                // Format-aware team selection (all teams for BracketOnly, advanced teams for BracketAndGroup)
                var teams = await _standingService.GetTeamsForBracketByFormat(context.TournamentId);

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
