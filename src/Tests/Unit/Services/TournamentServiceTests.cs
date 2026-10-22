using Application.Contracts;
using Application.DTOs.Tournament;
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
using Match = Domain.AggregateRoots.Match;

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

            var unitOfWork = new UnitOfWork(DbContext);

            var standingService = new StandingService(
                new StandingRepository(DbContext),
                new MatchRepository(DbContext),
                new Repository<Tournament>(DbContext),
                new GroupRepository(DbContext),
                new Repository<Team>(DbContext),
                new Repository<TournamentTeam>(DbContext),
                new FormatService(),
                new Mock<ILogger<StandingService>>().Object);

            _service = new TournamentService(
                new TournamentRepository(DbContext),
                new TournamentTeamRepository(DbContext),
                new StandingRepository(DbContext),
                new Repository<Team>(DbContext),
                standingService,
                new Mock<ILogger<TournamentService>>().Object,
                new TournamentStateMachine(),
                unitOfWork,
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

        private async Task<long> SeedTournamentWithTeamsAsync(Format format, TournamentStatus status, int teamCount)
        {
            var tournament = TestDataBuilder.Tournaments.CreateDefault(format: format);
            tournament.Status = status;
            DbContext.Tournaments.Add(tournament);
            await DbContext.SaveChangesAsync();

            for (var i = 0; i < teamCount; i++)
            {
                var team = TestDataBuilder.Teams.CreateDefault();
                DbContext.Teams.Add(team);
                await DbContext.SaveChangesAsync();
                DbContext.TournamentTeams.Add(TestDataBuilder.TournamentTeams.Create(tournament.Id, team.Id));
            }

            await SaveAndDetachAsync();
            return tournament.Id;
        }

        private async Task<Tournament> ReloadAsync(long id)
        {
            DbContext.ChangeTracker.Clear();
            return await DbContext.Tournaments.SingleAsync(t => t.Id == id);
        }

        #region State

        [Theory]
        [InlineData(Format.GroupsOnly, TournamentStatus.Setup, 2, true, false)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.Setup, 2, true, false)]
        [InlineData(Format.BracketOnly, TournamentStatus.Setup, 2, false, true)]
        [InlineData(Format.GroupsOnly, TournamentStatus.Setup, 1, false, false)]
        [InlineData(Format.BracketOnly, TournamentStatus.Setup, 0, false, false)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.GroupsInProgress, 4, false, false)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.GroupsCompleted, 4, false, true)]
        [InlineData(Format.GroupsOnly, TournamentStatus.GroupsInProgress, 4, false, false)]
        [InlineData(Format.BracketOnly, TournamentStatus.BracketInProgress, 4, false, false)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.Finished, 4, false, false)]
        [InlineData(Format.GroupsAndBracket, TournamentStatus.Cancelled, 4, false, false)]
        public async Task State_Should_SetStartFlags_ByStatusFormatAndTeamCount(
            Format format, TournamentStatus status, int teamCount, bool canStartGroups, bool canStartBracket)
        {
            var id = await SeedTournamentWithTeamsAsync(format, status, teamCount);

            var state = await _service.GetTournamentStateAsync(id);

            state.CanStartGroups.Should().Be(canStartGroups);
            state.CanStartBracket.Should().Be(canStartBracket);
        }

        #endregion

        #region Get

        [Fact]
        public async Task Get_Should_ReturnTeamCountGroupCountAndAdvancing()
        {
            var created = await _service.CreateTournamentAsync(
                new CreateTournamentRequestDTO("Summer Cup", "Desc", 8, Format.GroupsAndBracket, 2, 2));
            for (var i = 0; i < 3; i++)
            {
                var team = TestDataBuilder.Teams.CreateDefault();
                DbContext.Teams.Add(team);
                await DbContext.SaveChangesAsync();
                DbContext.TournamentTeams.Add(TestDataBuilder.TournamentTeams.Create(created.Id, team.Id));
            }
            await SaveAndDetachAsync();

            var result = await _service.GetTournamentAsync(created.Id);

            result.CurrentTeams.Should().Be(3);
            result.NumberOfGroups.Should().Be(2);
            result.AdvancingPerGroup.Should().Be(2);
        }

        [Fact]
        public async Task Get_Should_ReturnNoGroups_When_BracketOnly()
        {
            var created = await _service.CreateTournamentAsync(
                new CreateTournamentRequestDTO("Summer Cup", "Desc", 8, Format.BracketOnly, null, null));

            var result = await _service.GetTournamentAsync(created.Id);

            result.CurrentTeams.Should().Be(0);
            result.NumberOfGroups.Should().Be(0);
            result.AdvancingPerGroup.Should().BeNull();
        }

        #endregion

        #region List

        private async Task<long> SeedWithPlacementsAsync(Format format, TournamentStatus status, params (string Name, int? Placement)[] teams)
        {
            var tournament = TestDataBuilder.Tournaments.CreateDefault(format: format);
            tournament.Status = status;
            tournament.LastModifiedDate = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
            DbContext.Tournaments.Add(tournament);
            await DbContext.SaveChangesAsync();

            foreach (var (name, placement) in teams)
            {
                var team = TestDataBuilder.Teams.CreateDefault(name);
                DbContext.Teams.Add(team);
                await DbContext.SaveChangesAsync();
                var entry = TestDataBuilder.TournamentTeams.Create(tournament.Id, team.Id);
                entry.FinalPlacement = placement;
                DbContext.TournamentTeams.Add(entry);
            }

            await SaveAndDetachAsync();
            return tournament.Id;
        }

        [Fact]
        public async Task List_Should_ReturnWinnerAndFinishedAt_When_Finished()
        {
            var id = await SeedWithPlacementsAsync(Format.BracketOnly, TournamentStatus.Finished,
                ("Void Runners", 1), ("Static Wolves", 2), ("Team C", 3));

            var item = (await _service.GetTournamentListAsync()).Single(t => t.Id == id);

            item.WinnerName.Should().Be("Void Runners");
            item.FinishedAt.Should().Be(new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc));
            item.CurrentTeams.Should().Be(3);
        }

        [Fact]
        public async Task List_Should_ReturnNoWinner_When_SeveralGroupWinners()
        {
            // GroupsOnly with two groups: placement = group position, so two teams are 1st
            var id = await SeedWithPlacementsAsync(Format.GroupsOnly, TournamentStatus.Finished,
                ("A1", 1), ("B1", 1), ("A2", 2), ("B2", 2));

            var item = (await _service.GetTournamentListAsync()).Single(t => t.Id == id);

            item.WinnerName.Should().BeNull();
            item.FinishedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task List_Should_ReturnNoWinnerOrFinishedAt_When_Ongoing()
        {
            var id = await SeedWithPlacementsAsync(Format.BracketOnly, TournamentStatus.BracketInProgress,
                ("A", null), ("B", null));

            var item = (await _service.GetTournamentListAsync()).Single(t => t.Id == id);

            item.WinnerName.Should().BeNull();
            item.FinishedAt.Should().BeNull();
            item.CurrentTeams.Should().Be(2);
        }

        [Fact]
        public async Task List_Should_ReturnFinishedGroupsAndRounds()
        {
            var tournament = TestDataBuilder.Tournaments.CreateDefault(format: Format.GroupsAndBracket);
            tournament.Status = TournamentStatus.BracketInProgress;
            DbContext.Tournaments.Add(tournament);
            await DbContext.SaveChangesAsync();

            var groupA = TestDataBuilder.Standings.CreateGroup(tournament.Id, "Group A");
            groupA.IsFinished = true;
            var groupB = TestDataBuilder.Standings.CreateGroup(tournament.Id, "Group B");
            var bracket = TestDataBuilder.Standings.CreateBracket(tournament.Id);
            DbContext.Standings.AddRange(groupA, groupB, bracket);
            await DbContext.SaveChangesAsync();

            // Round 1 done (a BYE and a played match), round 2 open
            Match BracketMatch(int round, int seed, long? winnerId)
            {
                var match = TestDataBuilder.Matches.CreateWithoutTeams(bracket.Id);
                match.Round = round;
                match.Seed = seed;
                match.WinnerId = winnerId;
                return match;
            }
            DbContext.Matches.AddRange(BracketMatch(1, 1, 1), BracketMatch(1, 2, 2), BracketMatch(2, 1, null));
            await SaveAndDetachAsync();

            var item = (await _service.GetTournamentListAsync()).Single(t => t.Id == tournament.Id);

            item.GroupsFinished.Should().Be(1);
            item.GroupsTotal.Should().Be(2);
            item.RoundsFinished.Should().Be(1);
            item.RoundsTotal.Should().Be(2);
        }

        [Fact]
        public async Task List_Should_ReturnCancellation_When_Cancelled()
        {
            var id = await SeedTournamentAsync(TournamentStatus.GroupsInProgress);
            await _service.CancelTournamentAsync(id, new CancelTournamentRequestDTO("Not enough teams"));

            var item = (await _service.GetTournamentListAsync()).Single(t => t.Id == id);

            item.CancellationReason.Should().Be("Not enough teams");
            item.CancelledAt.Should().NotBeNull();
            item.FinishedAt.Should().BeNull();
        }

        #endregion

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
                $"INSERT INTO Players (Name, TeamId, CreatedDate, LastModifiedDate, CreatedBy, LastModifiedBy, Version) VALUES ('Player 1', {team.Id}, {DateTime.UtcNow}, {DateTime.UtcNow}, '', '', 0)");
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

            await _service.UpdateTournamentAsync(id, new UpdateTournamentRequestDTO("Winter Cup", "Desc", 0));

            var tournament = await ReloadAsync(id);
            tournament.Name.Should().Be("Winter Cup");
            tournament.Status.Should().Be(TournamentStatus.Cancelled);
        }

        [Fact]
        public async Task Update_Should_ReturnIncrementedVersion()
        {
            var id = await SeedTournamentAsync(TournamentStatus.Setup);

            var response = await _service.UpdateTournamentAsync(id, new UpdateTournamentRequestDTO("Winter Cup", "Desc", 0));

            response.Version.Should().Be(1);
            (await ReloadAsync(id)).Version.Should().Be(1);
        }

        [Fact]
        public async Task Update_Should_ThrowConflict_When_VersionStale()
        {
            // Client edits data it loaded before someone else's update
            var id = await SeedTournamentAsync(TournamentStatus.Setup);
            await _service.UpdateTournamentAsync(id, new UpdateTournamentRequestDTO("Winter Cup", "Desc", 0));
            DbContext.ChangeTracker.Clear();

            var act = () => _service.UpdateTournamentAsync(id, new UpdateTournamentRequestDTO("Spring Cup", "Desc", 0));

            await act.Should().ThrowAsync<ConflictException>();
            (await ReloadAsync(id)).Name.Should().Be("Winter Cup");
            _signalRMock.Verify(x => x.BroadcastTournamentUpdated(id, It.IsAny<string>()), Times.Once);
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

        [Fact]
        public async Task Create_Should_PersistTournamentAndStandings()
        {
            var request = new CreateTournamentRequestDTO("Summer Cup", "Desc", 8, Format.GroupsAndBracket, 2, 2);

            var result = await _service.CreateTournamentAsync(request);

            DbContext.ChangeTracker.Clear();
            var standings = await DbContext.Standings.Where(s => s.TournamentId == result.Id).ToListAsync();
            standings.Should().HaveCount(3);
            standings.Count(s => s.StandingType == StandingType.Group).Should().Be(2);
            standings.Should().ContainSingle(s => s.StandingType == StandingType.Bracket && s.MaxTeams == 4);
        }

        #endregion
    }
}
