using Application.Contracts;
using Application.Pipelines.StartGroups.Contracts;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Domain.Exceptions;

namespace Application.Pipelines.StartGroups.Steps
{
    /// <summary>
    /// Step 4: Creates or updates GroupEntry records for each team in their assigned groups.
    /// </summary>
    public class CreateGroupEntriesStep : IStartGroupsPipelineStep
    {
        private readonly ILogger<CreateGroupEntriesStep> _logger;
        private readonly IRepository<GroupEntry> _groupRepository;

        public CreateGroupEntriesStep(
            ILogger<CreateGroupEntriesStep> logger,
            IRepository<GroupEntry> groupRepository)
        {
            _logger = logger;
            _groupRepository = groupRepository;
        }

        public async Task<bool> ExecuteAsync(StartGroupsContext context)
        {
            _logger.LogInformation("Step 4: Creating group entries for tournament {TournamentId}",
                context.TournamentId);

            int entriesCreated = 0;
            int entriesUpdated = 0;

            // Get all existing group entries for this tournament once
            var allGroupEntries = await _groupRepository.FindAllAsync(ge => ge.TournamentId == context.TournamentId);

            foreach (var (standing, teamsInGroup) in context.GroupAssignments)
            {
                foreach (var team in teamsInGroup)
                {
                    // Check if entry already exists
                    var existingEntry = allGroupEntries.FirstOrDefault(ge => ge.TeamId == team.Id);

                    if (existingEntry != null)
                        throw new ConflictException($"Group entry already exists on tournament: {existingEntry.TournamentId}, team: {existingEntry.TeamId}");

                    // Create new entry
                    var groupEntry = new GroupEntry(context.TournamentId, standing.Id, team.Id, team.Name);
                    await _groupRepository.AddAsync(groupEntry);
                    entriesCreated++;
                    _logger.LogInformation("Created GroupEntry for team {TeamName} in {StandingName}",
                        team.Name, standing.Name);
                }
            }

            _logger.LogInformation("Created {Created} and updated {Updated} group entries for tournament {TournamentId}",
                entriesCreated, entriesUpdated, context.TournamentId);

            return true; // Continue to next step
        }
    }
}
