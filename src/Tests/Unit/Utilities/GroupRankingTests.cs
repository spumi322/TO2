using Application.Utilities;
using Domain.Entities;
using FluentAssertions;

namespace Tests.Unit.Utilities
{
    public class GroupRankingTests
    {
        private static GroupEntry Entry(long teamId, int wins, int gameWins, int gameLosses) =>
            new GroupEntry(1, 1, teamId, $"Team {teamId}") { Wins = wins, GameWins = gameWins, GameLosses = gameLosses };

        [Fact]
        public void Rank_ByWinsFirst()
        {
            // team 1 has the better game difference, but fewer wins
            var ranked = GroupRanking.Rank(new[] { Entry(1, 1, 6, 2), Entry(2, 2, 4, 3) });

            ranked.Select(e => e.TeamId).Should().Equal(2, 1);
        }

        [Fact]
        public void Rank_EqualWins_BetterGameDifferenceWins()
        {
            // both won one match: 2-1 (+1) vs 2-0 (+2)
            var ranked = GroupRanking.Rank(new[] { Entry(1, 1, 2, 1), Entry(2, 1, 2, 0) });

            ranked.Select(e => e.TeamId).Should().Equal(2, 1);
        }

        [Fact]
        public void Rank_FullTie_LowerTeamIdFirst_WhateverTheInputOrder()
        {
            var ranked = GroupRanking.Rank(new[] { Entry(7, 1, 2, 1), Entry(4, 1, 2, 1), Entry(5, 1, 2, 1) });

            ranked.Select(e => e.TeamId).Should().Equal(4, 5, 7);
        }
    }
}
