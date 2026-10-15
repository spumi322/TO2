using Application.Contracts;
using Application.DTOs.Tournament;
using Application.Services;
using AutoMapper;
using Domain.AggregateRoots;
using Domain.Enums;
using Domain.Exceptions;
using Domain.StateMachine;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repository;
using Infrastructure.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Base;
using Tests.Helpers;

namespace Tests.Unit.Services
{
    public class TournamentServiceTests : IntegrationTestBase
    {
        private const string CurrentUser = "admin1";

        private readonly Mock<ISignalRService> _signalRMock = new();
        private readonly TournamentService _service;

        public TournamentServiceTests()
        {
            var tenantMock = new Mock<ITenantService>();
            tenantMock.Setup(x => x.GetCurrentTenantId()).Returns(1L);
            tenantMock.Setup(x => x.GetCurrentUserName()).Returns(CurrentUser);

            var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

            _service = new TournamentService(
                new TournamentRepository(DbContext),
                new Repository<Team>(DbContext),
                new Mock<IStandingService>().Object,
                mapper,
                new Mock<ILogger<TournamentService>>().Object,
                new TournamentStateMachine(),
                new UnitOfWork(DbContext),
                _signalRMock.Object,
                tenantMock.Object);

            DbContext.Tenants.Add(new Tenant { Id = 1, Name = "Org1" });
            DbContext.SaveChanges();
        }

        private async Task<long> SeedTournamentAsync(TournamentStatus status)
        {
            var tournament = TestDataBuilder.Tournaments.CreateInStatus(status, name: "Summer Cup");
            DbContext.Tournaments.Add(tournament);
            await SaveAndDetachAsync();
            return tournament.Id;
        }

        private async Task<Tournament> ReloadAsync(long id)
        {
            DbContext.ChangeTracker.Clear();
            return await DbContext.Tournaments.SingleAsync(t => t.Id == id);
        }

        #region Cancel

        [Theory]
        [InlineData(TournamentStatus.Setup)]
        [InlineData(TournamentStatus.Finished)]
        [InlineData(TournamentStatus.Cancelled)]
        public async Task Cancel_Should_Throw_When_StatusNotCancellable(TournamentStatus status)
        {
            var id = await SeedTournamentAsync(status);

            var act = () => _service.CancelTournamentAsync(id, new CancelTournamentRequestDTO(null));

            await act.Should().ThrowAsync<ConflictException>();
            (await ReloadAsync(id)).Status.Should().Be(status);
            _signalRMock.Verify(x => x.BroadcastTournamentCancelled(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(TournamentStatus.SeedingGroups)]
        [InlineData(TournamentStatus.GroupsInProgress)]
        [InlineData(TournamentStatus.GroupsCompleted)]
        [InlineData(TournamentStatus.SeedingBracket)]
        [InlineData(TournamentStatus.BracketInProgress)]
        public async Task Cancel_Should_StoreWhoWhenReason_When_StatusCancellable(TournamentStatus status)
        {
            var id = await SeedTournamentAsync(status);

            await _service.CancelTournamentAsync(id, new CancelTournamentRequestDTO("  Venue closed  "));

            var tournament = await ReloadAsync(id);
            tournament.Status.Should().Be(TournamentStatus.Cancelled);
            tournament.CancelledBy.Should().Be(CurrentUser);
            tournament.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            tournament.CancellationReason.Should().Be("Venue closed");
            _signalRMock.Verify(x => x.BroadcastTournamentCancelled(id, CurrentUser), Times.Once);
        }

        [Fact]
        public async Task Cancel_Should_StoreNullReason_When_ReasonBlank()
        {
            var id = await SeedTournamentAsync(TournamentStatus.GroupsInProgress);

            await _service.CancelTournamentAsync(id, new CancelTournamentRequestDTO("   "));

            (await ReloadAsync(id)).CancellationReason.Should().BeNull();
        }

        [Fact]
        public async Task Cancel_Should_ThrowNotFound_When_TournamentMissing()
        {
            var act = () => _service.CancelTournamentAsync(999, new CancelTournamentRequestDTO(null));

            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion

        #region Delete

        [Theory]
        [InlineData(TournamentStatus.SeedingGroups)]
        [InlineData(TournamentStatus.GroupsInProgress)]
        [InlineData(TournamentStatus.GroupsCompleted)]
        [InlineData(TournamentStatus.SeedingBracket)]
        [InlineData(TournamentStatus.BracketInProgress)]
        [InlineData(TournamentStatus.Finished)]
        [InlineData(TournamentStatus.Cancelled)]
        public async Task Delete_Should_Throw_When_NotInSetup(TournamentStatus status)
        {
            var id = await SeedTournamentAsync(status);

            var act = () => _service.DeleteTournamentAsync(id);

            await act.Should().ThrowAsync<ConflictException>();
            (await DbContext.Tournaments.AnyAsync(t => t.Id == id)).Should().BeTrue();
        }

        [Fact]
        public async Task Delete_Should_RemoveTournament_When_InSetup()
        {
            var id = await SeedTournamentAsync(TournamentStatus.Setup);

            await _service.DeleteTournamentAsync(id);

            DbContext.ChangeTracker.Clear();
            (await DbContext.Tournaments.AnyAsync(t => t.Id == id)).Should().BeFalse();
        }

        [Fact]
        public async Task Delete_Should_RemoveExclusiveTeamAndItsPlayers()
        {
            var id = await SeedTournamentAsync(TournamentStatus.Setup);
            var team = TestDataBuilder.Teams.CreateDefault("Alpha Squad");
            DbContext.Teams.Add(team);
            await DbContext.SaveChangesAsync();
            DbContext.TournamentTeams.Add(TestDataBuilder.TournamentTeams.Create(id, team.Id));
            await DbContext.SaveChangesAsync();

            // No app code creates players yet; insert one directly
            await DbContext.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO Players (Name, TeamId, CreatedDate, LastModifiedDate, CreatedBy, LastModifiedBy) VALUES ('Player 1', {team.Id}, {DateTime.UtcNow}, {DateTime.UtcNow}, '', '')");
            DbContext.ChangeTracker.Clear();

            await _service.DeleteTournamentAsync(id);

            DbContext.ChangeTracker.Clear();
            (await DbContext.Teams.AnyAsync(t => t.Id == team.Id)).Should().BeFalse();
            (await DbContext.Players.AnyAsync()).Should().BeFalse();
        }

        #endregion

        #region Update

        [Fact]
        public async Task Update_Should_NotChangeStatus()
        {
            // PUT /tournaments/{id} must not be a back door to un-cancel / jump states
            var id = await SeedTournamentAsync(TournamentStatus.Cancelled);

            await _service.UpdateTournamentAsync(id, new UpdateTournamentRequestDTO("Winter Cup", "Desc", null));

            var tournament = await ReloadAsync(id);
            tournament.Name.Should().Be("Winter Cup");
            tournament.Status.Should().Be(TournamentStatus.Cancelled);
        }

        #endregion

        #region Create

        [Fact]
        public async Task Create_Should_AllowDuplicateNames()
        {
            var request = new CreateTournamentRequestDTO("Summer Cup", "Desc", 8, Format.BracketOnly, null, null);

            var first = await _service.CreateTournamentAsync(request);
            var second = await _service.CreateTournamentAsync(request);

            second.Id.Should().NotBe(first.Id);
            DbContext.ChangeTracker.Clear();
            (await DbContext.Tournaments.CountAsync(t => t.Name == "Summer Cup")).Should().Be(2);
        }

        #endregion
    }
}
