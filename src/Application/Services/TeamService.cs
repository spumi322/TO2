using Application.Contracts;
using Application.Contracts.Repositories;
using Application.DTOs.Team;
using Application.Mappings;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Exceptions;
using Domain.StateMachine;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class TeamService : ITeamService
    {
        private readonly IRepository<Team> _teamRepository;
        private readonly IRepository<Tournament> _tournamentRepository;
        private readonly ITournamentTeamRepository _tournamentTeamRepository;
        private readonly ILogger<TeamService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISignalRService _signalRService;
        private readonly ITenantService _tenantService;
        private readonly ITournamentStateMachine _stateMachine;

        public TeamService(IRepository<Team> teamRepository,
                           IRepository<Tournament> tournamentRepository,
                           ITournamentTeamRepository tournamentTeamRepository,
                           ILogger<TeamService> logger,
                           IUnitOfWork unitOfWork,
                           ISignalRService signalRService,
                           ITenantService tenantService,
                           ITournamentStateMachine stateMachine)
        {
            _teamRepository = teamRepository;
            _tournamentRepository = tournamentRepository;
            _tournamentTeamRepository = tournamentTeamRepository;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _signalRService = signalRService;
            _tenantService = tenantService;
            _stateMachine = stateMachine;
        }

        public async Task<UpdateTeamResponseDTO> UpdateTeamAsync(long id, UpdateTeamRequestDTO request)
        {
            var existingTeam = await _teamRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Team", id);

            if (existingTeam.Version != request.Version)
                throw new ConflictException("Team was modified by another user. Reload and try again.");

            existingTeam.Rename(request.Name);

            await _unitOfWork.SaveChangesAsync();

            return existingTeam.ToUpdateTeamResponseDTO();
        }

        public async Task DeleteTeamAsync(long teamId)
        {
            await _teamRepository.DeleteAsync(teamId);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RemoveTeamFromTournamentAsync(long teamId, long tournamentId)
        {
            var existingTournament = await _tournamentRepository.GetByIdAsync(tournamentId)
                ?? throw new NotFoundException("Tournament", tournamentId);

            if (!_stateMachine.CanModifyTeams(existingTournament.Status))
                throw new ConflictException("Cannot remove teams after tournament has started");

            var tournamentTeam = await _tournamentTeamRepository.GetByTeamAndTournamentAsync(teamId, tournamentId)
                ?? throw new NotFoundException("Team is not registered in this tournament");

            await _tournamentTeamRepository.DeleteAsync(tournamentTeam);
            await _unitOfWork.SaveChangesAsync();

            await _signalRService.BroadcastTeamRemoved(tournamentId, teamId, _tenantService.GetCurrentUserName());
        }
    }
}
