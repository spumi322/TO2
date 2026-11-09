using Application.Contracts;
using Application.DTOs.Team;
using Application.DTOs.Tournament;
using Application.Services;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Exceptions;
using Domain.StateMachine;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Base;
using Tests.Helpers;

namespace Tests.Integration.Persistence
{
    public class TenantIsolationTests : IntegrationTestBase
    {
        private readonly ITournamentService _tournamentService;
        private readonly ITeamService _teamService;

        public TenantIsolationTests()
        {
            DbContext.Tenants.Add(new Tenant { Id = 1, Name = "Org1" });
            DbContext.Tenants.Add(new Tenant { Id = 2, Name = "Org2" });
            DbContext.SaveChanges();

            var unitOfWork = new UnitOfWork(DbContext);
            var signalR = new Mock<ISignalRService>().Object; 

            var standingService = new StandingService(
                new StandingRepository(DbContext),
                new MatchRepository(DbContext),
                new Repository<Tournament>(DbContext),
                new GroupRepository(DbContext),
                new Repository<Team>(DbContext),
                new Repository<TournamentTeam>(DbContext),
                new FormatService(),
                new Mock<ILogger<StandingService>>().Object);

            _tournamentService = new TournamentService(
                new TournamentRepository(DbContext),
                new TournamentTeamRepository(DbContext),
                new StandingRepository(DbContext),
                new Repository<Team>(DbContext),
                standingService,
                new Mock<ILogger<TournamentService>>().Object,
                new TournamentStateMachine(),
                unitOfWork,
                signalR,
                DbFactory.TenantService);

            _teamService = new TeamService(
                new Repository<Team>(DbContext),
                new Repository<Tournament>(DbContext),
                new TournamentTeamRepository(DbContext),
                new Mock<ILogger<TeamService>>().Object,
                unitOfWork,
                signalR,
                DbFactory.TenantService,
                new TournamentStateMachine());
        }

        private async Task<(long TournamentId, long TeamId)> SeedOrg2TournamentWithTeamAsync()
        {
            DbFactory.LogInAs(2);
            var tournament = TestDataBuilder.Tournaments.CreateDefault("Org2 Cup");
            DbContext.Tournaments.Add(tournament);
            await DbContext.SaveChangesAsync();

            var team = TestDataBuilder.Teams.CreateDefault();
            DbContext.Teams.Add(team);
            DbContext.TournamentTeams.Add(new TournamentTeam(tournament.Id, team));
            await SaveAndDetachAsync();

            DbFactory.LogInAs(1);
            return (tournament.Id, team.Id);
        }

        [Fact]
        public async Task GetTornament_Throws_When_OtherTenantsTournament()
        {
            var (tournamentId, _) = await SeedOrg2TournamentWithTeamAsync();

            var act = () => _tournamentService.GetTournamentAsync(tournamentId);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteTournament_Throws_AndKeepsIt_When_OtherTenantsTournament()
        {
            var (tournamentId, _) = await SeedOrg2TournamentWithTeamAsync();

            var act = () => _tournamentService.DeleteTournamentAsync(tournamentId);
            await act.Should().ThrowAsync<NotFoundException>();

            DbFactory.LogInAs(2);
            var stillThere = await DbContext.Tournaments.AnyAsync(t => t.Id == tournamentId);
            stillThere.Should().BeTrue();
        }

        [Fact]
        public async Task GetTournamentList_DoesNotContain_OtherTenantsTournament()
        {
            var (tournamentId, _) = await SeedOrg2TournamentWithTeamAsync();

            var list = await _tournamentService.GetTournamentListAsync();

            list.Should().NotContain(t => t.Id == tournamentId);
        }

        [Fact]
        public async Task UpdateTournament_Throws_AndKeepsName_When_OtherTenantsTournament()
        {
            var (tournamentId, _) = await SeedOrg2TournamentWithTeamAsync();

            var act = () => _tournamentService.UpdateTournamentAsync(tournamentId, new UpdateTournamentRequestDTO("Sajt", "Sajt", 0));
            await act.Should().ThrowAsync<NotFoundException>();

            DbFactory.LogInAs(2);
            var tournament = await DbContext.Tournaments.SingleAsync(t => t.Id == tournamentId);
            tournament.Name.Should().Be("Org2 Cup");
        }

        [Fact]
        public async Task AddTeams_Throws_AndAddsNothing_When_OtherTenantsTournament()
        {
            var (tournamentId, _) = await SeedOrg2TournamentWithTeamAsync();

            var act = () => _teamService.AddTeamsToTournamentAsync(tournamentId, new AddTeamsRequestDTO(["Lajos"]));
            await act.Should().ThrowAsync<NotFoundException>();

            DbFactory.LogInAs(2);
            var registered = await DbContext.TournamentTeams.CountAsync(tt => tt.TournamentId == tournamentId);
            registered.Should().Be(1);
        }

        [Fact]
        public async Task RemoveTeam_Throws_AndKeepsRegistration_When_OtherTenantsTournament()
        {
            var (tournamentId, teamId) = await SeedOrg2TournamentWithTeamAsync();

            var act = () => _teamService.RemoveTeamFromTournamentAsync(teamId, tournamentId);
            await act.Should().ThrowAsync<NotFoundException>();

            DbFactory.LogInAs(2);
            var stillRegistered = await DbContext.TournamentTeams.AnyAsync(tt => tt.TournamentId == tournamentId && tt.TeamId == teamId);
            stillRegistered.Should().BeTrue();
        }

        [Fact]
        public async Task Query_ShowsOnlyOwnTenantsTournaments()
        {
            DbFactory.LogInAs(2);
            DbContext.Tournaments.Add(TestDataBuilder.Tournaments.CreateDefault());
            await SaveAndDetachAsync();

            DbFactory.LogInAs(1);
            var seenByOrg1 = await DbContext.Tournaments.ToListAsync();
            seenByOrg1.Should().BeEmpty();

            DbFactory.LogInAs(2);
            var seenByOrg2 = await DbContext.Tournaments.ToListAsync();
            seenByOrg2.Should().HaveCount(1);
        }

        [Fact]
        public async Task Save_SetsTenantId_OnNewTournament()
        {
            DbFactory.LogInAs(2);
            var tournament = TestDataBuilder.Tournaments.CreateDefault();
            tournament.TenantId.Should().Be(0);

            DbContext.Tournaments.Add(tournament);
            await DbContext.SaveChangesAsync();

            tournament.TenantId.Should().Be(2);
        }

        [Fact]
        public async Task Save_SetsTenantId_OnTeamAndTournamentTeam()
        {
            DbFactory.LogInAs(2);
            var tournament = TestDataBuilder.Tournaments.CreateDefault();
            var team = TestDataBuilder.Teams.CreateDefault();
            DbContext.Tournaments.Add(tournament);
            DbContext.Teams.Add(team);
            await DbContext.SaveChangesAsync();

            var registration = new TournamentTeam(tournament.Id, team.Id);
            DbContext.TournamentTeams.Add(registration);
            await DbContext.SaveChangesAsync();

            team.TenantId.Should().Be(2);
            registration.TenantId.Should().Be(2);
        }

        [Fact]
        public void EveryEntityWithTenantId_HasQueryFilter()
        {
            // User is unfiltered on purpose: login and register look users up before a tenant is known
            var unfiltered = DbContext.Model.GetEntityTypes()
                .Where(e => e.FindProperty("TenantId") != null && e.ClrType != typeof(User))
                .Where(e => e.GetQueryFilter() == null)
                .Select(e => e.ClrType.Name);

            unfiltered.Should().BeEmpty("a tenant entity without a query filter leaks rows across organizations");
        }

    }
}
