using Application.Contracts;
using Application.Pipelines.StartGroups.Contracts;
using Application.Pipelines.StartGroups.Steps;
using Domain.AggregateRoots;
using FluentAssertions;
using Moq;
using Match = Domain.AggregateRoots.Match;
using Tests.Base;
using Tests.Helpers;

namespace Tests.Unit.Pipelines.StartGroups
{
    public class GenerateRoundRobinMatchesStepTests : UnitTestBase
    {
        [Theory]
        [InlineData(2, 1)]
        [InlineData(3, 3)]
        [InlineData(4, 3)]
        [InlineData(5, 5)]
        [InlineData(8, 7)]
        public void BuildSchedule_Should_PlayEveryPairOnce_AndEachTeamAtMostOncePerRound(int teamCount, int expectedRounds)
        {
            var teams = TestDataBuilder.Teams.CreateMultiple(teamCount);

            var schedule = GenerateRoundRobinMatchesStep.BuildSchedule(teams);

            schedule.Should().HaveCount(teamCount * (teamCount - 1) / 2);
            schedule.Select(m => m.Round).Distinct().Should().HaveCount(expectedRounds);

            // Every pair exactly once
            schedule.Select(m => PairKey(m.TeamA, m.TeamB)).Should().OnlyHaveUniqueItems();

            foreach (var round in schedule.GroupBy(m => m.Round))
            {
                var teamsInRound = round.SelectMany(m => new[] { m.TeamA, m.TeamB }).ToList();
                teamsInRound.Should().OnlyHaveUniqueItems($"round {round.Key} must not have a team twice");
                round.Select(m => m.Seed).Should().Equal(Enumerable.Range(1, round.Count()));
            }
        }

        [Fact]
        public void BuildSchedule_OddGroup_Should_GiveEachTeamOneRoundOff()
        {
            var teams = TestDataBuilder.Teams.CreateMultiple(3);

            var schedule = GenerateRoundRobinMatchesStep.BuildSchedule(teams);

            // 3 rounds, one match each, every team sits out exactly one round
            schedule.GroupBy(m => m.Round).Should().AllSatisfy(r => r.Should().HaveCount(1));
            foreach (var team in teams)
                schedule.Count(m => m.TeamA == team || m.TeamB == team).Should().Be(2);
        }

        [Fact]
        public async Task ExecuteAsync_Should_GenerateMatchesWithScheduleRounds()
        {
            var (_, logger) = CreateLogger<GenerateRoundRobinMatchesStep>();
            var matchService = new Mock<IMatchService>();
            var gameService = new Mock<IGameService>();
            var created = new List<(Team A, Team B, int Round, int Seed)>();
            matchService
                .Setup(x => x.GenerateMatch(It.IsAny<Team?>(), It.IsAny<Team?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<long>()))
                .Callback<Team?, Team?, int, int, long>((a, b, round, seed, _) => created.Add((a!, b!, round, seed)))
                .ReturnsAsync(new Match());
            var step = new GenerateRoundRobinMatchesStep(logger, matchService.Object, gameService.Object);

            var teams = TestDataBuilder.Teams.CreateMultiple(4);
            var context = new StartGroupsContext
            {
                TournamentId = 1,
                GroupAssignments = new() { [TestDataBuilder.Standings.CreateGroup(1, "Group A", 4)] = teams }
            };

            var result = await step.ExecuteAsync(context);

            result.Should().BeTrue();
            created.Should().HaveCount(6);
            created.GroupBy(m => m.Round).Should().HaveCount(3)
                .And.AllSatisfy(r => r.Should().HaveCount(2));
            gameService.Verify(x => x.GenerateGames(It.IsAny<Match>()), Times.Exactly(6));
        }

        private static string PairKey(Team a, Team b)
        {
            var names = new[] { a.Name, b.Name }.OrderBy(n => n);
            return string.Join("|", names);
        }
    }
}
