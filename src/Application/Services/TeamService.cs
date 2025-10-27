using Application.Contracts;
using Application.Contracts.Repositories;
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
        private readonly IRepository<Team> _teamRepository;
        private readonly IRepository<Tournament> _tournamentRepository;
        private readonly ITournamentTeamRepository _tournamentTeamRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<TeamService> _logger;

        private readonly IUnitOfWork _unitOfWork;

        public TeamService(IRepository<Team> teamRepository,
                           IRepository<Tournament> tournamentRepository,
                           ITournamentTeamRepository tournamentTeamRepository,
                           IMapper mapper,
                           ILogger<TeamService> logger,
                                 IUnitOfWork unitOfWork)
        {
            _teamRepository = teamRepository;
            _tournamentRepository = tournamentRepository;
            _tournamentTeamRepository = tournamentTeamRepository;
            _mapper = mapper;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<CreateTeamResponseDTO> CreateTeamAsync(CreateTeamRequestDTO request)
        {
            try
            {
                var team = _mapper.Map<Team>(request);

                await _teamRepository.AddAsync(team);
                await _unitOfWork.SaveChangesAsync();

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
            var teams = await _teamRepository.GetAllAsync();

            return _mapper.Map<List<GetAllTeamsResponseDTO>>(teams);
        }

        public async Task<GetTeamResponseDTO> GetTeamAsync(long teamId)
        {
            var existingTeam = await _teamRepository.GetByIdAsync(teamId);

            return _mapper.Map<GetTeamResponseDTO>(existingTeam) ?? throw new Exception("Team not found");
        }

        public async Task<UpdateTeamResponseDTO> UpdateTeamAsync(long id, UpdateTeamRequestDTO request)
        {
            var existingTeam = await _teamRepository.GetByIdAsync(id) ?? throw new Exception("Team not found");

            try
            {
                _mapper.Map(request, existingTeam);

                await _teamRepository.UpdateAsync(existingTeam);
                await _unitOfWork.SaveChangesAsync();

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
                await _teamRepository.DeleteAsync(teamId);
                await _unitOfWork.SaveChangesAsync();
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
            var team = await _teamRepository.GetByIdAsync(request.TeamId)
                ?? throw new Exception("Team not found");
            var tournament = await _tournamentRepository.GetByIdAsync(request.TournamentId)
                ?? throw new Exception("Tournament not found");

            // Check registration open
            if (!tournament.IsRegistrationOpen)
                throw new Exception("Tournament registration is closed");

            if (await _tournamentTeamRepository.ExistsInTournamentAsync(request.TeamId, request.TournamentId))
                throw new Exception("Team is already in the tournament!");

            if (await _tournamentTeamRepository.HasTeamWithNameAsync(request.TournamentId, team.Name))
                throw new Exception($"A team with the name '{team.Name}' is already registered in this tournament");

            var currentParticipantsCount = await _tournamentTeamRepository.GetCountByTournamentAsync(request.TournamentId);

            if (currentParticipantsCount >= tournament.MaxTeams)
                throw new Exception("Tournament is at maximum capacity");

            // Create and save
            var tournamentTeam = new TournamentTeam(request.TournamentId, request.TeamId);

            await _tournamentTeamRepository.AddAsync(tournamentTeam);
            await _unitOfWork.SaveChangesAsync();

            return new AddTeamToTournamentResponseDTO(request.TournamentId, request.TeamId);
        }

        public async Task RemoveTeamFromTournamentAsync(long teamId, long tournamentId)
        {
            var existingTournament = await _tournamentRepository.GetByIdAsync(tournamentId) ?? throw new Exception("Tournament not found");

            // Check if registration is still open
            if (!existingTournament.IsRegistrationOpen)
                throw new Exception("Cannot remove teams after tournament has started");

            try
            {
                var tournamentTeam = await _tournamentTeamRepository.GetByTeamAndTournamentAsync(teamId, tournamentId)
                    ?? throw new Exception("Team is not registered in this tournament");

                await _tournamentTeamRepository.DeleteAsync(tournamentTeam);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing team from tournament: {Message}", ex.Message);
                throw;
            }
        }
    }
}
