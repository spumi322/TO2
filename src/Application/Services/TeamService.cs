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
        private readonly ILogger<TeamService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISignalRService _signalRService;
        private readonly ITenantService _tenantService;
        private readonly ITournamentStateMachine _stateMachine;

        public TeamService(IRepository<Team> teamRepository,
                           IRepository<Tournament> tournamentRepository,
                           ILogger<TeamService> logger,
                           IUnitOfWork unitOfWork,
                           ISignalRService signalRService,
                           ITenantService tenantService,
                           ITournamentStateMachine stateMachine)
        {
            _teamRepository = teamRepository;
            _tournamentRepository = tournamentRepository;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _signalRService = signalRService;
            _tenantService = tenantService;
            _stateMachine = stateMachine;
        }

        // All or nothing: every name becomes a new team of this tournament, one save
        public async Task<List<GetTeamResponseDTO>> AddTeamsToTournamentAsync(long tournamentId, AddTeamsRequestDTO request)
        {
            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId)
                ?? throw new NotFoundException("Tournament", tournamentId);

            if (!_stateMachine.CanModifyTeams(tournament.Status))
                throw new ConflictException("Tournament registration is closed.");

            var names = request.Names.Select(n => n.Trim()).ToList();
            var registeredNames = (await _teamRepository.FindAllAsync(t => t.TournamentId == tournamentId))
                .Select(t => t.Name)
                .ToList();

            var freeSlots = tournament.MaxTeams - registeredNames.Count;
            if (freeSlots <= 0)
                throw new ConflictException("Tournament is full.");
            if (names.Count > freeSlots)
                throw new ConflictException($"Only {freeSlots} {(freeSlots == 1 ? "place" : "places")} left.");

            var alreadyRegistered = names
                .Where(n => registeredNames.Contains(n, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (alreadyRegistered.Count > 0)
                throw new ConflictException($"Already registered: {string.Join(", ", alreadyRegistered)}.");

            var teams = names.Select(n => new Team(n, tournamentId)).ToList();
            await _teamRepository.AddRangeAsync(teams);

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
                throw new ConflictException("Teams cannot be removed once the tournament has started.");

            var team = await _teamRepository.FindAsync(t => t.Id == teamId && t.TournamentId == tournamentId)
                ?? throw new NotFoundException("Team is not registered in this tournament");

            await _teamRepository.DeleteAsync(team);

            _tournamentRepository.BumpVersion(existingTournament);
            await _unitOfWork.SaveChangesAsync();

            await _signalRService.BroadcastTeamRemoved(tournamentId, teamId, _tenantService.GetCurrentUserName());
        }
    }
}
