using Application.Utilities;
using Domain.AggregateRoots;
using Domain.Entities;
using FluentAssertions;

namespace Tests.Unit.Utilities
{
    public class GroupRankingTests
    {
        private static GroupEntry Entry(long teamId, int wins, int gameWins, int gameLosses) =>
            new GroupEntry(1, 1, teamId, $"Team {teamId}") { Wins = wins, GameWins = gameWins, GameLosses = gameLosses };

        private static readonly Match[] NoMatches = [];

        private static Match Played(long teamA, long teamB, long winner) =>
            new Match { TeamAId = teamA, TeamBId = teamB, WinnerId = winner };

        [Fact]
        public void Rank_ByWinsFirst()
        {
            // team 1 has the better game difference, but fewer wins
            var ranked = GroupRanking.Rank(new[] { Entry(1, 1, 6, 2), Entry(2, 2, 4, 3) }, NoMatches);

            ranked.Select(e => e.TeamId).Should().Equal(2, 1);
        }

        [Fact]
        public void Rank_EqualWins_BetterGameDifferenceWins()
        {
            // both won one match: 2-1 (+1) vs 2-0 (+2)
            var ranked = GroupRanking.Rank(new[] { Entry(1, 1, 2, 1), Entry(2, 1, 2, 0) }, NoMatches);

            ranked.Select(e => e.TeamId).Should().Equal(2, 1);
        }

        [Fact]
        public void Rank_FullTie_LowerTeamIdFirst_WhateverTheInputOrder()
        {
            var ranked = GroupRanking.Rank(new[] { Entry(7, 1, 2, 1), Entry(4, 1, 2, 1), Entry(5, 1, 2, 1) }, NoMatches);

            ranked.Select(e => e.TeamId).Should().Equal(4, 5, 7);
        }

        [Fact]
        public void Rank_TwoTeamsTied_WinnerOfTheirMatchFirst()
        {
            // both 2-1 with +2; team 7 beat team 4, so 7 goes first despite the higher id
            var entries = new[] { Entry(4, 2, 5, 3), Entry(7, 2, 5, 3), Entry(9, 0, 1, 6) };

            var ranked = GroupRanking.Rank(entries, new[] { Played(4, 7, winner: 7), Played(4, 9, winner: 4) });

            ranked.Select(e => e.TeamId).Should().Equal(7, 4, 9);
        }

        [Fact]
        public void Rank_TwoTeamsTied_NotPlayedYet_LowerTeamIdFirst()
        {
            var ranked = GroupRanking.Rank(new[] { Entry(7, 1, 2, 1), Entry(4, 1, 2, 1) }, new[] { new Match { TeamAId = 4, TeamBId = 7 } });

            ranked.Select(e => e.TeamId).Should().Equal(4, 7);
        }

        [Fact]
        public void Rank_ThreeTeamsTied_HeadToHeadNotUsed()
        {
            // a circle (5 beat 4, 7 beat 5, 4 beat 7): no head-to-head order, the fallback decides
            var matches = new[] { Played(4, 5, winner: 5), Played(5, 7, winner: 7), Played(4, 7, winner: 4) };

            var ranked = GroupRanking.Rank(new[] { Entry(7, 1, 2, 2), Entry(5, 1, 2, 2), Entry(4, 1, 2, 2) }, matches);

            ranked.Select(e => e.TeamId).Should().Equal(4, 5, 7);
        }
    }
}
