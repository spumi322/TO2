using Application.Contracts;
using Application.DTOs.Team;
using AutoMapper;
using Domain.AggregateRoots;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class TeamService : ITeamService
    {
        private readonly IGenericRepository<Team> _teamRepository;
        private readonly IGenericRepository<Tournament> _tournamentRepository;
        private readonly IGenericRepository<TournamentTeam> _tournamentTeamRepository;
        private readonly ITO2DbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<TeamService> _logger;

        public TeamService(IGenericRepository<Team> teamRepository,
                           IGenericRepository<Tournament> tournamentRepository,
                           IGenericRepository<TournamentTeam> tournamentTeamRepository,
                           ITO2DbContext dbContext,
                           IMapper mapper,
                           ILogger<TeamService> logger)
        {
            _teamRepository = teamRepository;
            _tournamentRepository = tournamentRepository;
            _tournamentTeamRepository = tournamentTeamRepository;
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<CreateTeamResponseDTO> CreateTeamAsync(CreateTeamRequestDTO request)
        {
            try
            {
                var team = _mapper.Map<Team>(request);

                await _teamRepository.Add(team);
                await _teamRepository.Save();

                return new CreateTeamResponseDTO(team.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving team: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<List<GetAllTeamsResponseDTO>> GetAllTeamsAsync()
        {
            var teams = await _teamRepository.GetAll();

            return _mapper.Map<List<GetAllTeamsResponseDTO>>(teams);
        }

        public async Task<List<GetTeamWithStatsResponseDTO>> GetTeamsWithStatsAsync(long standingId)
        {
            var participants = await _dbContext.GroupEntries
                .Where(tp => tp.StandingId == standingId)
                .ToListAsync();

            return _mapper.Map<List<GetTeamWithStatsResponseDTO>>(participants) ?? throw new Exception("Participants not found");
        }

        public async Task<GetTeamResponseDTO> GetTeamAsync(long teamId)
        {
            var existingTeam = await _teamRepository.Get(teamId);

            return _mapper.Map<GetTeamResponseDTO>(existingTeam) ?? throw new Exception("Team not found");
        }

        public async Task<UpdateTeamResponseDTO> UpdateTeamAsync(long id, UpdateTeamRequestDTO request)
        {
            var existingTeam = await _teamRepository.Get(id) ?? throw new Exception("Team not found");

            try
            {
                _mapper.Map(request, existingTeam);

                await _teamRepository.Update(existingTeam);
                await _teamRepository.Save();

                return _mapper.Map<UpdateTeamResponseDTO>(existingTeam);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating team: {Message}", ex.Message);
                throw;
            }
        }

        public async Task DeleteTeamAsync(long teamId)
        {
            try
            {
                await _teamRepository.Delete(teamId);
                await _teamRepository.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting team: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<AddTeamToTournamentResponseDTO> AddTeamToTournamentAsync(AddTeamToTournamentRequestDTO request)
        {
            // Validate that both team and tournament exist
            var team = await _teamRepository.Get(request.TeamId)
                ?? throw new Exception("Team not found");
            var tournament = await _tournamentRepository.Get(request.TournamentId)
                ?? throw new Exception("Tournament not found");

            // Check registration open
            if (!tournament.IsRegistrationOpen)
                throw new Exception("Tournament registration is closed");

            // Check if team already in tournament
            var existingEntry = await _dbContext.TournamentTeams
                .Where(tt => tt.TournamentId == request.TournamentId && tt.TeamId == request.TeamId)
                .FirstOrDefaultAsync();

            if (existingEntry != null) throw new Exception("Team is already in the tournament!");

            // Check unique team name constraint
            var sameNameTeamExists = await _dbContext.TournamentTeams
                .Where(tt => tt.TournamentId == request.TournamentId)
                .Join(_dbContext.Teams, tt => tt.TeamId, t => t.Id, (tt, t) => t)
                .AnyAsync(t => t.Name.ToLower() == team.Name.ToLower());

            if (sameNameTeamExists)
                throw new Exception($"A team with the name '{team.Name}' is already registered in this tournament");

            // Check capacity
            var currentParticipantsCount = await _dbContext.TournamentTeams
                .CountAsync(tt => tt.TournamentId == request.TournamentId);

            if (currentParticipantsCount >= tournament.MaxTeams)
                throw new Exception("Tournament is at maximum capacity");

            // Create and save
            var tournamentTeam = new TournamentTeam(request.TournamentId, request.TeamId);

            await _tournamentTeamRepository.Add(tournamentTeam);
            await _tournamentTeamRepository.Save();

            return new AddTeamToTournamentResponseDTO(request.TournamentId, request.TeamId);
        }

        public async Task RemoveTeamFromTournamentAsync(long teamId, long tournamentId)
        {
            var existingTournament = await _tournamentRepository.Get(tournamentId) ?? throw new Exception("Tournament not found");

            // Check if registration is still open
            if (!existingTournament.IsRegistrationOpen)
                throw new Exception("Cannot remove teams after tournament has started");

            try
            {
                // Remove from TournamentTeams table
                var tournamentTeam = await _dbContext.TournamentTeams
                    .FirstOrDefaultAsync(tt => tt.TeamId == teamId && tt.TournamentId == tournamentId);

                if (tournamentTeam == null)
                    throw new Exception("Team is not registered in this tournament");

                _dbContext.TournamentTeams.Remove(tournamentTeam);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing team from tournament: {Message}", ex.Message);
                throw;
            }
        }
    }
}
