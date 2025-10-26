using Domain.Enums;

namespace Domain.StateMachine
{
    /// <summary>
    /// Domain service interface for tournament state machine validation and transitions.
    /// Implemented in Domain layer, injected via Application layer.
    /// </summary>
    public interface ITournamentStateMachine
    {
        /// <summary>
        /// Checks if a state transition is valid according to the state machine rules.
        /// </summary>
        bool IsTransitionValid(TournamentStatus currentState, TournamentStatus nextState);

        /// <summary>
        /// Validates a state transition and throws InvalidOperationException if invalid.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when transition is not allowed</exception>
        void ValidateTransition(TournamentStatus currentState, TournamentStatus nextState);

        /// <summary>
        /// Gets all valid transition states from the current state.
        /// </summary>
        IEnumerable<TournamentStatus> GetAllowedTransitions(TournamentStatus currentState);

        /// <summary>
        /// Checks if the status is a transition state (SeedingGroups, SeedingBracket).
        /// </summary>
        bool IsTransitionState(TournamentStatus status);

        /// <summary>
        /// Checks if the status is an active state (GroupsInProgress, BracketInProgress).
        /// </summary>
        bool IsActiveState(TournamentStatus status);

        /// <summary>
        /// Checks if the status is a terminal state (Finished, Cancelled).
        /// </summary>
        bool IsTerminalState(TournamentStatus status);

        /// <summary>
        /// Checks if matches can be scored in the given state.
        /// </summary>
        bool CanScoreMatches(TournamentStatus status);

        /// <summary>
        /// Checks if teams can be added/removed in the given state.
        /// </summary>
        bool CanModifyTeams(TournamentStatus status);
    }
}
