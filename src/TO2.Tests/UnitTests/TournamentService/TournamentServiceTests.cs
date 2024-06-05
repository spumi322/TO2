using Xunit;
using Moq;
using Application.Services;
using Application.Contracts;
using Application.DTOs.Tournament;
using AutoMapper;
using Domain.AggregateRoots;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Domain.ValueObjects;
using Application.DTOs.Team;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace TO2.Tests.UnitTests
{
    public class TournamentServiceTests
    {
        private readonly TournamentService _tournamentService;
        private readonly Mock<IGenericRepository<Tournament>> _tournamentRepositoryMock;
        private readonly Mock<ITeamService> _teamServiceMock;
        private readonly Mock<ITO2DbContext> _dbContextMock;
        private readonly Mock<IStandingService> _standingServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<TournamentService>> _loggerMock;

        public TournamentServiceTests()
        {
            _tournamentRepositoryMock = new Mock<IGenericRepository<Tournament>>();
            _teamServiceMock = new Mock<ITeamService>();
            _dbContextMock = new Mock<ITO2DbContext>();
            _standingServiceMock = new Mock<IStandingService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<TournamentService>>();

            _tournamentService = new TournamentService(
                _tournamentRepositoryMock.Object,
                _teamServiceMock.Object,
                _dbContextMock.Object,
                _standingServiceMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task CreateTournamentAsync_WhenCalled_ShouldCreateTournament()
        {
            // Arrange
            var request = new CreateTournamentRequestDTO(
                Name: "Test Tournament",
                Description: "Test Description",
                MaxTeams: 16,
                StartDate: DateTime.UtcNow,
                EndDate: DateTime.UtcNow.AddDays(1),
                Format: Format.BracketAndGroup,
                TeamsPerBracket: 8,
                TeamsPerGroup: 4
            );

            var tournament = new Tournament(
                request.Name,
                request.Description,
                request.MaxTeams,
                request.StartDate,
                request.EndDate,
                request.Format,
                TournamentStatus.Upcoming);

            _mapperMock.Setup(m => m.Map<Tournament>(request)).Returns(tournament);

            // Act
            await _tournamentService.CreateTournamentAsync(request);

            // Assert
            _tournamentRepositoryMock.Verify(r => r.Add(tournament), Times.Once);
            _tournamentRepositoryMock.Verify(r => r.Save(), Times.Once);
            _standingServiceMock.Verify(s => s.GenerateStanding(tournament.Id, "Main Bracket", StandingType.Bracket, request.TeamsPerBracket), Times.Once);

            for (int i = 0; i < (tournament.MaxTeams / request.TeamsPerGroup); i++)
            {
                _standingServiceMock.Verify(s => s.GenerateStanding(tournament.Id, $"Group {i + 1}", StandingType.Group, request.TeamsPerGroup), Times.Once);
            }
        }

        [Fact]
        public async Task AddTeamToTournamentAsync_ShouldAddTeam_WhenValidInput()
        {
            // Arrange
            var teamId = 1;
            var tournamentId = 1;

            var team = new Team("Test Team");
            team.Id = teamId;
            var tournament = new Tournament("Test Tournament", "Test Description", 16, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), Format.BracketOnly, TournamentStatus.Upcoming);
            tournament.Id = tournamentId;

            _teamServiceMock.Setup(t => t.GetTeamAsync(teamId)).ReturnsAsync(new GetTeamResponseDTO(1, "Test Team"));

            _tournamentRepositoryMock.Setup(r => r.Get(tournamentId)).ReturnsAsync(tournament);

            // Act
            await _tournamentService.AddTeamToTournamentAsync(teamId, tournamentId);

            // Assert
            Assert.Equal(teamId, tournament.TeamsTournaments[0].TeamId);
            Assert.Equal(tournamentId, tournament.TeamsTournaments[0].TournamentId);
        }

        //[Fact]
        //public async Task AddTeamToTournamentAsync_ShouldThrowException_WhenTeamAlreadyAdded()
        //{
        //    // Arrange
        //    var teamId = 1;
        //    var tournamentId = 1;

        //    var team = new Team("Test Team");
        //    var tournament = new Tournament("Test Tournament", "Test Description", 16, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), Format.BracketOnly, TournamentStatus.Upcoming);
        //    tournament.TeamsTournaments.Add(new TeamsTournaments(tournament, team));

        //    _teamServiceMock.Setup(t => t.GetTeamAsync(teamId)).ReturnsAsync(new GetTeamResponseDTO(1, "Test Team"));

        //    _tournamentRepositoryMock.Setup(r => r.Get(tournamentId)).ReturnsAsync(tournament);

        //    // Act
        //    var ex = await Assert.ThrowsAsync<Exception>(() => _tournamentService.AddTeamToTournamentAsync(teamId, tournamentId));

        //    // Assert
        //    Assert.Equal("Team already added to tournament", ex.Message);
        //}
    }
}
