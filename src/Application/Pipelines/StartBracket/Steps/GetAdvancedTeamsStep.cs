using Application.Contracts;
using Application.Pipelines.StartBracket.Contracts;
using Microsoft.Extensions.Logging;
using Domain.Exceptions;

namespace Application.Pipelines.StartBracket.Steps
{
    /// <summary>
    /// Step 3: Gets the teams entering the bracket (unordered; seeding happens in step 5).
    /// - BracketOnly: All registered teams
    /// - GroupsAndBracket: Teams advancing from groups, with group placement (also updates GroupEntry statuses)
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

            // Format-aware team selection (all teams for BracketOnly, advanced teams for GroupsAndBracket)
            var teams = await _standingService.GetTeamsForBracketByFormat(context.TournamentId);

            if (teams == null || teams.Count == 0)
                throw new ConflictException("No teams available to advance to bracket");

            // Store in context
            context.AdvancedTeams = teams;

            _logger.LogInformation("{TeamCount} teams advanced to bracket for tournament {TournamentId}",
                teams.Count, context.TournamentId);

            return true; // Continue to next step
        }
    }
}
