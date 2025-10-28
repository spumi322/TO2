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

        [Fact]
        public void Should_AllowTransition_From_Setup_To_SeedingGroups()
        {
            // Act
            var isValid = _stateMachine.IsTransitionValid(
                TournamentStatus.Setup,
                TournamentStatus.SeedingGroups);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Should_AllowTransition_From_SeedingGroups_To_GroupsInProgress()
        {
            // Act
            var isValid = _stateMachine.IsTransitionValid(
                TournamentStatus.SeedingGroups,
                TournamentStatus.GroupsInProgress);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Should_AllowTransition_From_GroupsInProgress_To_GroupsCompleted()
        {
            // Act
            var isValid = _stateMachine.IsTransitionValid(
                TournamentStatus.GroupsInProgress,
                TournamentStatus.GroupsCompleted);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Should_AllowTransition_From_GroupsCompleted_To_SeedingBracket()
        {
            // Act
            var isValid = _stateMachine.IsTransitionValid(
                TournamentStatus.GroupsCompleted,
                TournamentStatus.SeedingBracket);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Should_AllowTransition_From_SeedingBracket_To_BracketInProgress()
        {
            // Act
            var isValid = _stateMachine.IsTransitionValid(
                TournamentStatus.SeedingBracket,
                TournamentStatus.BracketInProgress);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Should_AllowTransition_From_BracketInProgress_To_Finished()
        {
            // Act
            var isValid = _stateMachine.IsTransitionValid(
                TournamentStatus.BracketInProgress,
                TournamentStatus.Finished);

            // Assert
            isValid.Should().BeTrue();
        }

        #endregion

        #region Invalid Transitions

        [Theory]
        [InlineData(TournamentStatus.Setup, TournamentStatus.GroupsInProgress)]
        [InlineData(TournamentStatus.Setup, TournamentStatus.Finished)]
        [InlineData(TournamentStatus.GroupsInProgress, TournamentStatus.Setup)]
        [InlineData(TournamentStatus.GroupsInProgress, TournamentStatus.SeedingGroups)]
        [InlineData(TournamentStatus.Finished, TournamentStatus.Setup)]
        [InlineData(TournamentStatus.Finished, TournamentStatus.BracketInProgress)]
        public void Should_NotAllowInvalidTransitions(TournamentStatus from, TournamentStatus to)
        {
            // Act
            var isValid = _stateMachine.IsTransitionValid(from, to);

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void ValidateTransition_Should_ThrowException_For_InvalidTransition()
        {
            // Act
            Action act = () => _stateMachine.ValidateTransition(
                TournamentStatus.Setup,
                TournamentStatus.Finished);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Invalid state transition*");
        }

        #endregion

        #region Cancelled State

        [Theory]
        [InlineData(TournamentStatus.Setup)]
        [InlineData(TournamentStatus.SeedingGroups)]
        [InlineData(TournamentStatus.GroupsInProgress)]
        [InlineData(TournamentStatus.GroupsCompleted)]
        [InlineData(TournamentStatus.SeedingBracket)]
        [InlineData(TournamentStatus.BracketInProgress)]
        public void Should_AllowTransition_To_Cancelled_From_AnyState(TournamentStatus from)
        {
            // Act
            var isValid = _stateMachine.IsTransitionValid(from, TournamentStatus.Cancelled);

            // Assert
            isValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(TournamentStatus.Setup)]
        [InlineData(TournamentStatus.GroupsInProgress)]
        [InlineData(TournamentStatus.Finished)]
        public void Should_NotAllowTransition_From_Cancelled_To_AnyState(TournamentStatus to)
        {
            // Act
            var isValid = _stateMachine.IsTransitionValid(TournamentStatus.Cancelled, to);

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

        [Fact]
        public void GetAllowedTransitions_Should_ReturnCorrectTransitions_ForSetup()
        {
            // Act
            var transitions = _stateMachine.GetAllowedTransitions(TournamentStatus.Setup);

            // Assert
            transitions.Should().BeEquivalentTo(new[]
            {
                TournamentStatus.SeedingGroups,
                TournamentStatus.Cancelled
            });
        }

        [Fact]
        public void GetAllowedTransitions_Should_ReturnEmptyList_ForFinished()
        {
            // Act
            var transitions = _stateMachine.GetAllowedTransitions(TournamentStatus.Finished);

            // Assert
            transitions.Should().BeEmpty();
        }

        #endregion
    }
}
