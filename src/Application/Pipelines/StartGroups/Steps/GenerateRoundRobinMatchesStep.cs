using Application.Contracts;
using Application.Pipelines.StartGroups.Contracts;
using Domain.AggregateRoots;
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

                foreach (var (round, seed, teamA, teamB) in BuildSchedule(teamsInGroup))
                {
                    var match = await _matchService.GenerateMatch(
                        teamA,
                        teamB,
                        round: round,
                        seed: seed,
                        standingId: standing.Id
                    );

                    await _gameService.GenerateGames(match);

                    totalMatchesGenerated++;
                    _logger.LogInformation("Generated match: {TeamA} vs {TeamB} (round {Round})",
                        teamA.Name, teamB.Name, round);
                }
            }

            _logger.LogInformation("Generated {MatchCount} matches successfully for tournament {TournamentId}",
                totalMatchesGenerated, context.TournamentId);

            return true; // Continue to next step
        }

        /// <summary>
        /// Circle method: every team plays at most once per round.
        /// n teams → n - 1 rounds (even n) or n rounds with one team sitting out each round (odd n).
        /// Seed = match number within the round.
        /// </summary>
        public static List<(int Round, int Seed, Team TeamA, Team TeamB)> BuildSchedule(IReadOnlyList<Team> teams)
        {
            var slots = teams.Cast<Team?>().ToList();
            if (slots.Count % 2 == 1)
                slots.Add(null); // the team paired with this slot sits out the round

            var schedule = new List<(int, int, Team, Team)>();
            int slotCount = slots.Count;

            for (int round = 1; round < slotCount; round++)
            {
                int seed = 0;
                for (int k = 0; k < slotCount / 2; k++)
                {
                    var teamA = slots[k];
                    var teamB = slots[slotCount - 1 - k];
                    if (teamA != null && teamB != null)
                        schedule.Add((round, ++seed, teamA, teamB));
                }

                // Keep the first slot fixed, rotate the others by one
                var last = slots[^1];
                slots.RemoveAt(slotCount - 1);
                slots.Insert(1, last);
            }

            return schedule;
        }
    }
}
