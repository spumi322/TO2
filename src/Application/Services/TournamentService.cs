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
        private readonly IMapper _mapper;
        private readonly ILogger<TournamentService> _logger;

        public TournamentService(IGenericRepository<Tournament> tournamentRepository, IMapper mapper, ILogger<TournamentService> logger)
        {
            _tournamentRepository = tournamentRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<CreateTournamentResponseDTO> CreateTournamentAsync(CreateTournamentRequestDTO request)
        {

            try
            {
                var tournament = _mapper.Map<Tournament>(request);
                tournament.Status = Domain.Enums.TournamentStatus.Upcoming;

                await _tournamentRepository.Add(tournament);
                await _tournamentRepository.Save();

                return new CreateTournamentResponseDTO(tournament.Id);
            }
            catch (Exception ex)
            {
                // Log the full exception details, including inner exceptions
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
