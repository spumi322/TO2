using Domain.Enums;

namespace Domain.StateMachine
{
    /// <summary>
    /// Domain service that validates and manages tournament state transitions.
    /// Based on explicit state machine pattern with guard clauses.
    /// </summary>
    public class TournamentStateMachine : ITournamentStateMachine
    {
        private readonly Dictionary<TournamentStatus, HashSet<TournamentStatus>> _validTransitions = new()
        {
            { TournamentStatus.Setup, new HashSet<TournamentStatus>
                { TournamentStatus.SeedingGroups, TournamentStatus.Cancelled }
            },
            { TournamentStatus.SeedingGroups, new HashSet<TournamentStatus>
                { TournamentStatus.GroupsInProgress, TournamentStatus.Cancelled }
            },
            { TournamentStatus.GroupsInProgress, new HashSet<TournamentStatus>
                { TournamentStatus.GroupsCompleted, TournamentStatus.Cancelled }
            },
            { TournamentStatus.GroupsCompleted, new HashSet<TournamentStatus>
                { TournamentStatus.SeedingBracket, TournamentStatus.Cancelled }
            },
            { TournamentStatus.SeedingBracket, new HashSet<TournamentStatus>
                { TournamentStatus.BracketInProgress, TournamentStatus.Cancelled }
            },
            { TournamentStatus.BracketInProgress, new HashSet<TournamentStatus>
                { TournamentStatus.Finished, TournamentStatus.Cancelled }
            },
            { TournamentStatus.Finished, new HashSet<TournamentStatus>() },
            { TournamentStatus.Cancelled, new HashSet<TournamentStatus>() },
        };

        private readonly Dictionary<Format, Dictionary<TournamentStatus, HashSet<TournamentStatus>>> _formatSpecificTransitions = new()
        {
            { Format.BracketOnly, new Dictionary<TournamentStatus, HashSet<TournamentStatus>>
                {
                    { TournamentStatus.Setup, new HashSet<TournamentStatus>
                        { TournamentStatus.SeedingBracket, TournamentStatus.Cancelled }
                    },
                    { TournamentStatus.SeedingBracket, new HashSet<TournamentStatus>
                        { TournamentStatus.BracketInProgress, TournamentStatus.Cancelled }
                    },
                    { TournamentStatus.BracketInProgress, new HashSet<TournamentStatus>
                        { TournamentStatus.Finished, TournamentStatus.Cancelled }
                    },
                    { TournamentStatus.Finished, new HashSet<TournamentStatus>() },
                    { TournamentStatus.Cancelled, new HashSet<TournamentStatus>() },
                }
            },
            { Format.GroupsOnly, new Dictionary<TournamentStatus, HashSet<TournamentStatus>>
                {
                    { TournamentStatus.Setup, new HashSet<TournamentStatus>
                        { TournamentStatus.SeedingGroups, TournamentStatus.Cancelled }
                    },
                    { TournamentStatus.SeedingGroups, new HashSet<TournamentStatus>
                        { TournamentStatus.GroupsInProgress, TournamentStatus.Cancelled }
                    },
                    { TournamentStatus.GroupsInProgress, new HashSet<TournamentStatus>
                        { TournamentStatus.Finished, TournamentStatus.Cancelled }
                    },
                    { TournamentStatus.Finished, new HashSet<TournamentStatus>() },
                    { TournamentStatus.Cancelled, new HashSet<TournamentStatus>() },
                }
            },
            { Format.GroupsAndBracket, new Dictionary<TournamentStatus, HashSet<TournamentStatus>>
                {
                    { TournamentStatus.Setup, new HashSet<TournamentStatus>
                        { TournamentStatus.SeedingGroups, TournamentStatus.Cancelled }
                    },
                    { TournamentStatus.SeedingGroups, new HashSet<TournamentStatus>
                        { TournamentStatus.GroupsInProgress, TournamentStatus.Cancelled }
                    },
                    { TournamentStatus.GroupsInProgress, new HashSet<TournamentStatus>
                        { TournamentStatus.GroupsCompleted, TournamentStatus.Cancelled }
                    },
                    { TournamentStatus.GroupsCompleted, new HashSet<TournamentStatus>
                        { TournamentStatus.SeedingBracket, TournamentStatus.Cancelled }
                    },
                    { TournamentStatus.SeedingBracket, new HashSet<TournamentStatus>
                        { TournamentStatus.BracketInProgress, TournamentStatus.Cancelled }
                    },
                    { TournamentStatus.BracketInProgress, new HashSet<TournamentStatus>
                        { TournamentStatus.Finished, TournamentStatus.Cancelled }
                    },
                    { TournamentStatus.Finished, new HashSet<TournamentStatus>() },
                    { TournamentStatus.Cancelled, new HashSet<TournamentStatus>() },
                }
            }
        };

        public bool IsTransitionValid(TournamentStatus currentState, TournamentStatus nextState)
        {
            if (!_validTransitions.ContainsKey(currentState))
                return false;

            return _validTransitions[currentState].Contains(nextState);
        }

        public void ValidateTransition(TournamentStatus currentState, TournamentStatus nextState)
        {
            if (!IsTransitionValid(currentState, nextState))
            {
                throw new InvalidOperationException(
                    $"Invalid state transition: {currentState} -> {nextState}. " +
                    $"Allowed: {string.Join(", ", GetAllowedTransitions(currentState))}"
                );
            }
        }

        public IEnumerable<TournamentStatus> GetAllowedTransitions(TournamentStatus currentState)
        {
            if (!_validTransitions.ContainsKey(currentState))
                return Array.Empty<TournamentStatus>();

            return _validTransitions[currentState];
        }

        public bool IsTransitionValid(TournamentStatus currentState, TournamentStatus nextState, Format format)
        {
            if (!_formatSpecificTransitions.ContainsKey(format))
                return false;

            var transitions = _formatSpecificTransitions[format];
            if (!transitions.ContainsKey(currentState))
                return false;

            return transitions[currentState].Contains(nextState);
        }

        public void ValidateTransition(TournamentStatus currentState, TournamentStatus nextState, Format format)
        {
            if (!IsTransitionValid(currentState, nextState, format))
            {
                throw new InvalidOperationException(
                    $"Invalid state transition for {format} format: {currentState} -> {nextState}. " +
                    $"Allowed: {string.Join(", ", GetAllowedTransitions(currentState, format))}"
                );
            }
        }

        public IEnumerable<TournamentStatus> GetAllowedTransitions(TournamentStatus currentState, Format format)
        {
            if (!_formatSpecificTransitions.ContainsKey(format))
                return Array.Empty<TournamentStatus>();

            var transitions = _formatSpecificTransitions[format];
            if (!transitions.ContainsKey(currentState))
                return Array.Empty<TournamentStatus>();

            return transitions[currentState];
        }

        public bool IsTransitionState(TournamentStatus status) =>
            status == TournamentStatus.SeedingGroups || status == TournamentStatus.SeedingBracket;

        public bool IsActiveState(TournamentStatus status) =>
            status == TournamentStatus.GroupsInProgress || status == TournamentStatus.BracketInProgress;

        public bool IsTerminalState(TournamentStatus status) =>
            status == TournamentStatus.Finished || status == TournamentStatus.Cancelled;

        public bool CanScoreMatches(TournamentStatus status) => IsActiveState(status);

        public bool CanModifyTeams(TournamentStatus status) => status == TournamentStatus.Setup;
    }
}
