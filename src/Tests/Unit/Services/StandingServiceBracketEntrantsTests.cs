using Application.Services;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
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
    /// <summary>
    /// StandingService.GetTeamsForBracketByFormat: which teams enter the bracket, with their group placement.
    /// </summary>
    public class StandingServiceBracketEntrantsTests : IntegrationTestBase
    {
        private readonly StandingService _service;

        public StandingServiceBracketEntrantsTests()
        {
            _service = new StandingService(
                new StandingRepository(DbContext),
                new MatchRepository(DbContext),
                new Repository<Tournament>(DbContext),
                new GroupRepository(DbContext),
                new Repository<Team>(DbContext),
                new TournamentTeamRepository(DbContext),
                new FormatService(),
                new Mock<ILogger<StandingService>>().Object,
                new UnitOfWork(DbContext));

            DbContext.Tenants.Add(new Tenant { Id = 1, Name = "Org1" });
            DbContext.SaveChanges();
        }

        [Fact]
        public async Task GroupsAndBracket_ReturnsAdvancingTeamsWithGroupPlacement()
        {
            var tournamentId = await SeedFinishedGroupsAsync(groupCount: 3, advancingPerGroup: 2);

            var entrants = await _service.GetTeamsForBracketByFormat(tournamentId);

            // Team names are group letter + placement (A1 = winner of group A)
            entrants.Select(e => e.Team.Name)
                .Should().BeEquivalentTo("A1", "A2", "B1", "B2", "C1", "C2");
            entrants.Should().AllSatisfy(e =>
            {
                e.GroupPlacement.Should().Be(int.Parse(e.Team.Name[1..]));
                e.GroupId.Should().NotBeNull();
            });
            entrants.Select(e => e.GroupId).Distinct().Should().HaveCount(3);
        }

        [Fact]
        public async Task GroupsAndBracket_MarksGroupEntriesAdvancedOrEliminated()
        {
            var tournamentId = await SeedFinishedGroupsAsync(groupCount: 2, advancingPerGroup: 2);

            await _service.GetTeamsForBracketByFormat(tournamentId);
            await SaveAndDetachAsync();

            var entries = await DbContext.GroupEntries.ToListAsync();
            entries.Where(e => e.TeamName.EndsWith("3")).Should().AllSatisfy(e => e.Status.Should().Be(TeamStatus.Eliminated));
            entries.Where(e => !e.TeamName.EndsWith("3")).Should().AllSatisfy(e => e.Status.Should().Be(TeamStatus.Advanced));
        }

        [Fact]
        public async Task BracketOnly_ReturnsAllRegisteredTeamsWithoutGroup()
        {
            var tournament = TestDataBuilder.Tournaments.CreateDefault(format: Format.BracketOnly);
            DbContext.Tournaments.Add(tournament);
            var teams = TestDataBuilder.Teams.CreateMultiple(3);
            DbContext.Teams.AddRange(teams);
            await DbContext.SaveChangesAsync();
            DbContext.TournamentTeams.AddRange(teams.Select(t => TestDataBuilder.TournamentTeams.Create(tournament.Id, t.Id)));
            await SaveAndDetachAsync();

            var entrants = await _service.GetTeamsForBracketByFormat(tournament.Id);

            entrants.Select(e => e.Team.Id).Should().BeEquivalentTo(teams.Select(t => t.Id));
            entrants.Should().AllSatisfy(e =>
            {
                e.GroupId.Should().BeNull();
                e.GroupPlacement.Should().BeNull();
            });
        }

        /// <summary>
        /// Creates finished groups with one more team than advance (so 1 is eliminated per group).
        /// Team names are group letter + placement; teams are inserted worst-first.
        /// </summary>
        private async Task<long> SeedFinishedGroupsAsync(int groupCount, int advancingPerGroup)
        {
            int groupSize = advancingPerGroup + 1;

            var tournament = TestDataBuilder.Tournaments.CreateGroupsAndBracket(maxTeams: groupCount * groupSize);
            tournament.AdvancingPerGroup = advancingPerGroup;
            DbContext.Tournaments.Add(tournament);
            await DbContext.SaveChangesAsync();

            for (int g = 0; g < groupCount; g++)
            {
                var groupName = ((char)('A' + g)).ToString();
                var group = TestDataBuilder.Standings.CreateGroup(tournament.Id, $"Group {groupName}", groupSize);
                DbContext.Standings.Add(group);
                await DbContext.SaveChangesAsync();

                for (int place = groupSize; place >= 1; place--)
                {
                    var team = new Team($"{groupName}{place}");
                    DbContext.Teams.Add(team);
                    await DbContext.SaveChangesAsync();

                    var entry = TestDataBuilder.Groups.CreateWithIds(tournament.Id, group.Id, team.Id, team.Name);
                    entry.Wins = groupSize - place;
                    entry.Losses = place - 1;
                    DbContext.GroupEntries.Add(entry);
                }
            }

            await SaveAndDetachAsync();
            return tournament.Id;
        }
    }
}
