using Application.Contracts;
using Application.Pipelines.StartBracket.Contracts;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartBracket.Steps
{
    /// <summary>
    /// Step 7: Marks bracket standing as seeded.
    /// </summary>
    public class MarkBracketAsSeededStep : IStartBracketPipelineStep
    {
        private readonly ILogger<MarkBracketAsSeededStep> _logger;
        private readonly IRepository<Standing> _standingRepository;

        public MarkBracketAsSeededStep(
            ILogger<MarkBracketAsSeededStep> logger,
            IRepository<Standing> standingRepository)
        {
            _logger = logger;
            _standingRepository = standingRepository;
        }

        public async Task<bool> ExecuteAsync(StartBracketContext context)
        {
            _logger.LogInformation("Step 7: Marking bracket as seeded for tournament {TournamentId}",
                context.TournamentId);

            context.BracketStanding.IsSeeded = true;

            _logger.LogInformation("Marked bracket '{BracketName}' as seeded for tournament {TournamentId}",
                context.BracketStanding.Name, context.TournamentId);

            return true; // Continue to next step
        }
    }
}
