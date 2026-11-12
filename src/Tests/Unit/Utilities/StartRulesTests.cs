using Application.Utilities;
using FluentAssertions;

namespace Tests.Unit.Utilities
{
    public class StartRulesTests
    {
        [Theory]
        [InlineData(0, 2)]  // bracket only: one final
        [InlineData(1, 2)]
        [InlineData(4, 8)]
        [InlineData(10, 20)]
        public void MinTeamsToStart_Is_TwoPerGroup_OrTwoForABracket(int groupCount, int expected)
        {
            StartRules.MinTeamsToStart(groupCount).Should().Be(expected);
        }

        [Fact]
        public void NotEnoughTeamsMessage_Says_WhatIsNeeded()
        {
            StartRules.NotEnoughTeamsMessage(1, 1).Should().Be("1 group needs at least 2 teams (2 per group); 1 registered.");
            StartRules.NotEnoughTeamsMessage(0, 1).Should().Be("At least 2 teams are needed to start; 1 registered.");
        }
    }
}
