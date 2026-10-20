using Application.Contracts;
using Application.DTOs.Team;
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

namespace Tests.Unit.Services
{
    public class TeamServiceTests : IntegrationTestBase
    {
        private readonly Mock<ISignalRService> _signalRMock = new();
        private readonly TeamService _service;

        public TeamServiceTests()
        {
            _service = CreateService(DbContext);

            DbContext.Tenants.Add(new Tenant { Id = 1, Name = "Org1" });
            DbContext.SaveChanges();
        }

        private TeamService CreateService(TO2DbContext context)
        {
            var tenantMock = new Mock<ITenantService>();
            tenantMock.Setup(x => x.GetCurrentTenantId()).Returns(1L);
            tenantMock.Setup(x => x.GetCurrentUserName()).Returns("admin1");

            return new TeamService(
                new Repository<Team>(context),
                new Repository<Tournament>(context),
                new TournamentTeamRepository(context),
                new Mock<ILogger<TeamService>>().Object,
                new UnitOfWork(context),
                _signalRMock.Object,
                tenantMock.Object,
                new TournamentStateMachine());
        }

        private async Task<long> SeedTournamentAsync(TournamentStatus status = TournamentStatus.Setup, int maxTeams = 8, params string[] teamNames)
        {
            var tournament = TestDataBuilder.Tournaments.CreateDefault(maxTeams: maxTeams);
            tournament.Status = status;
            DbContext.Tournaments.Add(tournament);
            await DbContext.SaveChangesAsync();

            foreach (var name in teamNames)
            {
                var team = new Team(name);
                DbContext.Teams.Add(team);
                await DbContext.SaveChangesAsync();
                DbContext.TournamentTeams.Add(TestDataBuilder.TournamentTeams.Create(tournament.Id, team.Id));
            }

            await SaveAndDetachAsync();
            return tournament.Id;
        }

        private async Task<List<string>> RegisteredNamesAsync(long tournamentId)
        {
            DbContext.ChangeTracker.Clear();
            return await DbContext.TournamentTeams
                .Where(tt => tt.TournamentId == tournamentId)
                .Select(tt => tt.Team.Name)
                .ToListAsync();
        }

        private static AddTeamsRequestDTO Names(params string[] names) => new(names.ToList());

        #region Add teams

        [Fact]
        public async Task AddTeams_Should_CreateAndRegisterAllTeams()
        {
            var id = await SeedTournamentAsync(teamNames: "Alpha");

            var result = await _service.AddTeamsToTournamentAsync(id, Names(" Bravo ", "Charlie"));

            result.Select(t => t.Name).Should().Equal("Bravo", "Charlie");
            result.Should().OnlyContain(t => t.Id > 0);
            (await RegisteredNamesAsync(id)).Should().BeEquivalentTo("Alpha", "Bravo", "Charlie");
        }

        [Fact]
        public async Task AddTeams_Should_BroadcastOnceWithAllTeamIds()
        {
            var id = await SeedTournamentAsync();

            var result = await _service.AddTeamsToTournamentAsync(id, Names("Alpha", "Bravo"));

            var ids = result.Select(t => t.Id).ToList();
            _signalRMock.Verify(x => x.BroadcastTeamAdded(id, It.Is<IReadOnlyList<long>>(l => l.SequenceEqual(ids)), "admin1"), Times.Once);
        }

        [Fact]
        public async Task AddTeams_Should_BumpTournamentVersion()
        {
            var id = await SeedTournamentAsync();

            await _service.AddTeamsToTournamentAsync(id, Names("Alpha"));

            DbContext.ChangeTracker.Clear();
            (await DbContext.Tournaments.SingleAsync(t => t.Id == id)).Version.Should().Be(1);
        }

        [Fact]
        public async Task AddTeams_Should_AlwaysCreateNewTeams()
        {
            // A team of the same name in another tournament is not reused
            await SeedTournamentAsync(teamNames: "Alpha");
            var id = await SeedTournamentAsync();

            await _service.AddTeamsToTournamentAsync(id, Names("Alpha"));

            DbContext.ChangeTracker.Clear();
            (await DbContext.Teams.CountAsync(t => t.Name == "Alpha")).Should().Be(2);
        }

        [Fact]
        public async Task AddTeams_Should_ThrowNotFound_When_TournamentMissing()
        {
            var act = () => _service.AddTeamsToTournamentAsync(999, Names("Alpha"));

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Theory]
        [InlineData(TournamentStatus.GroupsInProgress)]
        [InlineData(TournamentStatus.Finished)]
        [InlineData(TournamentStatus.Cancelled)]
        public async Task AddTeams_Should_ThrowConflict_When_RegistrationClosed(TournamentStatus status)
        {
            var id = await SeedTournamentAsync(status);

            var act = () => _service.AddTeamsToTournamentAsync(id, Names("Alpha"));

            await act.Should().ThrowAsync<ConflictException>();
            (await RegisteredNamesAsync(id)).Should().BeEmpty();
        }

        [Fact]
        public async Task AddTeams_Should_ThrowConflict_And_SaveNothing_When_NotEnoughSlots()
        {
            var id = await SeedTournamentAsync(maxTeams: 4, teamNames: new[] { "Alpha", "Bravo" });

            var act = () => _service.AddTeamsToTournamentAsync(id, Names("Charlie", "Delta", "Echo"));

            (await act.Should().ThrowAsync<ConflictException>()).WithMessage("Only 2 slots left.");
            (await RegisteredNamesAsync(id)).Should().HaveCount(2);
            (await DbContext.Teams.CountAsync()).Should().Be(2);
        }

        [Fact]
        public async Task AddTeams_Should_ThrowConflict_When_Full()
        {
            var id = await SeedTournamentAsync(maxTeams: 4, teamNames: new[] { "A", "B", "C", "D" });

            var act = () => _service.AddTeamsToTournamentAsync(id, Names("E"));

            (await act.Should().ThrowAsync<ConflictException>()).WithMessage("Tournament is full.");
        }

        [Fact]
        public async Task AddTeams_Should_ThrowConflict_And_SaveNothing_When_NameAlreadyRegistered_IgnoringCaseAndSpaces()
        {
            var id = await SeedTournamentAsync(teamNames: "Alpha");

            var act = () => _service.AddTeamsToTournamentAsync(id, Names("Bravo", "  ALPHA "));

            (await act.Should().ThrowAsync<ConflictException>()).WithMessage("Already registered: ALPHA.");
            (await RegisteredNamesAsync(id)).Should().Equal("Alpha");
            (await DbContext.Teams.CountAsync()).Should().Be(1);
            _signalRMock.Verify(x => x.BroadcastTeamAdded(It.IsAny<long>(), It.IsAny<IReadOnlyList<long>>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AddTeams_Should_ThrowConflict_When_TournamentChangedSinceLoad()
        {
            // Two batches at once: the second one loaded the tournament before the first one saved
            var id = await SeedTournamentAsync(maxTeams: 4);
            using var otherContext = DbFactory.CreateContext();
            var otherService = CreateService(otherContext);
            await otherContext.Tournaments.FindAsync(id);

            await _service.AddTeamsToTournamentAsync(id, Names("Alpha", "Bravo"));
            var act = () => otherService.AddTeamsToTournamentAsync(id, Names("Charlie"));

            await act.Should().ThrowAsync<ConflictException>();
            (await RegisteredNamesAsync(id)).Should().BeEquivalentTo("Alpha", "Bravo");
        }

        #endregion

        #region Remove team

        [Fact]
        public async Task RemoveTeam_Should_DeleteTeam_When_InNoOtherTournament()
        {
            var id = await SeedTournamentAsync(teamNames: "Alpha");
            var teamId = (await DbContext.Teams.SingleAsync()).Id;
            DbContext.ChangeTracker.Clear();

            await _service.RemoveTeamFromTournamentAsync(teamId, id);

            DbContext.ChangeTracker.Clear();
            (await DbContext.Teams.AnyAsync()).Should().BeFalse();
            (await DbContext.TournamentTeams.AnyAsync()).Should().BeFalse();
            (await DbContext.Tournaments.SingleAsync(t => t.Id == id)).Version.Should().Be(1);
            _signalRMock.Verify(x => x.BroadcastTeamRemoved(id, teamId, "admin1"), Times.Once);
        }

        [Fact]
        public async Task RemoveTeam_Should_KeepTeam_When_InAnotherTournament()
        {
            var id = await SeedTournamentAsync(teamNames: "Alpha");
            var team = await DbContext.Teams.SingleAsync();
            var other = TestDataBuilder.Tournaments.CreateDefault();
            DbContext.Tournaments.Add(other);
            await DbContext.SaveChangesAsync();
            DbContext.TournamentTeams.Add(TestDataBuilder.TournamentTeams.Create(other.Id, team.Id));
            await SaveAndDetachAsync();

            await _service.RemoveTeamFromTournamentAsync(team.Id, id);

            DbContext.ChangeTracker.Clear();
            (await DbContext.Teams.AnyAsync(t => t.Id == team.Id)).Should().BeTrue();
            (await RegisteredNamesAsync(id)).Should().BeEmpty();
            (await RegisteredNamesAsync(other.Id)).Should().Equal("Alpha");
        }

        [Fact]
        public async Task RemoveTeam_Should_ThrowConflict_When_RegistrationClosed()
        {
            var id = await SeedTournamentAsync(TournamentStatus.GroupsInProgress, teamNames: "Alpha");
            var teamId = (await DbContext.Teams.SingleAsync()).Id;

            var act = () => _service.RemoveTeamFromTournamentAsync(teamId, id);

            await act.Should().ThrowAsync<ConflictException>();
            (await RegisteredNamesAsync(id)).Should().Equal("Alpha");
        }

        #endregion
    }
}
