using Application.Contracts;
using Application.Pipelines.StartGroups;
using Application.Pipelines.StartGroups.Contracts;
using Application.Pipelines.StartGroups.Steps;
using Application.Services;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.StateMachine;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Base;
using Tests.Helpers;

namespace Tests.Unit.Pipelines.StartGroups
{
    public class StartGroupsPipelineRollbackTests : IntegrationTestBase
    {
        private readonly Mock<IStandingService> _standingServiceMock = new();
        private readonly Mock<ISignalRService> _signalRMock = new();
        private readonly StartGroupsPipeline _pipeline;

        public StartGroupsPipelineRollbackTests()
        {
            var steps = new IStartGroupsPipelineStep[]
            {
                new ValidateAndTransitionToSeedingGroupsStep(
                    new Mock<ILogger<ValidateAndTransitionToSeedingGroupsStep>>().Object,
                    new Repository<Tournament>(DbContext),
                    new TournamentStateMachine(),
                    new FormatService()),
                new ValidateStandingsNotSeededStep(
                    new Mock<ILogger<ValidateStandingsNotSeededStep>>().Object,
                    _standingServiceMock.Object),
                new DistributeTeamsIntoGroupsStep(
                    new Mock<ILogger<DistributeTeamsIntoGroupsStep>>().Object,
                    new Repository<TournamentTeam>(DbContext),
                    new Repository<Team>(DbContext)),
            };

            _pipeline = new StartGroupsPipeline(
                new Mock<ILogger<StartGroupsPipeline>>().Object,
                steps,
                new UnitOfWork(DbContext),
                _signalRMock.Object,
                new Mock<ITenantService>().Object);

            DbContext.Tenants.Add(new Tenant { Id = 1, Name = "Org1" });
            DbContext.SaveChanges();
        }

        [Fact]
        public async Task Should_RollBackStatusChange_When_LaterStepFails()
        {
            // GroupsOnly tournament with 2 groups and no teams: step 1 moves it to SeedingGroups, step 3 fails
            var tournament = TestDataBuilder.Tournaments.CreateDefault(format: Format.GroupsOnly);
            DbContext.Tournaments.Add(tournament);
            await DbContext.SaveChangesAsync();

            var groups = new List<Standing>
            {
                TestDataBuilder.Standings.CreateGroup(tournament.Id, "Group A"),
                TestDataBuilder.Standings.CreateGroup(tournament.Id, "Group B"),
            };
            DbContext.Standings.AddRange(groups);
            await SaveAndDetachAsync();
            _standingServiceMock.Setup(s => s.GetStandingsAsync(tournament.Id)).ReturnsAsync(groups);

            var act = () => _pipeline.ExecuteAsync(tournament.Id);

            await act.Should().ThrowAsync<ConflictException>().WithMessage("Not enough teams*");

            DbContext.ChangeTracker.Clear();
            var stored = await DbContext.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id);
            stored.Status.Should().Be(TournamentStatus.Setup);
            stored.IsRegistrationOpen.Should().BeTrue();
            _signalRMock.Verify(s => s.BroadcastGroupsStarted(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Should_KeepCommittedChanges_When_BroadcastFails()
        {
            var tournament = TestDataBuilder.Tournaments.CreateDefault(format: Format.GroupsOnly);
            var teams = TestDataBuilder.Teams.CreateMultiple(2);
            DbContext.Tournaments.Add(tournament);
            DbContext.Teams.AddRange(teams);
            await DbContext.SaveChangesAsync();

            var groups = new List<Standing>
            {
                TestDataBuilder.Standings.CreateGroup(tournament.Id, "Group A"),
                TestDataBuilder.Standings.CreateGroup(tournament.Id, "Group B"),
            };
            DbContext.Standings.AddRange(groups);
            DbContext.TournamentTeams.AddRange(teams.Select(t => TestDataBuilder.TournamentTeams.Create(tournament.Id, t.Id)));
            await SaveAndDetachAsync();
            _standingServiceMock.Setup(s => s.GetStandingsAsync(tournament.Id)).ReturnsAsync(groups);
            _signalRMock.Setup(s => s.BroadcastGroupsStarted(It.IsAny<long>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("SignalR down"));

            var act = () => _pipeline.ExecuteAsync(tournament.Id);

            await act.Should().NotThrowAsync();

            DbContext.ChangeTracker.Clear();
            var stored = await DbContext.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id);
            stored.Status.Should().Be(TournamentStatus.SeedingGroups);
        }

        [Fact]
        public async Task Should_ThrowNotFound_When_TournamentMissing()
        {
            var act = () => _pipeline.ExecuteAsync(999);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Should_ThrowConflict_When_FormatHasNoGroups()
        {
            var tournament = TestDataBuilder.Tournaments.CreateDefault(format: Format.BracketOnly);
            DbContext.Tournaments.Add(tournament);
            await SaveAndDetachAsync();

            var act = () => _pipeline.ExecuteAsync(tournament.Id);

            await act.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task Should_ThrowConflict_When_GroupsAlreadyStarted()
        {
            var tournament = TestDataBuilder.Tournaments.CreateDefault(format: Format.GroupsOnly);
            tournament.Status = TournamentStatus.GroupsInProgress;
            DbContext.Tournaments.Add(tournament);
            await SaveAndDetachAsync();

            var act = () => _pipeline.ExecuteAsync(tournament.Id);

            await act.Should().ThrowAsync<ConflictException>();
        }
    }
}
