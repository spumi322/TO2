using Application.Contracts;
using Application.Pipelines.Common;
using Application.Pipelines.GameResult.Contracts;
using Domain.AggregateRoots;
using Domain.Enums;
using Domain.StateMachine;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.GameResult.Steps
{
    /// <summary>
    /// Step 6: Transitions tournament state when all groups finish or tournament finishes.
    /// Only executes if a state transition is needed (indicated by context flags).
    /// </summary>
    public class TransitionTournamentStateStep : PipeLineBase<TransitionTournamentStateStep>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Tournament> _tournamentRepository;
        private readonly ITournamentStateMachine _stateMachine;

        public TransitionTournamentStateStep(
            ILogger<TransitionTournamentStateStep> logger,
            IGenericRepository<Tournament> tournamentRepository,
            IUnitOfWork unitOfWork,
            ITournamentStateMachine stateMachine) : base(logger)
        {
            _tournamentRepository = tournamentRepository;
            _stateMachine = stateMachine;
            _unitOfWork = unitOfWork;
        }

        protected override async Task<bool> ExecuteStepAsync(GameResultContext context)
        {
            // Only execute if a state transition is needed
            if (!context.AllGroupsFinished && !context.TournamentFinished)
            {
                return true; // Skip this step
            }

            var tournament = context.Tournament;

            if (context.AllGroupsFinished)
            {
                // Transition to GroupsCompleted
                _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.GroupsCompleted);
                tournament.Status = TournamentStatus.GroupsCompleted;
                context.NewTournamentStatus = TournamentStatus.GroupsCompleted;

                Logger.LogInformation("Tournament {TournamentId} transitioned to GroupsCompleted",
                    tournament.Id);
            }
            else if (context.TournamentFinished)
            {
                // Transition to Finished
                _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.Finished);
                tournament.Status = TournamentStatus.Finished;
                context.NewTournamentStatus = TournamentStatus.Finished;

                Logger.LogInformation("Tournament {TournamentName} FINISHED", tournament.Name);
            }

            // Save tournament state
            await _tournamentRepository.Update(tournament);

            // Continue to next step
            return true;
        }
    }
}
