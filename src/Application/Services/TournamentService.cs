using Application.Contracts;
using Application.DTOs.Team;
using Application.DTOs.Tournament;
using AutoMapper;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
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
        private readonly ITeamService _teamService;
        private readonly ITO2DbContext _dbContext;
        private readonly IStandingService _standingService;
        private readonly IMapper _mapper;
        private readonly ILogger<TournamentService> _logger;

        public TournamentService(IGenericRepository<Tournament> tournamentRepository,
                                 ITeamService teamService,
                                 ITO2DbContext tO2DbContext,
                                 IStandingService standingService,
                                 IMapper mapper,
                                 ILogger<TournamentService> logger)
        {
            _tournamentRepository = tournamentRepository;
            _teamService = teamService;
            _dbContext = tO2DbContext;
            _standingService = standingService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<CreateTournamentResponseDTO> CreateTournamentAsync(CreateTournamentRequestDTO request)
        {
            try
            {
                var tournament = _mapper.Map<Tournament>(request);
                tournament.IsRegistrationOpen = true;

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

        public async Task<List<GetTeamResponseDTO>> GetTeamsByTournamentAsync(long tournamentId)
        {
            var teams = await _dbContext.GroupEntries
                .Where(tt => tt.TournamentId == tournamentId)
                .Select(tt => tt.Team)
                .ToListAsync();

            return _mapper.Map<List<GetTeamResponseDTO>>(teams);
        }

        //public async Task<AddTeamToTournamentResponseDTO> AddTeamToTournamentAsync(long teamId, long tournamentId)
        //{
        //    var existingTeam = await _teamService.GetTeamAsync(teamId) ?? throw new Exception("Team not found");
        //    var existingTournament = await _tournamentRepository.Get(tournamentId) ?? throw new Exception("Tournament not found");
        //    var teamsInTournament = await GetTeamsByTournamentAsync(tournamentId);

        //    if (teamsInTournament.Count >= existingTournament.MaxTeams)
        //    {
        //        throw new Exception("Tournament is full");
        //    }

        //    var teamTournamentEntry = new Group(
        //        tournamentId,
        //        );

        //    try
        //    {
        //       await _dbContext.TournamentParticipants.AddAsync(teamTournamentEntry);
        //       await _dbContext.SaveChangesAsync();

        //       return new AddTeamToTournamentResponseDTO(teamTournamentEntry.TeamId, teamTournamentEntry.TournamentId);
        //    }
        //    catch (Exception)
        //    {
        //        _logger.LogError("Error adding team to tournament, teams can only added to a tournament once.");

        //        throw new Exception("Error adding team to tournament, teams can only added to a tournament once.");
        //    }
        //}

        public async Task RemoveTeamFromTournamentAsync(long teamId, long tournamentId)
        {
            var existingTeam = await _teamService.GetTeamAsync(teamId) ?? throw new Exception("Team not found");
            var existingTournament = await _tournamentRepository.Get(tournamentId) ?? throw new Exception("Tournament not found");

            try
            {
                await _dbContext.GroupEntries
                    .Where(tt => tt.TeamId == existingTeam.Id && tt.TournamentId == existingTournament.Id)
                    .ForEachAsync(tt => _dbContext.GroupEntries.Remove(tt));
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                _logger.LogError("Error removing team from tournament");

                throw new Exception("Error removing team from tournament");
            }
        }

        public async Task<StartTournamentDTO> StartTournament(long tournamentId)
        {
            var existingTournament = await _tournamentRepository.Get(tournamentId) ?? throw new Exception("Tournament not found");

            if (existingTournament.IsRegistrationOpen)
            {
                try
                {
                    existingTournament.IsRegistrationOpen = false;
                    await _tournamentRepository.Update(existingTournament);
                    await _tournamentRepository.Save();

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

        public async Task<IsNameUniqueResponseDTO> CheckNameIsUniqueAsync(string name)
        {
            var isUnique = (await _tournamentRepository.GetAll()).Any(t => t.Name == name);

            return new IsNameUniqueResponseDTO(!isUnique);
        }
    }
}
