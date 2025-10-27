using Application.Contracts;
using Application.Contracts.Repositories;
using Application.DTOs.Team;
using Application.DTOs.Tournament;
using AutoMapper;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using Domain.StateMachine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace Application.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly ITournamentRepository _tournamentRepository;
        private readonly IStandingService _standingService;
        private readonly IMapper _mapper;
        private readonly ILogger<TournamentService> _logger;
        private readonly ITournamentStateMachine _stateMachine;

        private readonly IUnitOfWork _unitOfWork;

        public TournamentService(ITournamentRepository tournamentRepository,
                                 IStandingService standingService,
                                 IMapper mapper,
                                 ILogger<TournamentService> logger,
                                 ITournamentStateMachine stateMachine,
                                 IUnitOfWork unitOfWork)
        {
            _tournamentRepository = tournamentRepository;
            _standingService = standingService;
            _mapper = mapper;
            _logger = logger;
            _stateMachine = stateMachine;
            _unitOfWork = unitOfWork;
        }

        public async Task<TournamentStateDTO> GetTournamentStateAsync(long tournamentId)
        {
            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId)
                ?? throw new Exception("Tournament not found");

            return new TournamentStateDTO(
                CurrentStatus: tournament.Status,
                IsTransitionState: _stateMachine.IsTransitionState(tournament.Status),
                IsActiveState: _stateMachine.IsActiveState(tournament.Status),
                CanScoreMatches: _stateMachine.CanScoreMatches(tournament.Status),
                CanModifyTeams: _stateMachine.CanModifyTeams(tournament.Status),
                StatusDisplayName: GetStatusDisplayName(tournament.Status),
                StatusDescription: GetStatusDescription(tournament.Status)
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
            try
            {
                var tournament = _mapper.Map<Tournament>(request);
                tournament.IsRegistrationOpen = true;

                await _tournamentRepository.AddAsync(tournament);
                await _unitOfWork.SaveChangesAsync();
                await _standingService.GenerateStanding(tournament.Id, "Main Bracket", StandingType.Bracket, request.TeamsPerBracket);

                if (request.Format is Format.BracketAndGroup)
                {
                    for (int i = 0; i < (tournament.MaxTeams / request.TeamsPerGroup); i++)
                    {
                        await _standingService.GenerateStanding(tournament.Id, $"Group {i + 1}", StandingType.Group, request.TeamsPerGroup);
                    }
                }

                return new CreateTournamentResponseDTO(tournament.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving tournament: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<GetTournamentResponseDTO> GetTournamentAsync(long id)
        {
            var existingTournament = await _tournamentRepository.GetByIdAsync(id);

            return _mapper.Map<GetTournamentResponseDTO>(existingTournament) ?? throw new Exception("Tournament not found");
        }

        public async Task<List<GetAllTournamentsResponseDTO>> GetAllTournamentsAsync()
        {
            var tournaments = await _tournamentRepository.GetAllAsync();

            return _mapper.Map<List<GetAllTournamentsResponseDTO>>(tournaments);
        }

        public async Task<UpdateTournamentResponseDTO> UpdateTournamentAsync(long id, UpdateTournamentRequestDTO request)
        {
            var existingTournament = await _tournamentRepository.GetByIdAsync(id) ?? throw new Exception("Tournament not found");

            try
            {
                _mapper.Map(request, existingTournament);

                await _tournamentRepository.UpdateAsync(existingTournament);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<UpdateTournamentResponseDTO>(existingTournament);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tournament: {Message}", ex.Message);
                throw;
            }
        }

        public async Task SoftDeleteTournamentAsync(long id)
        {
            var existingTournament = await _tournamentRepository.GetByIdAsync(id) ?? throw new Exception("Tournament not found");

            try
            {
                // Validate transition before setting
                _stateMachine.ValidateTransition(existingTournament.Status, TournamentStatus.Cancelled);
                existingTournament.Status = TournamentStatus.Cancelled;

                await _tournamentRepository.UpdateAsync(existingTournament);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tournament: {Message}", ex.Message);
                throw;
            }
        }

        public async Task SetTournamentStatusAsync(long id, TournamentStatus status)
        {
            var existingTournament = await _tournamentRepository.GetByIdAsync(id) ?? throw new Exception("Tournament not found");

            try
            {
                // Validate transition before setting
                _stateMachine.ValidateTransition(existingTournament.Status, status);
                existingTournament.Status = status;

                await _tournamentRepository.UpdateAsync(existingTournament);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting tournament status: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<List<GetTeamResponseDTO>> GetTeamsByTournamentAsync(long tournamentId)
        {
            var tournament = await _tournamentRepository.GetWithTeamsAsync(tournamentId);
            var teams = tournament?.TournamentTeams.Select(tt => tt.Team).ToList() ?? new();

            return _mapper.Map<List<GetTeamResponseDTO>>(teams);
        }

        public async Task<StartTournamentDTO> StartTournament(long tournamentId)
        {
            var existingTournament = await _tournamentRepository.GetByIdAsync(tournamentId) ?? throw new Exception("Tournament not found");

            if (existingTournament.IsRegistrationOpen)
            {
                try
                {
                    existingTournament.IsRegistrationOpen = false;
                    await _tournamentRepository.UpdateAsync(existingTournament);
                    await _unitOfWork.SaveChangesAsync();

                    return new StartTournamentDTO("Tournament succesfully started", true);
                }
                catch (Exception)
                {
                    _logger.LogError("Error starting the tournament");

                    throw new Exception("Error starting the tournament");
                }
            }
            else
            {
                _logger.LogInformation("Tournament already started");

                return new StartTournamentDTO("Tournament already started", false);
            }
        }
    }
}

