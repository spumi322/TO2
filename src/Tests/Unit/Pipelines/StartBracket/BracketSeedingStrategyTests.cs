using Application.DTOs.Standing;
using Application.Pipelines.StartBracket.Strategies;
using Domain.AggregateRoots;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Base;

namespace Tests.Unit.Pipelines.StartBracket
{
    public class BracketSeedingStrategyTests : UnitTestBase
    {
        private readonly RandomSeedingStrategy _random;
        private readonly GroupResultSeedingStrategy _groupResult;

        public BracketSeedingStrategyTests()
        {
            _random = new RandomSeedingStrategy(CreateLogger<RandomSeedingStrategy>().Logger);
            _groupResult = new GroupResultSeedingStrategy(CreateLogger<GroupResultSeedingStrategy>().Logger);
        }

        [Fact]
        public void Strategies_HandleTheirFormat()
        {
            _random.Format.Should().Be(Format.BracketOnly);
            _groupResult.Format.Should().Be(Format.GroupsAndBracket);
        }

        // ── RandomSeedingStrategy ────────────────────────────────────────────

        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(8)]
        public void Random_KeepsEveryTeam(int teamCount)
        {
            var entrants = Enumerable.Range(1, teamCount)
                .Select(i => new BracketEntrant(new Team($"Team {i}") { Id = i }, null, null))
                .ToList();

            var pairs = _random.CreateFirstRoundPairs(entrants);

            PairedTeamIds(pairs).Should().BeEquivalentTo(entrants.Select(e => e.Team.Id));
        }

        // ── GroupResultSeedingStrategy ───────────────────────────────────────

        [Fact]
        public void GroupResult_2Groups_WinnersFaceOtherGroupRunnersUp()
        {
            var pairs = _groupResult.CreateFirstRoundPairs(MakeGroupEntrants(groupCount: 2, advancingPerGroup: 2));

            PairNames(pairs).Should().Equal("A1 vs B2", "B1 vs A2");
        }

        [Fact]
        public void GroupResult_3Groups_WinnersGetByes()
        {
            // 6 teams → 8 slots: the 2 BYEs go to the top seeds (group winners A1, B1)
            var pairs = _groupResult.CreateFirstRoundPairs(MakeGroupEntrants(groupCount: 3, advancingPerGroup: 2));

            pairs.Where(p => p.teamB == null).Select(p => p.teamA!.Name)
                .Should().BeEquivalentTo("A1", "B1");
        }

        [Fact]
        public void GroupResult_3Groups_SeparatesSameGroupMatch()
        {
            // Plain seeding would pair C1 vs C2 (seed 3 vs 6)
            var pairs = _groupResult.CreateFirstRoundPairs(MakeGroupEntrants(groupCount: 3, advancingPerGroup: 2));

            PairNames(pairs).Should().Equal("A1 vs BYE", "C1 vs B2", "B1 vs BYE", "A2 vs C2");
        }

        [Theory]
        [InlineData(2, 2)] // 4 teams, no BYEs
        [InlineData(3, 2)] // 6 teams → 8 slots, 2 BYEs
        [InlineData(4, 2)] // 8 teams, no BYEs
        [InlineData(3, 3)] // 9 teams → 16 slots, 7 BYEs
        [InlineData(5, 3)] // 15 teams → 16 slots, 1 BYE
        [InlineData(7, 2)] // 14 teams → 16 slots, 2 BYEs
        [InlineData(3, 5)] // 15 teams → 16 slots, 1 BYE
        public void GroupResult_NoWinnersOrSameGroupTeamsMeetInFirstRound(int groupCount, int advancingPerGroup)
        {
            var pairs = _groupResult.CreateFirstRoundPairs(MakeGroupEntrants(groupCount, advancingPerGroup));

            PairedTeamIds(pairs).Should().HaveCount(groupCount * advancingPerGroup);
            pairs.Should().NotContain(p => IsWinner(p.teamA) && IsWinner(p.teamB));
            pairs.Should().NotContain(p => p.teamA != null && p.teamB != null && p.teamA.Name[0] == p.teamB.Name[0]);
        }

        [Fact]
        public void GroupResult_SingleGroup_KeepsPairsAndLogsWarning()
        {
            var (loggerMock, logger) = CreateLogger<GroupResultSeedingStrategy>();
            var strategy = new GroupResultSeedingStrategy(logger);

            var pairs = strategy.CreateFirstRoundPairs(MakeGroupEntrants(groupCount: 1, advancingPerGroup: 4));

            PairNames(pairs).Should().Equal("A1 vs A4", "A2 vs A3");
            loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Entrants named by group letter + placement (A1 = winner of group A), passed in worst-first order.
        /// </summary>
        private static List<BracketEntrant> MakeGroupEntrants(int groupCount, int advancingPerGroup)
        {
            var entrants = new List<BracketEntrant>();
            long id = 1;
            for (int g = 0; g < groupCount; g++)
                for (int place = 1; place <= advancingPerGroup; place++)
                    entrants.Add(new BracketEntrant(new Team($"{(char)('A' + g)}{place}") { Id = id++ }, g + 1, place));

            entrants.Reverse();
            return entrants;
        }

        private static bool IsWinner(Team? team) => team != null && team.Name.EndsWith("1");

        private static IEnumerable<long> PairedTeamIds(List<(Team? teamA, Team? teamB)> pairs) =>
            pairs.SelectMany(p => new[] { p.teamA, p.teamB }).Where(t => t != null).Select(t => t!.Id);

        private static IEnumerable<string> PairNames(List<(Team? teamA, Team? teamB)> pairs) =>
            pairs.Select(p => $"{p.teamA?.Name ?? "BYE"} vs {p.teamB?.Name ?? "BYE"}");
    }
}
