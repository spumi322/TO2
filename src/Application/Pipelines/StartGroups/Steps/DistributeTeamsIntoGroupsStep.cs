using Application.Contracts;
using Application.Pipelines.StartGroups.Contracts;
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

        public DistributeTeamsIntoGroupsStep(
            ILogger<DistributeTeamsIntoGroupsStep> logger,
            IRepository<TournamentTeam> tournamentTeamRepository,
            IRepository<Team> teamRepository)
        {
            _logger = logger;
            _teamRepository = teamRepository;
        }

        public async Task<bool> ExecuteAsync(StartGroupsContext context)
        {
            _logger.LogInformation("Step 3: Distributing teams into groups for tournament {TournamentId}",
                context.TournamentId);

            var teams = (await _teamRepository
                .FindAllAsync(t => t.TournamentParticipations
                .Any(tp => tp.TournamentId == context.TournamentId)))
                .ToList();

            if (teams.Count < context.GroupStandings.Count)
                throw new ConflictException($"Not enough teams ({teams.Count}) for {context.GroupStandings.Count} groups");

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
            var shuffledTeams = teams.OrderBy(t => Guid.NewGuid()).ToList();

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
