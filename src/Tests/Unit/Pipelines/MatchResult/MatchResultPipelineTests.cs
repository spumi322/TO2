using Application.Contracts;
using Application.DTOs.Match;
using Application.DTOs.SignalR;
using Application.Pipelines.MatchResult;
using Application.Pipelines.MatchResult.Contracts;
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

namespace Tests.Unit.Pipelines.MatchResult
{
    public class MatchResultPipelineTests : IntegrationTestBase
    {
        private const long TeamA = 1;
        private const long TeamB = 2;

        private readonly Mock<ISignalRService> _signalRMock = new();
        private readonly MatchDraftStore _draftStore = new(TimeProvider.System);
        private readonly Mock<IMatchResultPipelineStep> _stepMock = new();
        private MatchResultContext? _seenContext;
        private readonly MatchResultPipeline _pipeline;

        public MatchResultPipelineTests()
        {
            _stepMock.Setup(s => s.ExecuteAsync(It.IsAny<MatchResultContext>()))
                .Callback<MatchResultContext>(c => _seenContext = c)
                .ReturnsAsync(true);

            // One fake step: sees the context the real steps get
            IMatchResultPipelineStep[] steps = { _stepMock.Object };

            _pipeline = new MatchResultPipeline(
                new Mock<ILogger<MatchResultPipeline>>().Object,
                new Repository<Tournament>(DbContext),
                new Repository<Standing>(DbContext),
                new Repository<Match>(DbContext),
                new Repository<Game>(DbContext),
                new TournamentStateMachine(),
                new FormatService(),
                steps,
                new UnitOfWork(DbContext),
                _signalRMock.Object,
                _draftStore,
                new Mock<ITenantService>().Object,
                new Mock<IStandingService>().Object);

            DbContext.Tenants.Add(new Tenant { Id = 1, Name = "Org1" });
            DbContext.SaveChanges();
        }

        // Best of 3 group match between team 1 and team 2, three unplayed games
        private async Task<SetMatchResultDTO> SeedMatchAsync(TournamentStatus status = TournamentStatus.GroupsInProgress)
        {
            var tournament = TestDataBuilder.Tournaments.CreateInStatus(status);
            DbContext.Tournaments.Add(tournament);
            await DbContext.SaveChangesAsync();

            var standing = TestDataBuilder.Standings.CreateGroup(tournament.Id);
            DbContext.Standings.Add(standing);
            await DbContext.SaveChangesAsync();

            var match = TestDataBuilder.Matches.CreateWithoutTeams(standing.Id, BestOf.Bo3);
            match.TeamAId = TeamA;
            match.TeamBId = TeamB;
            DbContext.Matches.Add(match);
            await DbContext.SaveChangesAsync();

            for (var i = 0; i < 3; i++)
                DbContext.Games.Add(TestDataBuilder.Games.Create(match, TeamA, TeamB));
            await SaveAndDetachAsync();

            return new SetMatchResultDTO(match.Id, standing.Id, tournament.Id, 0, 0, match.Version);
        }

        private async Task<List<long?>> GameWinnersAsync(long matchId) =>
            await DbContext.Games.AsNoTracking().Where(g => g.MatchId == matchId).OrderBy(g => g.Id).Select(g => g.WinnerId).ToListAsync();

        [Fact]
        public async Task Should_WriteLoserGamesFirst_AndDecideTheMatch()
        {
            var request = await SeedMatchAsync() with { TeamAWins = 1, TeamBWins = 2 };

            var result = await _pipeline.ExecuteAsync(request);

            (await GameWinnersAsync(request.MatchId)).Should().Equal(TeamA, TeamB, TeamB);
            var match = await DbContext.Matches.AsNoTracking().SingleAsync(m => m.Id == request.MatchId);
            match.WinnerId.Should().Be(TeamB);
            result.MatchWinnerId.Should().Be(TeamB);
            // Steps see the winner and the deciding (last) game
            var lastGameId = await DbContext.Games.Where(g => g.MatchId == request.MatchId).MaxAsync(g => g.Id);
            _seenContext!.WinnerId.Should().Be(TeamB);
            _seenContext.LoserId.Should().Be(TeamA);
            _seenContext.DecidingGameId.Should().Be(lastGameId);
        }

        [Fact]
        public async Task Should_LeaveUnplayedGamesEmpty()
        {
            var request = await SeedMatchAsync() with { TeamAWins = 2, TeamBWins = 0 };

            await _pipeline.ExecuteAsync(request);

            (await GameWinnersAsync(request.MatchId)).Should().Equal(TeamA, TeamA, null);
        }

        [Fact]
        public async Task Should_BroadcastOnce()
        {
            var request = await SeedMatchAsync() with { TeamAWins = 2, TeamBWins = 1 };

            await _pipeline.ExecuteAsync(request);

            _signalRMock.Verify(s => s.BroadcastGameUpdated(It.Is<GameUpdatedEvent>(e => e.MatchId == request.MatchId)), Times.Once);
        }

        [Fact]
        public async Task Should_ClearTheStoredDraft()
        {
            var request = await SeedMatchAsync() with { TeamAWins = 2, TeamBWins = 0 };
            _draftStore.Set(new MatchDraftUpdatedEvent(request.TournamentId, request.MatchId, 1, 0, "Other"));

            await _pipeline.ExecuteAsync(request);

            _draftStore.GetAll(request.TournamentId).Should().BeEmpty();
        }

        [Fact]
        public async Task Should_RollBackAndNotBroadcast_When_StepFails()
        {
            var request = await SeedMatchAsync() with { TeamAWins = 2, TeamBWins = 0 };
            _stepMock.Setup(s => s.ExecuteAsync(It.IsAny<MatchResultContext>()))
                .ThrowsAsync(new InvalidOperationException("Step failed"));

            var act = () => _pipeline.ExecuteAsync(request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Step failed");
            (await GameWinnersAsync(request.MatchId)).Should().AllSatisfy(winner => winner.Should().BeNull());
            _signalRMock.Verify(s => s.BroadcastGameUpdated(It.IsAny<GameUpdatedEvent>()), Times.Never);
        }

        [Theory]
        [InlineData(2, 2)]
        [InlineData(1, 1)]
        [InlineData(3, 0)]
        [InlineData(0, 0)]
        [InlineData(2, -1)]
        public async Task Should_Reject_When_ResultDoesNotEndTheMatch(int teamAWins, int teamBWins)
        {
            var request = await SeedMatchAsync() with { TeamAWins = teamAWins, TeamBWins = teamBWins };

            var act = () => _pipeline.ExecuteAsync(request);

            await act.Should().ThrowAsync<ValidationException>();
            (await GameWinnersAsync(request.MatchId)).Should().AllSatisfy(winner => winner.Should().BeNull());
        }

        [Fact]
        public async Task Should_Reject_When_MatchAlreadyConfirmed()
        {
            var request = await SeedMatchAsync() with { TeamAWins = 2, TeamBWins = 0 };
            await _pipeline.ExecuteAsync(request);
            var reloaded = await DbContext.Matches.AsNoTracking().SingleAsync(m => m.Id == request.MatchId);
            DbContext.ChangeTracker.Clear();

            var act = () => _pipeline.ExecuteAsync(request with { TeamAWins = 0, TeamBWins = 2, Version = reloaded.Version });

            await act.Should().ThrowAsync<ConflictException>().WithMessage("Match is already confirmed.");
        }

        [Fact]
        public async Task Should_Reject_When_VersionIsStale()
        {
            var request = await SeedMatchAsync() with { TeamAWins = 2, TeamBWins = 0 };

            var act = () => _pipeline.ExecuteAsync(request with { Version = request.Version + 1 });

            await act.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task Should_Reject_When_TournamentDoesNotAllowScoring()
        {
            var request = await SeedMatchAsync(TournamentStatus.GroupsCompleted) with { TeamAWins = 2, TeamBWins = 0 };

            var act = () => _pipeline.ExecuteAsync(request);

            await act.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task Should_Reject_When_IdsDoNotBelongTogether()
        {
            var request = await SeedMatchAsync() with { TeamAWins = 2, TeamBWins = 0 };

            var act = () => _pipeline.ExecuteAsync(request with { StandingId = request.StandingId + 99 });

            await act.Should().ThrowAsync<ValidationException>();
        }
    }
}
