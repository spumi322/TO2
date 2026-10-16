using Application.Contracts;
using Application.Pipelines.StartGroups.Contracts;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartGroups.Steps
{
    /// <summary>
    /// Step 5: Generates round-robin matches and games for each group.
    /// Each team plays every other team once.
    /// </summary>
    public class GenerateRoundRobinMatchesStep : IStartGroupsPipelineStep
    {
        private readonly ILogger<GenerateRoundRobinMatchesStep> _logger;
        private readonly IMatchService _matchService;
        private readonly IGameService _gameService;

        public GenerateRoundRobinMatchesStep(
            ILogger<GenerateRoundRobinMatchesStep> logger,
            IMatchService matchService,
            IGameService gameService)
        {
            _logger = logger;
            _matchService = matchService;
            _gameService = gameService;
        }

        public async Task<bool> ExecuteAsync(StartGroupsContext context)
        {
            _logger.LogInformation("Step 5: Generating round-robin matches for tournament {TournamentId}",
                context.TournamentId);

            int totalMatchesGenerated = 0;

            foreach (var (standing, teamsInGroup) in context.GroupAssignments)
            {
                _logger.LogInformation("Generating round-robin matches for {StandingName} with {TeamCount} teams",
                    standing.Name, teamsInGroup.Count);

                for (int i = 0; i < teamsInGroup.Count; i++)
                {
                    for (int j = i + 1; j < teamsInGroup.Count; j++)
                    {
                        var match = await _matchService.GenerateMatch(
                            teamsInGroup[i],
                            teamsInGroup[j],
                            round: i + 1,
                            seed: j,
                            standingId: standing.Id
                        );

                        await _gameService.GenerateGames(match);

                        totalMatchesGenerated++;
                        _logger.LogInformation("Generated match: {TeamA} vs {TeamB}",
                            teamsInGroup[i].Name, teamsInGroup[j].Name);
                    }
                }
            }

            _logger.LogInformation("Generated {MatchCount} matches successfully for tournament {TournamentId}",
                totalMatchesGenerated, context.TournamentId);

            return true; // Continue to next step
        }
    }
}
