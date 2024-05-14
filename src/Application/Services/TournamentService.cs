using Application.Contracts;
using Application.DTOs.Tournament;
using AutoMapper;
using Domain.AggregateRoots;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Application.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly IGenericRepository<Tournament> _tournamentRepository;
        private readonly IStandingService _standingService;
        private readonly IMapper _mapper;
        private readonly ILogger<TournamentService> _logger;

        public TournamentService(IGenericRepository<Tournament> tournamentRepository,IStandingService standingService, IMapper mapper, ILogger<TournamentService> logger)
        {
            _tournamentRepository = tournamentRepository;
            _standingService = standingService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<CreateTournamentResponseDTO> CreateTournamentAsync(CreateTournamentRequestDTO request)
        {
            try
            {
                var tournament = _mapper.Map<Tournament>(request);
                tournament.Status = TournamentStatus.Upcoming;

                await _tournamentRepository.Add(tournament);
                await _tournamentRepository.Save();
                await _standingService.GenerateStanding(tournament.Id, "Main Bracket", StandingType.Bracket, request.TeamsPerBracket);

                if(request.Format is Format.BracketAndGroup)
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
                _logger.LogError("Error saving tournament: {0}, Inner Exception: {1}", ex, ex.InnerException);

                throw new Exception(ex.Message);
            }
        }

        public async Task<GetTournamentResponseDTO> GetTournamentAsync(long id)
        {
            var existingTournament = await _tournamentRepository.Get(id);

            return _mapper.Map<GetTournamentResponseDTO>(existingTournament) ?? throw new Exception("Tournament not found");
        }

        public async Task<List<GetAllTournamentsResponseDTO>> GetAllTournamentsAsync()
        {
            var tournaments = await _tournamentRepository.GetAll();

            return _mapper.Map<List<GetAllTournamentsResponseDTO>>(tournaments);
        }

        public async Task<UpdateTournamentResponseDTO> UpdateTournamentAsync(long id, UpdateTournamentRequestDTO request)
        {
            var existingTournament = await _tournamentRepository.Get(id) ?? throw new Exception("Tournament not found");

            try
            {
                _mapper.Map(request, existingTournament);

                await _tournamentRepository.Update(existingTournament);
                await _tournamentRepository.Save();

                return _mapper.Map<UpdateTournamentResponseDTO>(existingTournament);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error updating tournament: {0}, Inner Exception: {1}", ex, ex.InnerException);

                throw new Exception(ex.Message);
            }
        }

        public async Task SoftDeleteTournamentAsync(long id)
        {
            var existingTournament = await _tournamentRepository.Get(id) ?? throw new Exception("Tournament not found");

            try
            {
                existingTournament.Status = TournamentStatus.Cancelled;

                await _tournamentRepository.Update(existingTournament);
                await _tournamentRepository.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error deleting tournament: {0}, Inner Exception: {1}", ex, ex.InnerException);

                throw new Exception(ex.Message);
            }
        }

        public async Task SetTournamentStatusAsync(long id, TournamentStatus status)
        {
            var existingTournament = await _tournamentRepository.Get(id) ?? throw new Exception("Tournament not found");

            try
            {
                existingTournament.Status = status;

                await _tournamentRepository.Update(existingTournament);
                await _tournamentRepository.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error setting tournament status: {0}, Inner Exception: {1}", ex, ex.InnerException);

                throw new Exception(ex.Message);
            }
        }
    }
}
