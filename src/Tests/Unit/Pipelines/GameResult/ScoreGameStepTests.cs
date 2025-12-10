using Application.Contracts;
using Application.DTOs.Game;
using Application.Pipelines.GameResult.Contracts;
using Application.Pipelines.GameResult.Steps;
using FluentAssertions;
using Moq;
using Tests.Base;

namespace Tests.Unit.Pipelines.GameResult
{
    public class ScoreGameStepTests : UnitTestBase
    {
        private readonly Mock<IGameService> _mockGameService;
        private readonly ScoreGameStep _step;

        public ScoreGameStepTests()
        {
            var (mockLogger, logger) = CreateLogger<ScoreGameStep>();
            _mockGameService = new Mock<IGameService>();
            _step = new ScoreGameStep(logger, _mockGameService.Object);
        }

        [Fact]
        public async Task ExecuteAsync_Should_CallGameService_WithCorrectParameters()
        {
            // Arrange
            var context = new GameResultContext
            {
                GameResult = new SetGameResultDTO(
                    gameId: 1,
                    WinnerId: 10,
                    TeamAScore: 15,
                    TeamBScore: 10,
                    MatchId: 1,
                    StandingId: 1,
                    TournamentId: 1
                )
            };

            _mockGameService
                .Setup(x => x.SetGameResult(
                    It.IsAny<long>(),
                    It.IsAny<long>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _step.ExecuteAsync(context);

            // Assert
            result.Should().BeTrue();
            _mockGameService.Verify(x => x.SetGameResult(
                1,   // gameId
                10,  // winnerId
                15,  // teamAScore
                10   // teamBScore
            ), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Should_ReturnTrue_WhenGameScored()
        {
            // Arrange
            var context = new GameResultContext
            {
                GameResult = new SetGameResultDTO(1, 10, 15, 10, 1, 1, 1)
            };

            _mockGameService
                .Setup(x => x.SetGameResult(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _step.ExecuteAsync(context);

            // Assert
            result.Should().BeTrue("step should continue to next step");
        }
    }
}
