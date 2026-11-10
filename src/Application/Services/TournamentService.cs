using Application.Contracts;
using Application.Contracts.Repositories;
using Application.DTOs.Team;
using Application.DTOs.Tournament;
using Application.Mappings;
using Application.Utilities;
using Domain.AggregateRoots;
using Domain.Enums;
using Domain.Exceptions;
using Domain.StateMachine;
using Microsoft.Extensions.Logging;


namespace Application.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly ITournamentRepository _tournamentRepository;
        private readonly IStandingRepository _standingRepository;
        private readonly IRepository<Team> _teamRepository;
        private readonly IStandingService _standingService;
        private readonly ILogger<TournamentService> _logger;
        private readonly ITournamentStateMachine _stateMachine;
        private readonly ISignalRService _signalRService;
        private readonly ITenantService _tenantService;
        private readonly IUnitOfWork _unitOfWork;

        public TournamentService(ITournamentRepository tournamentRepository,
                                 IStandingRepository standingRepository,
                                 IRepository<Team> teamRepository,
                                 IStandingService standingService,
                                 ILogger<TournamentService> logger,
                                 ITournamentStateMachine stateMachine,
                                 IUnitOfWork unitOfWork,
                                 ISignalRService signalRService,
                                 ITenantService tenantService)
        {
            _tournamentRepository = tournamentRepository;
            _standingRepository = standingRepository;
            _teamRepository = teamRepository;
            _standingService = standingService;
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

            var groupCount = (await _standingRepository.FindAllAsync(s => s.TournamentId == tournamentId && s.StandingType == StandingType.Group)).Count();
            var minTeamsToStart = StartRules.MinTeamsToStart(groupCount);
            var hasEnoughTeams = await _teamRepository.CountAsync(t => t.TournamentId == tournamentId) >= minTeamsToStart;

            return new TournamentStateDTO(
                CurrentStatus: tournament.Status,
                IsTransitionState: _stateMachine.IsTransitionState(tournament.Status),
                IsActiveState: _stateMachine.IsActiveState(tournament.Status),
                CanScoreMatches: _stateMachine.CanScoreMatches(tournament.Status),
                CanModifyTeams: _stateMachine.CanModifyTeams(tournament.Status),
                CanCancel: _stateMachine.CanCancel(tournament.Status),
                CanDelete: _stateMachine.CanDelete(tournament.Status),
                CanEditDetails: _stateMachine.CanEditDetails(tournament.Status),
                CanStartGroups: hasEnoughTeams
                    && _stateMachine.IsTransitionValid(tournament.Status, TournamentStatus.SeedingGroups, tournament.Format),
                CanStartBracket: hasEnoughTeams
                    && _stateMachine.IsTransitionValid(tournament.Status, TournamentStatus.SeedingBracket, tournament.Format),
                MinTeamsToStart: minTeamsToStart,
                Version: tournament.Version
            );
        }

        public async Task<CreateTournamentResponseDTO> CreateTournamentAsync(CreateTournamentRequestDTO request)
        {
            var tournament = request.ToTournament();

            await _tournamentRepository.AddAsync(tournament);
            await _standingService.InitializeStandingsForTournamentAsync(tournament, request.NumberOfGroups);
            await _unitOfWork.SaveChangesAsync(); // single save: tournament + standings atomically

            await _signalRService.BroadcastTournamentCreated(tournament.Id, _tenantService.GetCurrentUserName());

            return new CreateTournamentResponseDTO(tournament.Id, tournament.Version);
        }

        public async Task<GetTournamentResponseDTO> GetTournamentAsync(long id)
        {
            var existingTournament = await _tournamentRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Tournament", id);

            var currentTeams = await _teamRepository.CountAsync(t => t.TournamentId == id);
            var numberOfGroups = await _standingRepository.CountAsync(s => s.TournamentId == id && s.StandingType == StandingType.Group);

            return existingTournament.ToGetTournamentResponseDTO(currentTeams, numberOfGroups);
        }

        public async Task<List<GetTournamentListResponseDTO>> GetTournamentListAsync()
        {
            var rows = await _tournamentRepository.GetListAsync();

            // Nothing edits a finished tournament, so its last modification is when it finished
            return rows
                .Select(r => r.Tournament.ToGetTournamentListResponseDTO(
                    r.TeamCount,
                    r.WinnerName,
                    r.Tournament.Status == TournamentStatus.Finished ? r.Tournament.LastModifiedDate : null,
                    r.GroupsFinished,
                    r.GroupsTotal,
                    r.RoundsFinished,
                    r.RoundsTotal))
                .ToList();
        }

        public async Task<UpdateTournamentResponseDTO> UpdateTournamentAsync(long id, UpdateTournamentRequestDTO request)
        {
            var existingTournament = await _tournamentRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Tournament", id);

            if (!_stateMachine.CanEditDetails(existingTournament.Status))
                throw new ConflictException("Name and description can only be changed before the tournament starts.");

            if (existingTournament.Version != request.Version)
                throw new ConflictException("The tournament was changed by someone else meanwhile. Check it and save again.");

            var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            existingTournament.UpdateDetails(request.Name.Trim(), description);

            await _unitOfWork.SaveChangesAsync();
            await _signalRService.BroadcastTournamentUpdated(id, _tenantService.GetCurrentUserName());

            return existingTournament.ToUpdateTournamentResponseDTO();
        }

        public async Task CancelTournamentAsync(long id, CancelTournamentRequestDTO request)
        {
            var tournament = await _tournamentRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Tournament", id);

            if (!_stateMachine.CanCancel(tournament.Status))
                throw new ConflictException(tournament.Status == TournamentStatus.Setup
                    ? "A tournament that has not started can be deleted instead."
                    : $"The tournament is already {TournamentStatusText.Describe(tournament.Status)}.");

            var cancelledBy = _tenantService.GetCurrentUserName();
            var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

            tournament.Cancel(cancelledBy, DateTime.UtcNow, reason);
            await _unitOfWork.SaveChangesAsync();

            await _signalRService.BroadcastTournamentCancelled(id, cancelledBy);
        }

        public async Task DeleteTournamentAsync(long id)
        {
            var tournament = await _tournamentRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Tournament", id);

            if (!_stateMachine.CanDelete(tournament.Status))
                throw new ConflictException("Only a tournament that has not started can be deleted.");

            // The database cascades to its teams and standings
            await _tournamentRepository.DeleteAsync(tournament);
            await _unitOfWork.SaveChangesAsync();

            await _signalRService.BroadcastTournamentDeleted(id, _tenantService.GetCurrentUserName());
        }

        public async Task<List<GetTeamResponseDTO>> GetTeamsByTournamentAsync(long tournamentId)
        {
            var teams = await _teamRepository.FindAllAsync(t => t.TournamentId == tournamentId);

            return teams.Select(t => t.ToGetTeamResponseDTO()).ToList();
        }
    }
}

