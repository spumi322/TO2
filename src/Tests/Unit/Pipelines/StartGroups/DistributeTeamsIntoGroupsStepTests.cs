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
using Domain.Exceptions;

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

            _mockTeamRepo
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(teams);

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

            _mockTeamRepo
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(teams);

            // Act
            var result = await _step.ExecuteAsync(context);

            // Assert
            result.Should().BeTrue();
            context.Teams.Should().HaveCount(7);
            context.GroupAssignments.Should().HaveCount(2);
            context.GroupAssignments.Values.Sum(g => g.Count).Should().Be(7, "all teams should be assigned");
        }

        [Fact]
        public async Task ExecuteAsync_Should_ThrowConflict_WhenNotEnoughTeams()
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

            _mockTeamRepo
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(teams);

            // Act
            var act = () => _step.ExecuteAsync(context);

            // Assert
            await act.Should().ThrowAsync<ConflictException>().WithMessage("4 groups need at least 8 teams (2 per group); 3 registered.");
        }

        [Theory]
        [InlineData(3, 3, false)]  // groups of 1 have no match and never finish
        [InlineData(5, 3, false)]
        [InlineData(6, 3, true)]   // 2, 2, 2
        [InlineData(10, 4, true)]  // 3, 3, 2, 2
        public async Task ExecuteAsync_Should_RequireTwoTeamsPerGroup(int teamCount, int groupCount, bool starts)
        {
            var context = new StartGroupsContext
            {
                TournamentId = 1,
                GroupStandings = Enumerable.Range(1, groupCount)
                    .Select(i => TestDataBuilder.Standings.CreateGroup(1, $"Group {i}", 4))
                    .ToList()
            };
            _mockTeamRepo
                .Setup(x => x.FindAllAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(TestDataBuilder.Teams.CreateMultiple(teamCount));

            var act = () => _step.ExecuteAsync(context);

            if (starts)
            {
                await act.Should().NotThrowAsync();
                context.GroupAssignments.Values.Should().AllSatisfy(teams => teams.Count.Should().BeGreaterThanOrEqualTo(2));
            }
            else
            {
                await act.Should().ThrowAsync<ConflictException>();
            }
        }
    }
}
