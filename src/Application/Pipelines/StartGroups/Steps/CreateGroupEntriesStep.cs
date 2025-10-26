using Application.Contracts;
using Application.Pipelines.StartGroups.Contracts;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartGroups.Steps
{
    /// <summary>
    /// Step 4: Creates or updates GroupEntry records for each team in their assigned groups.
    /// </summary>
    public class CreateGroupEntriesStep : IStartGroupsPipelineStep
    {
        private readonly ILogger<CreateGroupEntriesStep> _logger;
        private readonly IGenericRepository<Group> _groupRepository;

        public CreateGroupEntriesStep(
            ILogger<CreateGroupEntriesStep> logger,
            IGenericRepository<Group> groupRepository)
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
            var allGroupEntries = await _groupRepository.GetAllByFK("TournamentId", context.TournamentId);

            foreach (var (standing, teamsInGroup) in context.GroupAssignments)
            {
                foreach (var team in teamsInGroup)
                {
                    try
                    {
                        // Check if entry already exists
                        var existingEntry = allGroupEntries.FirstOrDefault(ge => ge.TeamId == team.Id);

                        if (existingEntry != null)
                        {
                            // Update existing entry
                            existingEntry.StandingId = standing.Id;
                            existingEntry.Status = TeamStatus.Competing;
                            await _groupRepository.Update(existingEntry);
                            entriesUpdated++;
                            _logger.LogInformation("Updated GroupEntry for team {TeamName} in {StandingName}",
                                team.Name, standing.Name);
                        }
                        else
                        {
                            // Create new entry
                            var groupEntry = new Group(context.TournamentId, standing.Id, team.Id, team.Name);
                            await _groupRepository.Add(groupEntry);
                            entriesCreated++;
                            _logger.LogInformation("Created GroupEntry for team {TeamName} in {StandingName}",
                                team.Name, standing.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        context.Success = false;
                        context.Message = $"Failed to create group entry for team {team.Name}";
                        _logger.LogError(ex, "Failed to create/update GroupEntry for team {TeamId}: {Message}",
                            team.Id, ex.Message);
                        return false;
                    }
                }
            }

            _logger.LogInformation("Created {Created} and updated {Updated} group entries for tournament {TournamentId}",
                entriesCreated, entriesUpdated, context.TournamentId);

            return true; // Continue to next step
        }
    }
}
