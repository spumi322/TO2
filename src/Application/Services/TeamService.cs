using Application.Contracts;
using Application.Contracts.Repositories;
using Application.DTOs.Team;
using AutoMapper;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Exceptions;
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
        private readonly ISignalRService _signalRService;
        private readonly ITenantService _tenantService;

        public TeamService(IRepository<Team> teamRepository,
                           IRepository<Tournament> tournamentRepository,
                           ITournamentTeamRepository tournamentTeamRepository,
                           IMapper mapper,
                           ILogger<TeamService> logger,
                           IUnitOfWork unitOfWork,
                           ISignalRService signalRService,
                           ITenantService tenantService)
        {
            _teamRepository = teamRepository;
            _tournamentRepository = tournamentRepository;
            _tournamentTeamRepository = tournamentTeamRepository;
            _mapper = mapper;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _signalRService = signalRService;
            _tenantService = tenantService;
        }

        public async Task<CreateTeamResponseDTO> CreateTeamAsync(CreateTeamRequestDTO request)
        {
            var team = _mapper.Map<Team>(request);

            await _teamRepository.AddAsync(team);
            await _unitOfWork.SaveChangesAsync();

            return new CreateTeamResponseDTO(team.Id);
        }

        public async Task<List<GetAllTeamsResponseDTO>> GetAllTeamsAsync()
        {
            var teams = await _teamRepository.GetAllAsync();

            return _mapper.Map<List<GetAllTeamsResponseDTO>>(teams);
        }

        public async Task<GetTeamResponseDTO> GetTeamAsync(long teamId)
        {
            var existingTeam = await _teamRepository.GetByIdAsync(teamId)
                ?? throw new NotFoundException("Team", teamId);

            return _mapper.Map<GetTeamResponseDTO>(existingTeam);
        }

        public async Task<UpdateTeamResponseDTO> UpdateTeamAsync(long id, UpdateTeamRequestDTO request)
        {
            var existingTeam = await _teamRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Team", id);

            _mapper.Map(request, existingTeam);

            await _teamRepository.UpdateAsync(existingTeam);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<UpdateTeamResponseDTO>(existingTeam);
        }

        public async Task DeleteTeamAsync(long teamId)
        {
            await _teamRepository.DeleteAsync(teamId);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<AddTeamToTournamentResponseDTO> AddTeamToTournamentAsync(AddTeamToTournamentRequestDTO request)
        {
            // Validate that both team and tournament exist
            var team = await _teamRepository.GetByIdAsync(request.TeamId)
                ?? throw new NotFoundException("Team", request.TeamId);
            var tournament = await _tournamentRepository.GetByIdAsync(request.TournamentId)
                ?? throw new NotFoundException("Tournament", request.TournamentId);

            // Check registration open
            if (!tournament.IsRegistrationOpen)
                throw new ForbiddenException("Tournament registration is closed");

            if (await _tournamentTeamRepository.ExistsInTournamentAsync(request.TeamId, request.TournamentId))
                throw new ConflictException("Team is already in the tournament!");

            if (await _tournamentTeamRepository.HasTeamWithNameAsync(request.TournamentId, team.Name))
                throw new ConflictException($"A team with the name '{team.Name}' is already registered in this tournament");

            var currentParticipantsCount = await _tournamentTeamRepository.GetCountByTournamentAsync(request.TournamentId);

            if (currentParticipantsCount >= tournament.MaxTeams)
                throw new ValidationException("Tournament is at maximum capacity");

            // Create and save
            var tournamentTeam = new TournamentTeam(request.TournamentId, request.TeamId);

            await _tournamentTeamRepository.AddAsync(tournamentTeam);
            await _unitOfWork.SaveChangesAsync();

            await _signalRService.BroadcastTeamAdded(request.TournamentId, request.TeamId, _tenantService.GetCurrentUserName());

            return new AddTeamToTournamentResponseDTO(request.TournamentId, request.TeamId);
        }

        public async Task RemoveTeamFromTournamentAsync(long teamId, long tournamentId)
        {
            var existingTournament = await _tournamentRepository.GetByIdAsync(tournamentId)
                ?? throw new NotFoundException("Tournament", tournamentId);

            // Check if registration is still open
            if (!existingTournament.IsRegistrationOpen)
                throw new ForbiddenException("Cannot remove teams after tournament has started");

            var tournamentTeam = await _tournamentTeamRepository.GetByTeamAndTournamentAsync(teamId, tournamentId)
                ?? throw new NotFoundException("Team is not registered in this tournament");

            await _tournamentTeamRepository.DeleteAsync(tournamentTeam);
            await _unitOfWork.SaveChangesAsync();

            await _signalRService.BroadcastTeamRemoved(tournamentId, teamId, _tenantService.GetCurrentUserName());
        }
    }
}
