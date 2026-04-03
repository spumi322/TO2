using Application.Contracts;
using Application.DTOs.Game;
using Application.Pipelines.StartBracket.Contracts;
using Application.Pipelines.StartBracket.Steps;
using Application.Pipelines.StartBracket.Utilities;
using Domain.AggregateRoots;
using Domain.Entities;
using DomainMatch = Domain.AggregateRoots.Match;
using Domain.Enums;
using FluentAssertions;
using Moq;
using Tests.Base;
using Tests.Helpers;

namespace Tests.Unit.Pipelines.StartBracket
{
    // ── ValidateTeamCountStep ────────────────────────────────────────────────

    public class ValidateTeamCountStepTests : UnitTestBase
    {
        private readonly ValidateTeamCountStep _step;

        public ValidateTeamCountStepTests()
        {
            var (_, logger) = CreateLogger<ValidateTeamCountStep>();
            _step = new ValidateTeamCountStep(logger);
        }

        [Fact]
        public async Task ExecuteAsync_1Team_ReturnsFalse()
        {
            var context = MakeContext(1);
            var result = await _step.ExecuteAsync(context);

            result.Should().BeFalse();
            context.Success.Should().BeFalse();
            context.Message.Should().Contain("2");
        }

        [Fact]
        public async Task ExecuteAsync_2Teams_ReturnsTrue()
        {
            var result = await _step.ExecuteAsync(MakeContext(2));
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public async Task ExecuteAsync_NonPowerOfTwo_NowAllowed(int count)
        {
            var result = await _step.ExecuteAsync(MakeContext(count));
            result.Should().BeTrue("non-power-of-2 teams are padded with BYEs");
        }

        [Theory]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        public async Task ExecuteAsync_PowerOfTwo_ReturnsTrue(int count)
        {
            var result = await _step.ExecuteAsync(MakeContext(count));
            result.Should().BeTrue();
        }

        private static StartBracketContext MakeContext(int teamCount) => new()
        {
            TournamentId = 1,
            AdvancedTeams = Enumerable.Range(1, teamCount)
                .Select(i => new Team($"Team {i}") { Id = i })
                .ToList()
        };
    }

    // ── CalculateBracketStructureStep ────────────────────────────────────────

    public class CalculateBracketStructureStepTests : UnitTestBase
    {
        private readonly CalculateBracketStructureStep _step;

        public CalculateBracketStructureStepTests()
        {
            var (_, logger) = CreateLogger<CalculateBracketStructureStep>();
            _step = new CalculateBracketStructureStep(logger);
        }

        [Fact]
        public async Task ExecuteAsync_5Teams_PadsTo8AndProduces3Rounds()
        {
            var context = MakeContext(5);
            var result = await _step.ExecuteAsync(context);

            result.Should().BeTrue();
            context.TotalRounds.Should().Be(3);
            context.SeededPairs.Should().HaveCount(4);
        }

        [Fact]
        public async Task ExecuteAsync_5Teams_Produces3ByePairs()
        {
            var context = MakeContext(5);
            await _step.ExecuteAsync(context);

            context.SeededPairs.Count(p => p.teamA == null || p.teamB == null).Should().Be(3);
        }

        [Fact]
        public async Task ExecuteAsync_8Teams_NoByes()
        {
            var context = MakeContext(8);
            await _step.ExecuteAsync(context);

            context.TotalRounds.Should().Be(3);
            context.SeededPairs.Should().HaveCount(4);
            context.SeededPairs.Should().AllSatisfy(p =>
            {
                p.teamA.Should().NotBeNull();
                p.teamB.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ExecuteAsync_6Teams_PadsTo8With2Byes()
        {
            var context = MakeContext(6);
            await _step.ExecuteAsync(context);

            context.TotalRounds.Should().Be(3);
            context.SeededPairs.Count(p => p.teamA == null || p.teamB == null).Should().Be(2);
        }

        [Fact]
        public async Task ExecuteAsync_2Teams_PadsTo2AndProduces1Round()
        {
            var context = MakeContext(2);
            await _step.ExecuteAsync(context);

            context.TotalRounds.Should().Be(1);
            context.SeededPairs.Should().HaveCount(1);
        }

        private static StartBracketContext MakeContext(int teamCount) => new()
        {
            TournamentId = 1,
            BracketStanding = TestDataBuilder.Standings.CreateBracket(1),
            AdvancedTeams = Enumerable.Range(1, teamCount)
                .Select(i => new Team($"Team {i}") { Id = i })
                .ToList()
        };
    }

    // ── GenerateBracketMatchesStep (BYE cases) ───────────────────────────────

    public class GenerateBracketMatchesStepByeTests : UnitTestBase
    {
        private readonly Mock<IMatchService> _matchService;
        private readonly Mock<IGameService> _gameService;
        private readonly GenerateBracketMatchesStep _step;

        public GenerateBracketMatchesStepByeTests()
        {
            var (_, logger) = CreateLogger<GenerateBracketMatchesStep>();
            _matchService = new Mock<IMatchService>();
            _gameService = new Mock<IGameService>();
            _step = new GenerateBracketMatchesStep(logger, _matchService.Object, _gameService.Object);

            _gameService
                .Setup(x => x.GenerateGames(It.IsAny<DomainMatch>()))
                .ReturnsAsync(new GenerateGamesDTO(true, null));
        }

        [Fact]
        public async Task ExecuteAsync_ByePair_SetsWinnerIdOnMatch()
        {
            var realTeam = new Team("Team 1") { Id = 1 };
            var byeMatch = new DomainMatch { StandingId = 1, BestOf = BestOf.Bo3 };

            _matchService
                .Setup(x => x.GenerateMatch(realTeam, null, 1, 1, It.IsAny<long>()))
                .ReturnsAsync(byeMatch);

            var context = MakeContext(new List<(Team?, Team?)> { (realTeam, null) }, totalRounds: 1);
            await _step.ExecuteAsync(context);

            byeMatch.WinnerId.Should().Be(realTeam.Id);
        }

        [Fact]
        public async Task ExecuteAsync_ByePair_DoesNotGenerateGames()
        {
            var realTeam = new Team("Team 1") { Id = 1 };
            var byeMatch = new DomainMatch { StandingId = 1, BestOf = BestOf.Bo3 };

            _matchService
                .Setup(x => x.GenerateMatch(realTeam, null, 1, 1, It.IsAny<long>()))
                .ReturnsAsync(byeMatch);

            var context = MakeContext(new List<(Team?, Team?)> { (realTeam, null) }, totalRounds: 1);
            await _step.ExecuteAsync(context);

            _gameService.Verify(x => x.GenerateGames(It.IsAny<DomainMatch>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_NormalPair_GeneratesGames()
        {
            var teamA = new Team("Team A") { Id = 1 };
            var teamB = new Team("Team B") { Id = 2 };
            var match = new DomainMatch(teamA, teamB, BestOf.Bo3) { StandingId = 1 };

            _matchService
                .Setup(x => x.GenerateMatch(teamA, teamB, 1, 1, It.IsAny<long>()))
                .ReturnsAsync(match);

            var context = MakeContext(new List<(Team?, Team?)> { (teamA, teamB) }, totalRounds: 1);
            await _step.ExecuteAsync(context);

            _gameService.Verify(x => x.GenerateGames(match), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_MixedPairs_OnlyNormalPairGetsGames()
        {
            var realTeam = new Team("Team 1") { Id = 1 };
            var teamA = new Team("Team A") { Id = 2 };
            var teamB = new Team("Team B") { Id = 3 };

            var byeMatch = new DomainMatch { StandingId = 1, BestOf = BestOf.Bo3 };
            var normalMatch = new DomainMatch(teamA, teamB, BestOf.Bo3) { StandingId = 1 };
            var r2Match = new DomainMatch { StandingId = 1, BestOf = BestOf.Bo3 };

            // totalRounds=2 → round 1 has 2 matches (seeds 1,2), round 2 has 1 TBD match
            _matchService.Setup(x => x.GenerateMatch(realTeam, null, 1, 1, It.IsAny<long>())).ReturnsAsync(byeMatch);
            _matchService.Setup(x => x.GenerateMatch(teamA, teamB, 1, 2, It.IsAny<long>())).ReturnsAsync(normalMatch);
            _matchService.Setup(x => x.GenerateMatch(null, null, 2, 1, It.IsAny<long>())).ReturnsAsync(r2Match);

            var pairs = new List<(Team?, Team?)> { (realTeam, null), (teamA, teamB) };
            var context = MakeContext(pairs, totalRounds: 2);
            await _step.ExecuteAsync(context);

            _gameService.Verify(x => x.GenerateGames(normalMatch), Times.Once);
            _gameService.Verify(x => x.GenerateGames(byeMatch), Times.Never);
            byeMatch.WinnerId.Should().Be(realTeam.Id);
        }

        [Fact]
        public async Task ExecuteAsync_ByePair_ReturnsTrue()
        {
            var realTeam = new Team("Team 1") { Id = 1 };
            _matchService
                .Setup(x => x.GenerateMatch(realTeam, null, 1, 1, It.IsAny<long>()))
                .ReturnsAsync(new DomainMatch { StandingId = 1, BestOf = BestOf.Bo3 });

            var context = MakeContext(new List<(Team?, Team?)> { (realTeam, null) }, totalRounds: 1);
            var result = await _step.ExecuteAsync(context);

            result.Should().BeTrue();
            context.Success.Should().BeTrue();
        }

        private static StartBracketContext MakeContext(
            List<(Team? teamA, Team? teamB)> pairs,
            int totalRounds)
        {
            var standing = TestDataBuilder.Standings.CreateBracket(1);
            return new StartBracketContext
            {
                TournamentId = 1,
                BracketStanding = standing,
                SeededPairs = pairs,
                TotalRounds = totalRounds
            };
        }
    }
}
