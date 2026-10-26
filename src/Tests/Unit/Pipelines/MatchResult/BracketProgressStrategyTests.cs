using Application.Contracts;
using Application.Pipelines.MatchResult.Strategies;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence.Repository;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Base;
using Tests.Helpers;
using Match = Domain.AggregateRoots.Match;

namespace Tests.Unit.Pipelines.MatchResult
{
    public class BracketProgressStrategyTests : IntegrationTestBase
    {
        private readonly BracketProgressStrategy _strategy;

        public BracketProgressStrategyTests()
        {
            _strategy = new BracketProgressStrategy(
                new Repository<Match>(DbContext),
                new Mock<IUnitOfWork>().Object,
                new Mock<IGameService>().Object,
                new Mock<IStandingService>().Object,
                new Mock<ILogger<BracketProgressStrategy>>().Object);

            DbContext.Tenants.Add(new Tenant { Id = 1, Name = "Org1" });
            DbContext.SaveChanges();
        }

        // 4-team bracket: R1S1 (team 1 vs BYE, resolved), R1S2 (teams 2 vs 3), final R2S1
        private async Task<(long StandingId, long TournamentId, Match Open)> SeedBracketAsync()
        {
            var tournament = TestDataBuilder.Tournaments.CreateInStatus(TournamentStatus.BracketInProgress);
            DbContext.Tournaments.Add(tournament);
            await DbContext.SaveChangesAsync();

            var standing = TestDataBuilder.Standings.CreateBracket(tournament.Id, maxTeams: 4);
            DbContext.Standings.Add(standing);
            await DbContext.SaveChangesAsync();

            var bye = TestDataBuilder.Matches.CreateWithoutTeams(standing.Id);
            bye.Round = 1; bye.Seed = 1; bye.TeamAId = 1; bye.WinnerId = 1;
            var open = TestDataBuilder.Matches.CreateWithoutTeams(standing.Id);
            open.Round = 1; open.Seed = 2; open.TeamAId = 2; open.TeamBId = 3;
            var final = TestDataBuilder.Matches.CreateWithoutTeams(standing.Id);
            final.Round = 2; final.Seed = 1; final.TeamAId = 1;
            DbContext.Matches.AddRange(bye, open, final);
            await DbContext.SaveChangesAsync();

            return (standing.Id, tournament.Id, open);
        }

        [Fact]
        public async Task Should_ReportRoundFinished_When_LastMatchOfRoundWon()
        {
            var (standingId, tournamentId, open) = await SeedBracketAsync();
            open.WinnerId = 2;

            var result = await _strategy.ProgressStandingAsync(tournamentId, standingId, open.Id, 2);

            result.RoundFinished.Should().BeTrue();
            result.TournamentFinished.Should().BeFalse();
        }

        [Fact]
        public async Task Should_NotReportRoundFinished_When_RoundHasOpenMatch()
        {
            var (standingId, tournamentId, _) = await SeedBracketAsync();
            var other = TestDataBuilder.Matches.CreateWithoutTeams(standingId);
            other.Round = 1; other.Seed = 3; other.TeamAId = 4; other.TeamBId = 5; other.WinnerId = 4;
            DbContext.Matches.Add(other);
            await DbContext.SaveChangesAsync();

            // R1S2 is still open, so finishing R1S3 does not finish round 1
            var result = await _strategy.ProgressStandingAsync(tournamentId, standingId, other.Id, 4);

            result.RoundFinished.Should().BeFalse();
        }
    }
}
