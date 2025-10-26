using Application.Contracts;
using Application.Pipelines.StartGroups.Contracts;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartGroups.Steps
{
    /// <summary>
    /// Step 6: Marks all group standings as seeded.
    /// </summary>
    public class MarkStandingsAsSeededStep : IStartGroupsPipelineStep
    {
        private readonly ILogger<MarkStandingsAsSeededStep> _logger;
        private readonly IGenericRepository<Standing> _standingRepository;

        public MarkStandingsAsSeededStep(
            ILogger<MarkStandingsAsSeededStep> logger,
            IGenericRepository<Standing> standingRepository)
        {
            _logger = logger;
            _standingRepository = standingRepository;
        }

        public async Task<bool> ExecuteAsync(StartGroupsContext context)
        {
            _logger.LogInformation("Step 6: Marking standings as seeded for tournament {TournamentId}",
                context.TournamentId);

            foreach (var standing in context.GroupStandings)
            {
                standing.IsSeeded = true;
                await _standingRepository.Update(standing);
                _logger.LogInformation("Marked {StandingName} as seeded", standing.Name);
            }

            _logger.LogInformation("Marked {Count} group standings as seeded for tournament {TournamentId}",
                context.GroupStandings.Count, context.TournamentId);

            return true; // Continue to next step
        }
    }
}
