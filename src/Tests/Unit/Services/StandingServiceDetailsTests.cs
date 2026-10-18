using Application.Services;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repository;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Base;
using Tests.Helpers;
using Match = Domain.AggregateRoots.Match;

namespace Tests.Unit.Services
{
    /// <summary>
    /// StandingService group/bracket detail reads: entity → DTO mapping and calculated match wins.
    /// </summary>
    public class StandingServiceDetailsTests : IntegrationTestBase
    {
        private readonly StandingService _service;

        public StandingServiceDetailsTests()
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

        private async Task<(Tournament Tournament, Standing Standing, Team TeamA, Team TeamB, Match Match)> SeedMatchAsync(Standing standingTemplate)
        {
            var tournament = TestDataBuilder.Tournaments.CreateDefault();
            DbContext.Tournaments.Add(tournament);
            var teamA = TestDataBuilder.Teams.CreateDefault("Alpha");
            var teamB = TestDataBuilder.Teams.CreateDefault("Bravo");
            DbContext.Teams.AddRange(teamA, teamB);
            await DbContext.SaveChangesAsync();

            standingTemplate.TournamentId = tournament.Id;
            DbContext.Standings.Add(standingTemplate);
            await DbContext.SaveChangesAsync();

            var match = TestDataBuilder.Matches.Create(standingTemplate.Id, teamA, teamB, BestOf.Bo3);
            match.Round = 1;
            match.Seed = 2;
            DbContext.Matches.Add(match);
            await DbContext.SaveChangesAsync();

            DbContext.Games.AddRange(
                TestDataBuilder.Games.CreateCompleted(match, teamA.Id, teamB.Id, winnerId: teamA.Id, teamAScore: 16, teamBScore: 9),
                TestDataBuilder.Games.Create(match, teamA.Id, teamB.Id));
            await SaveAndDetachAsync();

            return (tournament, standingTemplate, teamA, teamB, match);
        }

        [Fact]
        public async Task GetBracketWithDetails_MapsMatchAndGames()
        {
            var seeded = await SeedMatchAsync(TestDataBuilder.Standings.CreateBracket(0));

            var bracket = await _service.GetBracketWithDetailsAsync(seeded.Tournament.Id);

            bracket.Should().NotBeNull();
            bracket!.Id.Should().Be(seeded.Standing.Id);
            var match = bracket.Matches.Should().ContainSingle().Subject;
            match.Id.Should().Be(seeded.Match.Id);
            match.StandingId.Should().Be(seeded.Standing.Id);
            match.Round.Should().Be(1);
            match.Seed.Should().Be(2);
            match.TeamAId.Should().Be(seeded.TeamA.Id);
            match.TeamBId.Should().Be(seeded.TeamB.Id);
            match.BestOf.Should().Be((int)BestOf.Bo3);
            match.TeamAWins.Should().Be(1);
            match.TeamBWins.Should().Be(0);
            match.Games.Should().HaveCount(2);
            match.Games.Should().ContainSingle(g => g.WinnerId == seeded.TeamA.Id && g.TeamAScore == 16 && g.TeamBScore == 9)
                .Which.MatchId.Should().Be(seeded.Match.Id);
        }

        [Fact]
        public async Task GetGroupsWithDetails_MapsTeamsOrderedByPoints()
        {
            var seeded = await SeedMatchAsync(TestDataBuilder.Standings.CreateGroup(0));
            var entryA = TestDataBuilder.Groups.CreateWithIds(seeded.Tournament.Id, seeded.Standing.Id, seeded.TeamA.Id, "Alpha");
            entryA.Wins = 0; entryA.Losses = 1; entryA.Points = 0;
            var entryB = TestDataBuilder.Groups.CreateWithIds(seeded.Tournament.Id, seeded.Standing.Id, seeded.TeamB.Id, "Bravo");
            entryB.Wins = 1; entryB.Losses = 0; entryB.Points = 3; entryB.Status = TeamStatus.Advanced;
            DbContext.GroupEntries.AddRange(entryA, entryB);
            await SaveAndDetachAsync();

            var groups = await _service.GetGroupsWithDetailsAsync(seeded.Tournament.Id);

            var group = groups.Should().ContainSingle().Subject;
            group.Teams.Select(t => t.Name).Should().Equal("Bravo", "Alpha");
            var top = group.Teams[0];
            top.Id.Should().Be(seeded.TeamB.Id);
            top.Wins.Should().Be(1);
            top.Losses.Should().Be(0);
            top.Points.Should().Be(3);
            top.Status.Should().Be((int)TeamStatus.Advanced);
            group.Matches.Should().ContainSingle().Which.TeamAWins.Should().Be(1);
        }
    }
}
