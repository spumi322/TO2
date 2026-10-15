using Domain.Enums;
using Domain.StateMachine;
using FluentAssertions;

namespace Tests.Unit.Domain
{
    public class TournamentStateMachineTests
    {
        private readonly TournamentStateMachine _stateMachine;

        public TournamentStateMachineTests()
        {
            _stateMachine = new TournamentStateMachine();
        }

        #region Valid Transitions

        [Theory]
        [InlineData(Format.BracketOnly, TournamentStatus.Setup, TournamentStatus.SeedingBracket)]
        [InlineData(Format.BracketOnly, TournamentStatus.SeedingBracket, TournamentStatus.BracketInProgress)]
        [InlineData(Format.BracketOnly, TournamentStatus.BracketInProgress, TournamentStatus.Finished)]
        [InlineData(Format.GroupsOnly, TournamentStatus.Setup, TournamentStatus.SeedingGroups)]
        [InlineData(Format.GroupsOnly, TournamentStatus.SeedingGroups, TournamentStatus.GroupsInProgress)]
        [InlineData(Format.GroupsOnly, TournamentStatus.GroupsInProgress, TournamentStatus.Finished)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.Setup, TournamentStatus.SeedingGroups)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.SeedingGroups, TournamentStatus.GroupsInProgress)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.GroupsInProgress, TournamentStatus.GroupsCompleted)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.GroupsCompleted, TournamentStatus.SeedingBracket)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.SeedingBracket, TournamentStatus.BracketInProgress)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.BracketInProgress, TournamentStatus.Finished)]
        public void Should_AllowValidTransitions_PerFormat(Format format, TournamentStatus from, TournamentStatus to)
        {
            // Act
            var isValid = _stateMachine.IsTransitionValid(from, to, format);

            // Assert
            isValid.Should().BeTrue();
        }

        #endregion

        #region Invalid Transitions

        [Theory]
        // Generic invalid moves (skipping or going backwards)
        [InlineData(Format.GroupsAndBracket, TournamentStatus.Setup, TournamentStatus.GroupsInProgress)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.Setup, TournamentStatus.Finished)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.GroupsInProgress, TournamentStatus.Setup)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.GroupsInProgress, TournamentStatus.SeedingGroups)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.Finished, TournamentStatus.Setup)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.Finished, TournamentStatus.BracketInProgress)]
        // Format-specific: path belongs to another format
        [InlineData(Format.BracketOnly, TournamentStatus.Setup, TournamentStatus.SeedingGroups)]
        [InlineData(Format.BracketOnly, TournamentStatus.GroupsInProgress, TournamentStatus.GroupsCompleted)]
        [InlineData(Format.GroupsOnly, TournamentStatus.GroupsInProgress, TournamentStatus.GroupsCompleted)]
        [InlineData(Format.GroupsOnly, TournamentStatus.Setup, TournamentStatus.SeedingBracket)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.Setup, TournamentStatus.SeedingBracket)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.GroupsInProgress, TournamentStatus.Finished)]
        public void Should_NotAllowInvalidTransitions_PerFormat(Format format, TournamentStatus from, TournamentStatus to)
        {
            // Act
            var isValid = _stateMachine.IsTransitionValid(from, to, format);

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void ValidateTransition_Should_ThrowException_For_InvalidTransition()
        {
            // Act
            Action act = () => _stateMachine.ValidateTransition(
                TournamentStatus.Setup,
                TournamentStatus.SeedingGroups,
                Format.BracketOnly);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Invalid state transition for BracketOnly*");
        }

        [Fact]
        public void ValidateTransition_Should_NotThrow_For_ValidTransition()
        {
            // Act
            Action act = () => _stateMachine.ValidateTransition(
                TournamentStatus.GroupsInProgress,
                TournamentStatus.Finished,
                Format.GroupsOnly);

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region Cancelled State

        [Theory]
        [InlineData(Format.BracketOnly, TournamentStatus.Setup)]
        [InlineData(Format.BracketOnly, TournamentStatus.SeedingBracket)]
        [InlineData(Format.BracketOnly, TournamentStatus.BracketInProgress)]
        [InlineData(Format.GroupsOnly, TournamentStatus.Setup)]
        [InlineData(Format.GroupsOnly, TournamentStatus.SeedingGroups)]
        [InlineData(Format.GroupsOnly, TournamentStatus.GroupsInProgress)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.Setup)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.SeedingGroups)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.GroupsInProgress)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.GroupsCompleted)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.SeedingBracket)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.BracketInProgress)]
        public void Should_AllowTransition_To_Cancelled_From_AnyNonTerminalState(Format format, TournamentStatus from)
        {
            // Act
            var isValid = _stateMachine.IsTransitionValid(from, TournamentStatus.Cancelled, format);

            // Assert
            isValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(Format.BracketOnly, TournamentStatus.Setup)]
        [InlineData(Format.GroupsOnly, TournamentStatus.GroupsInProgress)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.Finished)]
        public void Should_NotAllowTransition_From_Cancelled_To_AnyState(Format format, TournamentStatus to)
        {
            // Act
            var isValid = _stateMachine.IsTransitionValid(TournamentStatus.Cancelled, to, format);

            // Assert
            isValid.Should().BeFalse();
        }

        #endregion

        #region Helper Methods - IsTransitionState

        [Theory]
        [InlineData(TournamentStatus.SeedingGroups, true)]
        [InlineData(TournamentStatus.SeedingBracket, true)]
        [InlineData(TournamentStatus.Setup, false)]
        [InlineData(TournamentStatus.GroupsInProgress, false)]
        [InlineData(TournamentStatus.BracketInProgress, false)]
        [InlineData(TournamentStatus.Finished, false)]
        public void IsTransitionState_Should_ReturnCorrectValue(TournamentStatus status, bool expected)
        {
            // Act
            var result = _stateMachine.IsTransitionState(status);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region Helper Methods - IsActiveState

        [Theory]
        [InlineData(TournamentStatus.GroupsInProgress, true)]
        [InlineData(TournamentStatus.BracketInProgress, true)]
        [InlineData(TournamentStatus.Setup, false)]
        [InlineData(TournamentStatus.SeedingGroups, false)]
        [InlineData(TournamentStatus.GroupsCompleted, false)]
        [InlineData(TournamentStatus.Finished, false)]
        public void IsActiveState_Should_ReturnCorrectValue(TournamentStatus status, bool expected)
        {
            // Act
            var result = _stateMachine.IsActiveState(status);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region Helper Methods - IsTerminalState

        [Theory]
        [InlineData(TournamentStatus.Finished, true)]
        [InlineData(TournamentStatus.Cancelled, true)]
        [InlineData(TournamentStatus.Setup, false)]
        [InlineData(TournamentStatus.GroupsInProgress, false)]
        [InlineData(TournamentStatus.BracketInProgress, false)]
        public void IsTerminalState_Should_ReturnCorrectValue(TournamentStatus status, bool expected)
        {
            // Act
            var result = _stateMachine.IsTerminalState(status);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region Helper Methods - CanScoreMatches

        [Theory]
        [InlineData(TournamentStatus.GroupsInProgress, true)]
        [InlineData(TournamentStatus.BracketInProgress, true)]
        [InlineData(TournamentStatus.Setup, false)]
        [InlineData(TournamentStatus.SeedingGroups, false)]
        [InlineData(TournamentStatus.GroupsCompleted, false)]
        [InlineData(TournamentStatus.Finished, false)]
        public void CanScoreMatches_Should_ReturnCorrectValue(TournamentStatus status, bool expected)
        {
            // Act
            var result = _stateMachine.CanScoreMatches(status);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region Helper Methods - CanModifyTeams

        [Theory]
        [InlineData(TournamentStatus.Setup, true)]
        [InlineData(TournamentStatus.SeedingGroups, false)]
        [InlineData(TournamentStatus.GroupsInProgress, false)]
        [InlineData(TournamentStatus.Finished, false)]
        public void CanModifyTeams_Should_ReturnCorrectValue(TournamentStatus status, bool expected)
        {
            // Act
            var result = _stateMachine.CanModifyTeams(status);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region GetAllowedTransitions

        [Theory]
        [InlineData(Format.BracketOnly, TournamentStatus.SeedingBracket)]
        [InlineData(Format.GroupsOnly, TournamentStatus.SeedingGroups)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.SeedingGroups)]
        public void GetAllowedTransitions_Should_ReturnCorrectTransitions_ForSetup(Format format, TournamentStatus expectedNext)
        {
            // Act
            var transitions = _stateMachine.GetAllowedTransitions(TournamentStatus.Setup, format);

            // Assert
            transitions.Should().BeEquivalentTo(new[]
            {
                expectedNext,
                TournamentStatus.Cancelled
            });
        }

        [Theory]
        [InlineData(Format.BracketOnly)]
        [InlineData(Format.GroupsOnly)]
        [InlineData(Format.GroupsAndBracket)]
        public void GetAllowedTransitions_Should_ReturnEmptyList_ForFinished(Format format)
        {
            // Act
            var transitions = _stateMachine.GetAllowedTransitions(TournamentStatus.Finished, format);

            // Assert
            transitions.Should().BeEmpty();
        }

        [Fact]
        public void GetAllowedTransitions_Should_ReturnEmptyList_ForStateNotInFormat()
        {
            // Act
            var transitions = _stateMachine.GetAllowedTransitions(TournamentStatus.GroupsInProgress, Format.BracketOnly);

            // Assert
            transitions.Should().BeEmpty();
        }

        #endregion
    }
}
