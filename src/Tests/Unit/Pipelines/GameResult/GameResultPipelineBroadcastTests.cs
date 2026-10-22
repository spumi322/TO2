using Application.Contracts;
using Application.DTOs.Game;
using Application.DTOs.SignalR;
using Application.Pipelines.GameResult;
using Application.Pipelines.GameResult.Contracts;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using Domain.StateMachine;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repository;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Base;
using Tests.Helpers;
using Match = Domain.AggregateRoots.Match;

namespace Tests.Unit.Pipelines.GameResult
{
    public class GameResultPipelineBroadcastTests : IntegrationTestBase
    {
        private readonly Mock<ISignalRService> _signalRMock = new();

        public GameResultPipelineBroadcastTests()
        {
            DbContext.Tenants.Add(new Tenant { Id = 1, Name = "Org1" });
            DbContext.SaveChanges();
        }

        // One fake step stands in for the real ones: it only sets what the pipeline reports (status change, finished group/round)
        private GameResultPipeline CreatePipeline(Action<GameResultContext> outcome)
        {
            var step = new Mock<IGameResultPipelineStep>();
            step.Setup(s => s.ExecuteAsync(It.IsAny<GameResultContext>()))
                .Callback(outcome)
                .ReturnsAsync(true);

            return new GameResultPipeline(
                new Mock<ILogger<GameResultPipeline>>().Object,
                new Repository<Tournament>(DbContext),
                new Repository<Standing>(DbContext),
                new Repository<Match>(DbContext),
                new Repository<Game>(DbContext),
                new TournamentStateMachine(),
                new[] { step.Object },
                new UnitOfWork(DbContext),
                _signalRMock.Object,
                new Mock<ITenantService>().Object,
                new Mock<IStandingService>().Object);
        }

        private async Task<SetGameResultDTO> SeedGameAsync()
        {
            var tournament = TestDataBuilder.Tournaments.CreateInStatus(TournamentStatus.BracketInProgress);
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

        [Fact]
        public async Task Should_BroadcastTournamentUpdated_When_StatusChanged()
        {
            var request = await SeedGameAsync();

            await CreatePipeline(c => c.NewTournamentStatus = TournamentStatus.Finished).ExecuteAsync(request);

            _signalRMock.Verify(s => s.BroadcastGameUpdated(It.IsAny<GameUpdatedEvent>()), Times.Once);
            _signalRMock.Verify(s => s.BroadcastTournamentUpdated(request.TournamentId, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Should_BroadcastTournamentUpdated_When_GroupFinished()
        {
            var request = await SeedGameAsync();

            await CreatePipeline(c => c.StandingFinished = true).ExecuteAsync(request);

            _signalRMock.Verify(s => s.BroadcastTournamentUpdated(request.TournamentId, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Should_BroadcastTournamentUpdated_When_BracketRoundFinished()
        {
            var request = await SeedGameAsync();

            await CreatePipeline(c => c.RoundFinished = true).ExecuteAsync(request);

            _signalRMock.Verify(s => s.BroadcastTournamentUpdated(request.TournamentId, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Should_NotBroadcastTournamentUpdated_When_NothingFinished()
        {
            var request = await SeedGameAsync();

            await CreatePipeline(_ => { }).ExecuteAsync(request);

            _signalRMock.Verify(s => s.BroadcastGameUpdated(It.IsAny<GameUpdatedEvent>()), Times.Once);
            _signalRMock.Verify(s => s.BroadcastTournamentUpdated(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
        }
    }
}
