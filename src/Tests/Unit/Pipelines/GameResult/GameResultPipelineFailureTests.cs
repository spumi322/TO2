using Application.Contracts;
using Application.DTOs.Game;
using Application.Pipelines.GameResult;
using Application.Pipelines.GameResult.Contracts;
using Application.Pipelines.GameResult.Steps;
using Application.Services;
using AutoMapper;
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
    public class GameResultPipelineFailureTests : IntegrationTestBase
    {
        private readonly Mock<ISignalRService> _signalRMock = new();
        private readonly GameResultPipeline _pipeline;

        public GameResultPipelineFailureTests()
        {
            var gameService = new GameService(
                new Repository<Game>(DbContext),
                new Repository<Match>(DbContext),
                new FormatService(),
                new Mock<ILogger<GameService>>().Object);

            _pipeline = new GameResultPipeline(
                new Mock<ILogger<GameResultPipeline>>().Object,
                new Repository<Tournament>(DbContext),
                new Repository<Standing>(DbContext),
                new Repository<Match>(DbContext),
                new Repository<Game>(DbContext),
                new TournamentStateMachine(),
                new IGameResultPipelineStep[] { new ScoreGameStep(new Mock<ILogger<ScoreGameStep>>().Object, gameService) },
                new UnitOfWork(DbContext),
                _signalRMock.Object,
                new Mock<ITenantService>().Object,
                new Mock<IMapper>().Object,
                new Mock<IStandingService>().Object);

            DbContext.Tenants.Add(new Tenant { Id = 1, Name = "Org1" });
            DbContext.SaveChanges();
        }

        [Fact]
        public async Task Should_ThrowAndNotBroadcast_When_StepFails()
        {
            // Match already decided: ScoreGameStep throws ConflictException
            var tournament = TestDataBuilder.Tournaments.CreateInStatus(TournamentStatus.BracketInProgress);
            DbContext.Tournaments.Add(tournament);
            await DbContext.SaveChangesAsync();

            var standing = TestDataBuilder.Standings.CreateBracket(tournament.Id);
            DbContext.Standings.Add(standing);
            await DbContext.SaveChangesAsync();

            var match = TestDataBuilder.Matches.CreateWithoutTeams(standing.Id);
            match.WinnerId = 1;
            DbContext.Matches.Add(match);
            await DbContext.SaveChangesAsync();

            var game = TestDataBuilder.Games.Create(match, 1, 2);
            DbContext.Games.Add(game);
            await SaveAndDetachAsync();

            var request = new SetGameResultDTO(game.Id, 1, null, null, match.Id, standing.Id, tournament.Id, game.Version);

            var act = () => _pipeline.ExecuteAsync(request);

            await act.Should().ThrowAsync<ConflictException>().WithMessage("Match already has a winner");
            _signalRMock.Verify(s => s.BroadcastGameUpdated(It.IsAny<Application.DTOs.SignalR.GameUpdatedEvent>()), Times.Never);
        }
    }
}
