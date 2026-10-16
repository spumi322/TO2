using Application.Contracts;
using Application.Contracts.Repositories;
using Application.DTOs.Team;
using Application.DTOs.Tournament;
using AutoMapper;
using Domain.AggregateRoots;
using Domain.Exceptions;
using Domain.StateMachine;
using Microsoft.Extensions.Logging;


namespace Application.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly ITournamentRepository _tournamentRepository;
        private readonly IRepository<Team> _teamRepository;
        private readonly IStandingService _standingService;
        private readonly IMapper _mapper;
        private readonly ILogger<TournamentService> _logger;
        private readonly ITournamentStateMachine _stateMachine;
        private readonly ISignalRService _signalRService;
        private readonly ITenantService _tenantService;
        private readonly IUnitOfWork _unitOfWork;

        public TournamentService(ITournamentRepository tournamentRepository,
                                 IRepository<Team> teamRepository,
                                 IStandingService standingService,
                                 IMapper mapper,
                                 ILogger<TournamentService> logger,
                                 ITournamentStateMachine stateMachine,
                                 IUnitOfWork unitOfWork,
                                 ISignalRService signalRService,
                                 ITenantService tenantService)
        {
            _tournamentRepository = tournamentRepository;
            _teamRepository = teamRepository;
            _standingService = standingService;
            _mapper = mapper;
            _logger = logger;
            _stateMachine = stateMachine;
            _unitOfWork = unitOfWork;
            _signalRService = signalRService;
            _tenantService = tenantService;
        }

        public async Task<TournamentStateDTO> GetTournamentStateAsync(long tournamentId)
        {
            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId)
                ?? throw new NotFoundException("Tournament", tournamentId);

            return new TournamentStateDTO(
                CurrentStatus: tournament.Status,
                IsTransitionState: _stateMachine.IsTransitionState(tournament.Status),
                IsActiveState: _stateMachine.IsActiveState(tournament.Status),
                CanScoreMatches: _stateMachine.CanScoreMatches(tournament.Status),
                CanModifyTeams: _stateMachine.CanModifyTeams(tournament.Status),
                CanCancel: _stateMachine.CanCancel(tournament.Status),
                CanDelete: _stateMachine.CanDelete(tournament.Status),
                RowVersion: tournament.RowVersion
            );
        }

        public async Task<CreateTournamentResponseDTO> CreateTournamentAsync(CreateTournamentRequestDTO request)
        {
            var tournament = _mapper.Map<Tournament>(request);
            tournament.IsRegistrationOpen = true;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _tournamentRepository.AddAsync(tournament);
                await _unitOfWork.SaveChangesAsync(); // flush to get tournament.Id

                await _standingService.InitializeStandingsForTournamentAsync(
                    tournament.Id, request.Format, tournament.MaxTeams, request.NumberOfGroups, request.AdvancingPerGroup);

                await _unitOfWork.CommitTransactionAsync(); // saves standings + commits
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            await _signalRService.BroadcastTournamentCreated(tournament.Id, _tenantService.GetCurrentUserName());

            return new CreateTournamentResponseDTO(tournament.Id, tournament.RowVersion);
        }

        public async Task<GetTournamentResponseDTO> GetTournamentAsync(long id)
        {
            var existingTournament = await _tournamentRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Tournament", id);

            return _mapper.Map<GetTournamentResponseDTO>(existingTournament);
        }

        public async Task<List<GetTournamentListResponseDTO>> GetTournamentListAsync()
        {
            var tournaments = await _tournamentRepository.GetAllForListAsync();

            return tournaments
                .Select(t => _mapper.Map<GetTournamentListResponseDTO>(t) with { CurrentTeams = t.TournamentTeams.Count })
                .ToList();
        }

        public async Task<UpdateTournamentResponseDTO> UpdateTournamentAsync(long id, UpdateTournamentRequestDTO request)
        {
            var existingTournament = await _tournamentRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Tournament", id);

            _mapper.Map(request, existingTournament);

            await _unitOfWork.SaveChangesAsync();
            await _signalRService.BroadcastTournamentUpdated(id, _tenantService.GetCurrentUserName());

            return _mapper.Map<UpdateTournamentResponseDTO>(existingTournament);
        }

        public async Task CancelTournamentAsync(long id, CancelTournamentRequestDTO request)
        {
            var tournament = await _tournamentRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Tournament", id);

            if (!_stateMachine.CanCancel(tournament.Status))
                throw new ConflictException($"A tournament in {tournament.Status} state cannot be cancelled.");

            var cancelledBy = _tenantService.GetCurrentUserName();
            var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

            tournament.Cancel(cancelledBy, DateTime.UtcNow, reason);
            await _unitOfWork.SaveChangesAsync();

            await _signalRService.BroadcastTournamentCancelled(id, cancelledBy);
        }

        public async Task DeleteTournamentAsync(long id)
        {
            var tournament = await _tournamentRepository.GetWithTeamsAsync(id)
                ?? throw new NotFoundException("Tournament", id);

            if (!_stateMachine.CanDelete(tournament.Status))
                throw new ConflictException($"A tournament in {tournament.Status} state cannot be deleted.");

            var exclusiveTeams = tournament.TournamentTeams
                .Where(tt => tt.Team.TournamentParticipations.Count == 1)
                .Select(tt => tt.Team)
                .ToList();

            foreach (var team in exclusiveTeams)
                await _teamRepository.DeleteAsync(team);

            await _tournamentRepository.DeleteAsync(tournament);
            await _unitOfWork.SaveChangesAsync();

            await _signalRService.BroadcastTournamentDeleted(id, _tenantService.GetCurrentUserName());
        }

        public async Task<List<GetTeamResponseDTO>> GetTeamsByTournamentAsync(long tournamentId)
        {
            var tournament = await _tournamentRepository.GetWithTeamsAsync(tournamentId);
            var teams = tournament?.TournamentTeams.Select(tt => tt.Team).ToList() ?? new();

            return _mapper.Map<List<GetTeamResponseDTO>>(teams);
        }

        public async Task<StartTournamentDTO> StartTournament(long tournamentId)
        {
            var existingTournament = await _tournamentRepository.GetByIdAsync(tournamentId)
                ?? throw new NotFoundException("Tournament", tournamentId);

            if (!existingTournament.IsRegistrationOpen)
                throw new ConflictException("Tournament already started");

            existingTournament.IsRegistrationOpen = false;
            await _unitOfWork.SaveChangesAsync();

            return new StartTournamentDTO("Tournament succesfully started", true, existingTournament.RowVersion);
        }
    }
}

