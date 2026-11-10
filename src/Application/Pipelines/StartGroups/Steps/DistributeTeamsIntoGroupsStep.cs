using Application.Contracts;
using Application.Pipelines.StartGroups.Contracts;
using Application.Utilities;
using Domain.AggregateRoots;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Domain.Exceptions;

namespace Application.Pipelines.StartGroups.Steps
{
    /// <summary>
    /// Step 3: Distributes teams randomly into groups.
    /// Loads teams and creates balanced group assignments.
    /// </summary>
    public class DistributeTeamsIntoGroupsStep : IStartGroupsPipelineStep
    {
        private readonly ILogger<DistributeTeamsIntoGroupsStep> _logger;
        private readonly IRepository<Team> _teamRepository;
        private readonly Random _random;

        public DistributeTeamsIntoGroupsStep(
            ILogger<DistributeTeamsIntoGroupsStep> logger,
            IRepository<Team> teamRepository,
            Random random)
        {
            _logger = logger;
            _teamRepository = teamRepository;
            _random = random;
        }

        public async Task<bool> ExecuteAsync(StartGroupsContext context)
        {
            _logger.LogInformation("Step 3: Distributing teams into groups for tournament {TournamentId}",
                context.TournamentId);

            var teams = (await _teamRepository.FindAllAsync(t => t.TournamentId == context.TournamentId)).ToList();

            // Every group needs a match, else it can never finish
            if (teams.Count < StartRules.MinTeamsToStart(context.GroupStandings.Count))
                throw new ConflictException(StartRules.NotEnoughTeamsMessage(context.GroupStandings.Count, teams.Count));

            // Shuffle teams into equal groups
            var groupAssignments = ShuffleTeamsIntoEqualGroups(teams, context.GroupStandings);

            // Store in context
            context.Teams = teams;
            context.GroupAssignments = groupAssignments;

            _logger.LogInformation("Distributed {TeamCount} teams across {GroupCount} groups for tournament {TournamentId}",
                teams.Count, context.GroupStandings.Count, context.TournamentId);

            return true; // Continue to next step
        }

        private Dictionary<Standing, List<Team>> ShuffleTeamsIntoEqualGroups(List<Team> teams, List<Standing> groupStandings)
        {
            // Shuffle teams randomly
            var shuffled = teams.ToArray();
            _random.Shuffle(shuffled);
            var shuffledTeams = shuffled.ToList();

            int teamsPerGroup = shuffledTeams.Count / groupStandings.Count;
            int remainingTeams = shuffledTeams.Count % groupStandings.Count;
            int teamIndex = 0;

            var assignments = new Dictionary<Standing, List<Team>>();

            for (int i = 0; i < groupStandings.Count; i++)
            {
                int groupSize = teamsPerGroup + (i < remainingTeams ? 1 : 0);
                var teamsForGroup = shuffledTeams.GetRange(teamIndex, groupSize);
                assignments[groupStandings[i]] = teamsForGroup;
                teamIndex += groupSize;

                _logger.LogInformation("Assigned {GroupSize} teams to {StandingName}",
                    groupSize, groupStandings[i].Name);
            }

            return assignments;
        }
    }
}
