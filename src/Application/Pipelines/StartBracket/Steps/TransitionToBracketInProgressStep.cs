using Application.Contracts;
using Application.Pipelines.StartBracket.Contracts;
using Domain.AggregateRoots;
using Domain.Enums;
using Domain.StateMachine;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartBracket.Steps
{
    /// <summary>
    /// Step 8: Transitions tournament to BracketInProgress status.
    /// </summary>
    public class TransitionToBracketInProgressStep : IStartBracketPipelineStep
    {
        private readonly ILogger<TransitionToBracketInProgressStep> _logger;
        private readonly ITournamentStateMachine _stateMachine;

        public TransitionToBracketInProgressStep(
            ILogger<TransitionToBracketInProgressStep> logger,
            ITournamentStateMachine stateMachine)
        {
            _logger = logger;
            _stateMachine = stateMachine;
        }

        public async Task<bool> ExecuteAsync(StartBracketContext context)
        {
            _logger.LogInformation("Step 8: Transitioning tournament {TournamentId} to BracketInProgress",
                context.TournamentId);

            // Step 1 already moved to SeedingBracket, so an invalid transition here is a bug (500)
            _stateMachine.ValidateTransition(context.Tournament.Status, TournamentStatus.BracketInProgress, context.Tournament.Format);
            context.Tournament.Status = TournamentStatus.BracketInProgress;

            // Store in context
            context.NewStatus = TournamentStatus.BracketInProgress;

            _logger.LogInformation("Tournament {TournamentId} transitioned to BracketInProgress successfully",
                context.TournamentId);

            return true; // Continue to next step
        }
    }
}
