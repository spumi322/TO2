using Application.Pipelines.StartBracket.Utilities;
using Domain.AggregateRoots;
using FluentAssertions;

namespace Tests.Unit.Pipelines.StartBracket
{
    public class BracketSeedingUtilityTests
    {
        // ── NextPowerOfTwo ──────────────────────────────────────────────────

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 2)]
        [InlineData(3, 4)]
        [InlineData(4, 4)]
        [InlineData(5, 8)]
        [InlineData(8, 8)]
        [InlineData(9, 16)]
        [InlineData(16, 16)]
        [InlineData(17, 32)]
        public void NextPowerOfTwo_ReturnsCorrectValue(int input, int expected)
        {
            BracketSeedingUtility.NextPowerOfTwo(input).Should().Be(expected);
        }

        // ── PadToPowerOfTwo ─────────────────────────────────────────────────

        [Fact]
        public void PadToPowerOfTwo_ExactPowerOfTwo_NoPaddingAdded()
        {
            var teams = MakeTeams(8);
            var result = BracketSeedingUtility.PadToPowerOfTwo(teams);

            result.Should().HaveCount(8);
            result.Should().NotContainNulls();
        }

        [Fact]
        public void PadToPowerOfTwo_5Teams_PadsTo8With3Nulls()
        {
            var teams = MakeTeams(5);
            var result = BracketSeedingUtility.PadToPowerOfTwo(teams);

            result.Should().HaveCount(8);
            result.Count(t => t == null).Should().Be(3);
            result.Count(t => t != null).Should().Be(5);
        }

        [Fact]
        public void PadToPowerOfTwo_RealTeamsPreservedAtStart()
        {
            var teams = MakeTeams(3);
            var result = BracketSeedingUtility.PadToPowerOfTwo(teams);

            result[0].Should().Be(teams[0]);
            result[1].Should().Be(teams[1]);
            result[2].Should().Be(teams[2]);
            result[3].Should().BeNull();
        }

        // ── CreateSingleEliminationPairs (nullable overload) ────────────────

        [Fact]
        public void CreateSingleEliminationPairs_8Teams_NoByes_Produces4Pairs()
        {
            var padded = BracketSeedingUtility.PadToPowerOfTwo(MakeTeams(8));
            var pairs = BracketSeedingUtility.CreateSingleEliminationPairs(padded);

            pairs.Should().HaveCount(4);
            pairs.Should().AllSatisfy(p =>
            {
                p.teamA.Should().NotBeNull();
                p.teamB.Should().NotBeNull();
            });
        }

        [Fact]
        public void CreateSingleEliminationPairs_5Teams_Produces4PairsAnd3Byes()
        {
            var padded = BracketSeedingUtility.PadToPowerOfTwo(MakeTeams(5));
            var pairs = BracketSeedingUtility.CreateSingleEliminationPairs(padded);

            pairs.Should().HaveCount(4);
            pairs.Count(p => p.teamA == null || p.teamB == null).Should().Be(3);
        }

        [Fact]
        public void CreateSingleEliminationPairs_TopSeedFacesLowestSeed()
        {
            var teams = MakeTeams(8); // ids 1–8 in rank order
            var padded = BracketSeedingUtility.PadToPowerOfTwo(teams);
            var pairs = BracketSeedingUtility.CreateSingleEliminationPairs(padded);

            // Standard seeding: rank 1 vs rank 8 in first pair
            pairs[0].teamA.Should().Be(teams[0]);
            pairs[0].teamB.Should().Be(teams[7]);
        }

        [Fact]
        public void CreateSingleEliminationPairs_2Teams_Produces1Pair()
        {
            var padded = BracketSeedingUtility.PadToPowerOfTwo(MakeTeams(2));
            var pairs = BracketSeedingUtility.CreateSingleEliminationPairs(padded);

            pairs.Should().HaveCount(1);
            pairs[0].teamA.Should().NotBeNull();
            pairs[0].teamB.Should().NotBeNull();
        }

        // ── SeparateSameGroupPairs ──────────────────────────────────────────

        [Fact]
        public void SeparateSameGroupPairs_SwapsLowerSeededTeams()
        {
            var t = MakeTeams(4);
            var pairs = new List<(Team?, Team?)> { (t[0], t[1]), (t[2], t[3]) };
            var groupOf = new Dictionary<long, long> { [1] = 1, [2] = 1, [3] = 2, [4] = 2 };

            var unresolved = BracketSeedingUtility.SeparateSameGroupPairs(pairs, groupOf);

            unresolved.Should().Be(0);
            pairs.Should().Equal((t[0], t[3]), (t[2], t[1]));
        }

        [Fact]
        public void SeparateSameGroupPairs_ByePairsUntouched()
        {
            var t = MakeTeams(3);
            var pairs = new List<(Team?, Team?)> { (t[0], null), (t[1], t[2]) };
            var groupOf = new Dictionary<long, long> { [1] = 1, [2] = 2, [3] = 2 };

            var unresolved = BracketSeedingUtility.SeparateSameGroupPairs(pairs, groupOf);

            // Only one real match: the same-group match cannot be separated
            unresolved.Should().Be(1);
            pairs.Should().Equal((t[0], null), (t[1], t[2]));
        }

        [Fact]
        public void SeparateSameGroupPairs_SingleGroup_ReturnsUnresolvedCount()
        {
            var t = MakeTeams(4);
            var pairs = new List<(Team?, Team?)> { (t[0], t[3]), (t[1], t[2]) };
            var groupOf = t.ToDictionary(team => team.Id, _ => 1L);

            var unresolved = BracketSeedingUtility.SeparateSameGroupPairs(pairs, groupOf);

            unresolved.Should().Be(2);
            pairs.Should().Equal((t[0], t[3]), (t[1], t[2]));
        }

        // ── helpers ─────────────────────────────────────────────────────────

        private static List<Team> MakeTeams(int count) =>
            Enumerable.Range(1, count)
                .Select(i => new Team($"Team {i}") { Id = i })
                .ToList();
    }
}
