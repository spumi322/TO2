using Application.Contracts;
using Application.Pipelines.StartGroups.Contracts;
using Application.Pipelines.StartGroups.Steps;
using Domain.AggregateRoots;
using Domain.Entities;
using FluentAssertions;
using Moq;
using System.Linq.Expressions;
using Tests.Base;
using Tests.Helpers;

namespace Tests.Unit.Pipelines.StartGroups
{
    public class DistributeTeamsIntoGroupsStepTests : UnitTestBase
    {
        private readonly Mock<IRepository<TournamentTeam>> _mockTournamentTeamRepo;
        private readonly Mock<IRepository<Team>> _mockTeamRepo;
        private readonly DistributeTeamsIntoGroupsStep _step;

        public DistributeTeamsIntoGroupsStepTests()
        {
            var (_, logger) = CreateLogger<DistributeTeamsIntoGroupsStep>();
            _mockTournamentTeamRepo = new Mock<IRepository<TournamentTeam>>();
            _mockTeamRepo = new Mock<IRepository<Team>>();
            _step = new DistributeTeamsIntoGroupsStep(
                logger,
                _mockTournamentTeamRepo.Object,
                _mockTeamRepo.Object);
        }

        [Fact]
        public async Task ExecuteAsync_Should_DistributeTeams_EvenlyAcrossGroups()
        {
            // Arrange
            var teams = TestDataBuilder.Teams.CreateMultiple(8);
            var groupStandings = new List<Standing>
            {
                TestDataBuilder.Standings.CreateGroup(1, "Group A", 4),
                TestDataBuilder.Standings.CreateGroup(1, "Group B", 4)
            };

            var context = new StartGroupsContext
            {
                TournamentId = 1,
                GroupStandings = groupStandings
            };

            var tournamentTeams = teams.Select((t, i) =>
                TestDataBuilder.TournamentTeams.Create(1, i + 1)).ToList();

            _mockTournamentTeamRepo
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<TournamentTeam, bool>>>()))
                .ReturnsAsync(tournamentTeams);

            for (int i = 0; i < teams.Count; i++)
            {
                var team = teams[i];
                _mockTeamRepo
                    .Setup(x => x.GetByIdAsync(i + 1))
                    .ReturnsAsync(team);
            }

            // Act
            var result = await _step.ExecuteAsync(context);

            // Assert
            result.Should().BeTrue();
            context.Success.Should().BeTrue();
            context.Teams.Should().HaveCount(8);
            context.GroupAssignments.Should().HaveCount(2);
            context.GroupAssignments.Values.Should().AllSatisfy(teams =>
                teams.Count.Should().Be(4, "teams should be evenly distributed"));
        }

        [Fact]
        public async Task ExecuteAsync_Should_HandleUnevenDistribution()
        {
            // Arrange
            var teams = TestDataBuilder.Teams.CreateMultiple(7);
            var groupStandings = new List<Standing>
            {
                TestDataBuilder.Standings.CreateGroup(1, "Group A", 4),
                TestDataBuilder.Standings.CreateGroup(1, "Group B", 3)
            };

            var context = new StartGroupsContext
            {
                TournamentId = 1,
                GroupStandings = groupStandings
            };

            var tournamentTeams = teams.Select((t, i) =>
                TestDataBuilder.TournamentTeams.Create(1, i + 1)).ToList();

            _mockTournamentTeamRepo
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<TournamentTeam, bool>>>()))
                .ReturnsAsync(tournamentTeams);

            for (int i = 0; i < teams.Count; i++)
            {
                var team = teams[i];
                _mockTeamRepo
                    .Setup(x => x.GetByIdAsync(i + 1))
                    .ReturnsAsync(team);
            }

            // Act
            var result = await _step.ExecuteAsync(context);

            // Assert
            result.Should().BeTrue();
            context.Teams.Should().HaveCount(7);
            context.GroupAssignments.Should().HaveCount(2);
            context.GroupAssignments.Values.Sum(g => g.Count).Should().Be(7, "all teams should be assigned");
        }

        [Fact]
        public async Task ExecuteAsync_Should_ReturnFalse_WhenNotEnoughTeams()
        {
            // Arrange
            var teams = TestDataBuilder.Teams.CreateMultiple(3);
            var groupStandings = new List<Standing>
            {
                TestDataBuilder.Standings.CreateGroup(1, "Group A", 2),
                TestDataBuilder.Standings.CreateGroup(1, "Group B", 2),
                TestDataBuilder.Standings.CreateGroup(1, "Group C", 2),
                TestDataBuilder.Standings.CreateGroup(1, "Group D", 2)
            };

            var context = new StartGroupsContext
            {
                TournamentId = 1,
                GroupStandings = groupStandings
            };

            var tournamentTeams = teams.Select((t, i) =>
                TestDataBuilder.TournamentTeams.Create(1, i + 1)).ToList();

            _mockTournamentTeamRepo
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<TournamentTeam, bool>>>()))
                .ReturnsAsync(tournamentTeams);

            for (int i = 0; i < teams.Count; i++)
            {
                var team = teams[i];
                _mockTeamRepo
                    .Setup(x => x.GetByIdAsync(i + 1))
                    .ReturnsAsync(team);
            }

            // Act
            var result = await _step.ExecuteAsync(context);

            // Assert
            result.Should().BeFalse("not enough teams for all groups");
            context.Success.Should().BeFalse();
            context.Message.Should().Contain("Not enough teams");
        }
    }
}
