using Application.Contracts;
using Application.Pipelines.StartGroups.Contracts;
using Domain.AggregateRoots;
using Domain.Enums;
using Domain.StateMachine;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartGroups.Steps
{
    /// <summary>
    /// Step 7: Transitions tournament to GroupsInProgress status.
    /// </summary>
    public class TransitionToGroupsInProgressStep : IStartGroupsPipelineStep
    {
        private readonly ILogger<TransitionToGroupsInProgressStep> _logger;
        private readonly IRepository<Tournament> _tournamentRepository;
        private readonly ITournamentStateMachine _stateMachine;

        public TransitionToGroupsInProgressStep(
            ILogger<TransitionToGroupsInProgressStep> logger,
            IRepository<Tournament> tournamentRepository,
            ITournamentStateMachine stateMachine)
        {
            _logger = logger;
            _tournamentRepository = tournamentRepository;
            _stateMachine = stateMachine;
        }

        public async Task<bool> ExecuteAsync(StartGroupsContext context)
        {
            _logger.LogInformation("Step 7: Transitioning tournament {TournamentId} to GroupsInProgress",
                context.TournamentId);

            // Step 1 already moved to SeedingGroups, so an invalid transition here is a bug (500)
            _stateMachine.ValidateTransition(context.Tournament.Status, TournamentStatus.GroupsInProgress, context.Tournament.Format);
            context.Tournament.Status = TournamentStatus.GroupsInProgress;

            // Store in context
            context.NewStatus = TournamentStatus.GroupsInProgress;

            _logger.LogInformation("Tournament {TournamentId} transitioned to GroupsInProgress successfully",
                context.TournamentId);

            return true; // Continue to next step
        }
    }
}
