using Application.Contracts;
using Application.Pipelines.StartBracket.Utilities;
using Application.Services;
using AutoMapper;
using Domain.AggregateRoots;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repository;
using Infrastructure.Profiles;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Base;
using Tests.Helpers;

namespace Tests.Unit.Services
{
    /// <summary>
    /// GroupsAndBracket: teams leaving the groups must be ordered so that
    /// group winners never meet each other in bracket round 1.
    /// </summary>
    public class StandingServiceBracketSeedingTests : IntegrationTestBase
    {
        private readonly StandingService _service;

        public StandingServiceBracketSeedingTests()
        {
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

            _service = new StandingService(
                new StandingRepository(DbContext),
                new MatchRepository(DbContext),
                new Repository<Tournament>(DbContext),
                new GroupRepository(DbContext),
                new Repository<Team>(DbContext),
                new TournamentTeamRepository(DbContext),
                new FormatService(),
                new Mock<ILogger<StandingService>>().Object,
                mapper,
                new UnitOfWork(DbContext));

            DbContext.Tenants.Add(new Tenant { Id = 1, Name = "Org1" });
            DbContext.SaveChanges();
        }

        [Theory]
        [InlineData(2, 2)] // 4 teams, no BYEs
        [InlineData(3, 2)] // 6 teams → 8 slots, 2 BYEs
        [InlineData(4, 2)] // 8 teams, no BYEs
        [InlineData(3, 3)] // 9 teams → 16 slots, 7 BYEs
        public async Task GroupWinners_Should_Not_Meet_In_FirstRound(int groupCount, int advancingPerGroup)
        {
            var (tournamentId, winnerIds) = await SeedFinishedGroupsAsync(groupCount, advancingPerGroup);

            var teams = await _service.GetTeamsForBracketByFormat(tournamentId);

            // Winners are the top seeds
            teams.Take(groupCount).Select(t => t.Id).Should().BeEquivalentTo(winnerIds);

            // Round 1 pairings as built by the StartBracket pipeline
            var pairs = BracketSeedingUtility.CreateSingleEliminationPairs(
                BracketSeedingUtility.PadToPowerOfTwo(teams));

            foreach (var (teamA, teamB) in pairs)
            {
                bool bothWinners = teamA != null && teamB != null
                    && winnerIds.Contains(teamA.Id) && winnerIds.Contains(teamB.Id);
                bothWinners.Should().BeFalse($"{teamA?.Name} vs {teamB?.Name} are both group winners");
            }
        }

        /// <summary>
        /// Creates finished groups with one more team than advance (so 1 is eliminated per group).
        /// Teams are inserted worst-first, so DB (Id) order differs from ranking order.
        /// </summary>
        private async Task<(long tournamentId, List<long> winnerIds)> SeedFinishedGroupsAsync(int groupCount, int advancingPerGroup)
        {
            int groupSize = advancingPerGroup + 1;

            var tournament = TestDataBuilder.Tournaments.CreateGroupsAndBracket(maxTeams: groupCount * groupSize);
            tournament.AdvancingPerGroup = advancingPerGroup;
            DbContext.Tournaments.Add(tournament);
            await DbContext.SaveChangesAsync();

            var winnerIds = new List<long>();

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

                    if (place == 1) winnerIds.Add(team.Id);
                }
            }

            await SaveAndDetachAsync();
            return (tournament.Id, winnerIds);
        }
    }
}
