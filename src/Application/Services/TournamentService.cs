using Application.Contracts;
using Application.Contracts.Repositories;
using Application.DTOs.Team;
using Application.DTOs.Tournament;
using AutoMapper;
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
        private readonly IStandingService _standingService;
        private readonly IFormatService _formatService;
        private readonly IMapper _mapper;
        private readonly ILogger<TournamentService> _logger;
        private readonly ITournamentStateMachine _stateMachine;
        private readonly ISignalRService _signalRService;
        private readonly ITenantService _tenantService;

        private readonly IUnitOfWork _unitOfWork;

        public TournamentService(ITournamentRepository tournamentRepository,
                                 IStandingService standingService,
                                 IFormatService formatService,
                                 IMapper mapper,
                                 ILogger<TournamentService> logger,
                                 ITournamentStateMachine stateMachine,
                                 IUnitOfWork unitOfWork,
                                 ISignalRService signalRService,
                                 ITenantService tenantService)
        {
            _tournamentRepository = tournamentRepository;
            _standingService = standingService;
            _formatService = formatService;
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
                StatusDisplayName: GetStatusDisplayName(tournament.Status),
                StatusDescription: GetStatusDescription(tournament.Status),
                RowVersion: tournament.RowVersion
            );
        }

        private string GetStatusDisplayName(TournamentStatus status) => status switch
        {
            TournamentStatus.Setup => "Setup",
            TournamentStatus.SeedingGroups => "Seeding Groups...",
            TournamentStatus.GroupsInProgress => "Groups In Progress",
            TournamentStatus.GroupsCompleted => "Groups Completed",
            TournamentStatus.SeedingBracket => "Seeding Bracket...",
            TournamentStatus.BracketInProgress => "Bracket In Progress",
            TournamentStatus.Finished => "Finished",
            TournamentStatus.Cancelled => "Cancelled",
            _ => "Unknown"
        };

        private string GetStatusDescription(TournamentStatus status) => status switch
        {
            TournamentStatus.Setup => "Add teams and configure tournament",
            TournamentStatus.SeedingGroups => "Generating group matches...",
            TournamentStatus.GroupsInProgress => "Group stage in progress - score matches",
            TournamentStatus.GroupsCompleted => "All groups finished - ready to start bracket",
            TournamentStatus.SeedingBracket => "Generating bracket matches...",
            TournamentStatus.BracketInProgress => "Bracket stage in progress - score matches",
            TournamentStatus.Finished => "Tournament complete",
            TournamentStatus.Cancelled => "Tournament cancelled",
            _ => ""
        };

        public async Task<CreateTournamentResponseDTO> CreateTournamentAsync(CreateTournamentRequestDTO request)
        {
            var tournament = _mapper.Map<Tournament>(request);
            tournament.IsRegistrationOpen = true;

            await _tournamentRepository.AddAsync(tournament);
            await _unitOfWork.SaveChangesAsync();

            var metadata = _formatService.GetFormatMetadata(request.Format);

            // Create bracket if required
            if (metadata.RequiresBracket)
            {
                await _standingService.GenerateStanding(
                    tournament.Id, "Main Bracket", StandingType.Bracket, request.TeamsPerBracket);
            }

            // Create groups if required
            if (metadata.RequiresGroups && request.TeamsPerGroup.HasValue)
            {
                int groupCount = _formatService.CalculateNumberOfGroups(
                    request.Format, tournament.MaxTeams, request.TeamsPerGroup.Value);

                for (int i = 0; i < groupCount; i++)
                {
                    await _standingService.GenerateStanding(
                        tournament.Id, $"Group {i + 1}", StandingType.Group, request.TeamsPerGroup);
                }
            }

            // Broadcast tournament creation to all users in tenant
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

            await _tournamentRepository.UpdateAsync(existingTournament);
            await _unitOfWork.SaveChangesAsync();
            await _signalRService.BroadcastTournamentUpdated(id, _tenantService.GetCurrentUserName());

            return _mapper.Map<UpdateTournamentResponseDTO>(existingTournament);
        }

        public async Task SoftDeleteTournamentAsync(long id)
        {
            var existingTournament = await _tournamentRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Tournament", id);

            // Validate transition before setting
            _stateMachine.ValidateTransition(existingTournament.Status, TournamentStatus.Cancelled);
            existingTournament.Status = TournamentStatus.Cancelled;

            await _tournamentRepository.UpdateAsync(existingTournament);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task SetTournamentStatusAsync(long id, TournamentStatus status)
        {
            var existingTournament = await _tournamentRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Tournament", id);

            // Validate transition before setting
            _stateMachine.ValidateTransition(existingTournament.Status, status);
            existingTournament.Status = status;

            await _tournamentRepository.UpdateAsync(existingTournament);
            await _unitOfWork.SaveChangesAsync();
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

            if (existingTournament.IsRegistrationOpen)
            {
                existingTournament.IsRegistrationOpen = false;
                await _tournamentRepository.UpdateAsync(existingTournament);
                await _unitOfWork.SaveChangesAsync();

                return new StartTournamentDTO("Tournament succesfully started", true, existingTournament.RowVersion);
            }
            else
            {
                _logger.LogInformation("Tournament already started");

                return new StartTournamentDTO("Tournament already started", false, existingTournament.RowVersion);
            }
        }
    }
}

