using Application.Services;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence.Repository;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Base;
using Tests.Helpers;

namespace Tests.Unit.Services
{
    public class PlacementServiceTests : IntegrationTestBase
    {
        private readonly PlacementService _service;

        public PlacementServiceTests()
        {
            _service = new PlacementService(
                new StandingRepository(DbContext),
                new MatchRepository(DbContext),
                new Repository<Tournament>(DbContext),
                new GroupRepository(DbContext),
                new Repository<Team>(DbContext),
                new Repository<TournamentTeam>(DbContext),
                new Mock<ILogger<PlacementService>>().Object);

            DbContext.Tenants.Add(new Tenant { Id = 1, Name = "Org1" });
            DbContext.SaveChanges();
        }

        // Finished tournament; each team: (name, group or null, eliminated in groups, final placement)
        private async Task<long> SeedFinishedAsync(Format format, params (string Name, string? Group, bool Eliminated, int Placement)[] teams)
        {
            var tournament = TestDataBuilder.Tournaments.CreateDefault(format: format);
            tournament.Status = TournamentStatus.Finished;
            DbContext.Tournaments.Add(tournament);
            await DbContext.SaveChangesAsync();

            var groups = new Dictionary<string, Standing>();
            foreach (var groupName in teams.Where(t => t.Group != null).Select(t => t.Group!).Distinct())
            {
                groups[groupName] = TestDataBuilder.Standings.CreateGroup(tournament.Id, groupName);
                DbContext.Standings.Add(groups[groupName]);
            }
            await DbContext.SaveChangesAsync();

            foreach (var t in teams)
            {
                var team = new Team(t.Name);
                DbContext.Teams.Add(team);
                await DbContext.SaveChangesAsync();

                var tournamentTeam = TestDataBuilder.TournamentTeams.Create(tournament.Id, team.Id);
                tournamentTeam.FinalPlacement = t.Placement;
                DbContext.TournamentTeams.Add(tournamentTeam);

                if (t.Group != null)
                {
                    var entry = TestDataBuilder.Groups.CreateWithIds(tournament.Id, groups[t.Group].Id, team.Id, team.Name);
                    entry.Eliminated = t.Eliminated;
                    DbContext.GroupEntries.Add(entry);
                }
            }

            await SaveAndDetachAsync();
            return tournament.Id;
        }

        [Fact]
        public async Task FinalResults_Should_SetGroupName_ForEveryTeam_When_GroupsOnly()
        {
            var id = await SeedFinishedAsync(Format.GroupsOnly,
                ("Alpha", "Group A", false, 1), ("Bravo", "Group B", false, 1),
                ("Charlie", "Group A", false, 2), ("Delta", "Group B", false, 2));

            var results = await _service.GetFinalResultsAsync(id);

            results.Select(r => (r.TeamName, r.GroupName)).Should().BeEquivalentTo(new[]
            {
                ("Alpha", "Group A"), ("Bravo", "Group B"), ("Charlie", "Group A"), ("Delta", "Group B")
            });
        }

        [Fact]
        public async Task FinalResults_Should_SetGroupName_OnlyForGroupStageKnockouts_When_GroupsAndBracket()
        {
            var id = await SeedFinishedAsync(Format.GroupsAndBracket,
                ("Alpha", "Group A", false, 1), ("Bravo", "Group B", false, 2),
                ("Charlie", "Group A", true, 3), ("Delta", "Group B", true, 3));

            var results = await _service.GetFinalResultsAsync(id);

            results.Single(r => r.TeamName == "Alpha").GroupName.Should().BeNull();
            results.Single(r => r.TeamName == "Bravo").GroupName.Should().BeNull();
            results.Single(r => r.TeamName == "Charlie").GroupName.Should().Be("Group A");
            results.Single(r => r.TeamName == "Delta").GroupName.Should().Be("Group B");
        }

        [Fact]
        public async Task FinalResults_Should_HaveNoGroupName_When_BracketOnly()
        {
            var id = await SeedFinishedAsync(Format.BracketOnly, ("Alpha", null, false, 1), ("Bravo", null, false, 2));

            var results = await _service.GetFinalResultsAsync(id);

            results.Should().OnlyContain(r => r.GroupName == null);
        }
    }
}
