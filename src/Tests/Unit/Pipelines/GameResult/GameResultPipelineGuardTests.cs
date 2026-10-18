using Application.Contracts;
using Application.DTOs.Game;
using Application.Pipelines.GameResult;
using Application.Pipelines.GameResult.Contracts;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.StateMachine;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repository;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Base;
using Tests.Helpers;
using Match = Domain.AggregateRoots.Match;

namespace Tests.Unit.Pipelines.GameResult
{
    public class GameResultPipelineGuardTests : IntegrationTestBase
    {
        private readonly Mock<IGameResultPipelineStep> _stepMock = new();
        private readonly GameResultPipeline _pipeline;

        public GameResultPipelineGuardTests()
        {
            // Step stops the pipeline right away; we only care whether it is reached
            _stepMock.Setup(s => s.ExecuteAsync(It.IsAny<GameResultContext>())).ReturnsAsync(false);

            _pipeline = new GameResultPipeline(
                new Mock<ILogger<GameResultPipeline>>().Object,
                new Repository<Tournament>(DbContext),
                new Repository<Standing>(DbContext),
                new Repository<Match>(DbContext),
                new Repository<Game>(DbContext),
                new TournamentStateMachine(),
                new[] { _stepMock.Object },
                new UnitOfWork(DbContext),
                new Mock<ISignalRService>().Object,
                new Mock<ITenantService>().Object,
                new Mock<IStandingService>().Object);

            DbContext.Tenants.Add(new Tenant { Id = 1, Name = "Org1" });
            DbContext.SaveChanges();
        }

        private async Task<SetGameResultDTO> SeedGameAsync(TournamentStatus status)
        {
            var tournament = TestDataBuilder.Tournaments.CreateInStatus(status, name: "Summer Cup");
            DbContext.Tournaments.Add(tournament);
            await DbContext.SaveChangesAsync();

            var standing = TestDataBuilder.Standings.CreateBracket(tournament.Id);
            DbContext.Standings.Add(standing);
            await DbContext.SaveChangesAsync();

            var match = TestDataBuilder.Matches.CreateWithoutTeams(standing.Id);
            DbContext.Matches.Add(match);
            await DbContext.SaveChangesAsync();

            var game = TestDataBuilder.Games.Create(match, 1, 2);
            DbContext.Games.Add(game);
            await SaveAndDetachAsync();

            return new SetGameResultDTO(game.Id, 1, null, null, match.Id, standing.Id, tournament.Id, game.Version);
        }

        [Theory]
        [InlineData(TournamentStatus.Setup)]
        [InlineData(TournamentStatus.GroupsCompleted)]
        [InlineData(TournamentStatus.Finished)]
        [InlineData(TournamentStatus.Cancelled)]
        public async Task Should_Reject_When_StatusDoesNotAllowScoring(TournamentStatus status)
        {
            var request = await SeedGameAsync(status);

            var act = () => _pipeline.ExecuteAsync(request);

            await act.Should().ThrowAsync<ConflictException>();
            _stepMock.Verify(s => s.ExecuteAsync(It.IsAny<GameResultContext>()), Times.Never);
        }

        [Theory]
        [InlineData(TournamentStatus.GroupsInProgress)]
        [InlineData(TournamentStatus.BracketInProgress)]
        public async Task Should_RunSteps_When_StatusAllowsScoring(TournamentStatus status)
        {
            var request = await SeedGameAsync(status);

            await _pipeline.ExecuteAsync(request);

            _stepMock.Verify(s => s.ExecuteAsync(It.IsAny<GameResultContext>()), Times.Once);
        }

        [Fact]
        public async Task Should_Reject_When_TournamentIdDoesNotOwnGame()
        {
            // Cancelled tournament's game sent with an active tournament's ID
            var cancelledGame = await SeedGameAsync(TournamentStatus.Cancelled);
            var activeGame = await SeedGameAsync(TournamentStatus.BracketInProgress);

            var act = () => _pipeline.ExecuteAsync(cancelledGame with { TournamentId = activeGame.TournamentId });

            await act.Should().ThrowAsync<ValidationException>();
            _stepMock.Verify(s => s.ExecuteAsync(It.IsAny<GameResultContext>()), Times.Never);
        }

        [Fact]
        public async Task Should_Reject_When_MatchIdDoesNotOwnGame()
        {
            var request = await SeedGameAsync(TournamentStatus.GroupsInProgress);
            var other = await SeedGameAsync(TournamentStatus.GroupsInProgress);

            var act = () => _pipeline.ExecuteAsync(request with { MatchId = other.MatchId });

            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task Should_Reject_When_GameVersionStale()
        {
            // Another admin scored the game after this client loaded it
            var request = await SeedGameAsync(TournamentStatus.GroupsInProgress);
            var game = await DbContext.Games.FindAsync(request.gameId);
            game!.WinnerId = 2;
            await SaveAndDetachAsync();

            var act = () => _pipeline.ExecuteAsync(request);

            await act.Should().ThrowAsync<ConflictException>();
            _stepMock.Verify(s => s.ExecuteAsync(It.IsAny<GameResultContext>()), Times.Never);
        }
    }
}
