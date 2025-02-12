using Application.Contracts;
using Application.DTOs.Team;
using AutoMapper;
using Domain.AggregateRoots;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class TeamService : ITeamService
    {
        private readonly IGenericRepository<Team> _teamRepository;
        private readonly ITO2DbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<TeamService> _logger;

        public TeamService(IGenericRepository<Team> teamRepository,
                           ITO2DbContext dbContext,
                           IMapper mapper,
                           ILogger<TeamService> logger)
        {
            _teamRepository = teamRepository;
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
                _logger.LogError("Error saving team: {0}, Inner Exception: {1}", ex, ex.InnerException);

                throw new Exception(ex.Message);
            }
        }

        public async Task<List<GetAllTeamsResponseDTO>> GetAllTeamsAsync()
        {
            var teams = await _teamRepository.GetAll();

            return _mapper.Map<List<GetAllTeamsResponseDTO>>(teams);
        }

        public async Task<List<GetTeamWithStatsResponseDTO>> GetTeamsWithStatsAsync(long standingId)
        {
            var participants = await _dbContext.TournamentParticipants
                .Where(tp => tp.StandingId == standingId)
                .ToListAsync();

            return _mapper.Map<List<GetTeamWithStatsResponseDTO>>(participants) ?? throw new Exception("Participants not found");
        }

        public async Task<GetTeamResponseDTO> GetTeamAsync(long teamId)
        {
            var existingTeam = await _teamRepository.Get(teamId);

            return _mapper.Map<GetTeamResponseDTO>(existingTeam) ?? throw new Exception("Team not found");
        }

        //// DTO
        //public async Task<List<Team>> GetTeamsByTournamentAsync(long tournamentId)
        //{
        //    var teams = await _teamRepository.GetAllByFK("TournamentId", tournamentId);

        //    return teams.ToList();
        //}

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
                _logger.LogError("Error updating team: {0}, Inner Exception: {1}", ex, ex.InnerException);

                throw new Exception(ex.Message);
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
                _logger.LogError("Error deleting team: {0}, Inner Exception: {1}", ex, ex.InnerException);

                throw new Exception(ex.Message);
            }
        }
    }
}
