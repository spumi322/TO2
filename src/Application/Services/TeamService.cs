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

        // All or nothing: every name becomes a new team (teams are not reused), one save
        public async Task<List<GetTeamResponseDTO>> AddTeamsToTournamentAsync(long tournamentId, AddTeamsRequestDTO request)
        {
            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId)
                ?? throw new NotFoundException("Tournament", tournamentId);

            if (!_stateMachine.CanModifyTeams(tournament.Status))
                throw new ConflictException("Tournament registration is closed.");

            var names = request.Names.Select(n => n.Trim()).ToList();
            var registeredNames = await _tournamentTeamRepository.GetTeamNamesAsync(tournamentId);

            var freeSlots = tournament.MaxTeams - registeredNames.Count;
            if (freeSlots <= 0)
                throw new ConflictException("Tournament is full.");
            if (names.Count > freeSlots)
                throw new ConflictException($"Only {freeSlots} slots left.");

            var alreadyRegistered = names
                .Where(n => registeredNames.Contains(n, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (alreadyRegistered.Count > 0)
                throw new ConflictException($"Already registered: {string.Join(", ", alreadyRegistered)}.");

            var teams = names.Select(n => new Team(n)).ToList();
            await _teamRepository.AddRangeAsync(teams);
            await _tournamentTeamRepository.AddRangeAsync(teams.Select(t => new TournamentTeam(tournamentId, t)));

            // Two batches at once: the second save fails on the version (409) instead of exceeding MaxTeams
            _tournamentRepository.BumpVersion(tournament);
            await _unitOfWork.SaveChangesAsync();

            await _signalRService.BroadcastTeamAdded(tournamentId, teams.Select(t => t.Id).ToList(), _tenantService.GetCurrentUserName());

            return teams.Select(t => t.ToGetTeamResponseDTO()).ToList();
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

            // Same rule as tournament delete: a team in no other tournament is deleted too
            var inOtherTournament = await _tournamentTeamRepository
                .ExistsAsync(tt => tt.TeamId == teamId && tt.TournamentId != tournamentId);
            if (!inOtherTournament)
                await _teamRepository.DeleteAsync(teamId);

            _tournamentRepository.BumpVersion(existingTournament);
            await _unitOfWork.SaveChangesAsync();

            await _signalRService.BroadcastTeamRemoved(tournamentId, teamId, _tenantService.GetCurrentUserName());
        }
    }
}
