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
                _logger.LogError(ex, "Error saving tournament: {Message}", ex.Message);
                throw;
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
                _logger.LogError(ex, "Error updating tournament: {Message}", ex.Message);
                throw;
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
                _logger.LogError(ex, "Error deleting tournament: {Message}", ex.Message);
                throw;
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
                _logger.LogError(ex, "Error setting tournament status: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<List<GetTeamResponseDTO>> GetTeamsByTournamentAsync(long tournamentId)
        {
            var teams = await _dbContext.TournamentTeams
                .Where(tt => tt.TournamentId == tournamentId)
                .Select(tt => tt.Team)
                .ToListAsync();

            return _mapper.Map<List<GetTeamResponseDTO>>(teams);
        }

        public async Task RemoveTeamFromTournamentAsync(long teamId, long tournamentId)
        {
            var existingTeam = await _teamService.GetTeamAsync(teamId) ?? throw new Exception("Team not found");
            var existingTournament = await _tournamentRepository.Get(tournamentId) ?? throw new Exception("Tournament not found");

            // Check if registration is still open
            if (!existingTournament.IsRegistrationOpen)
                throw new Exception("Cannot remove teams after tournament has started");

            try
            {
                // Remove from TournamentTeams table
                var tournamentTeam = await _dbContext.TournamentTeams
                    .FirstOrDefaultAsync(tt => tt.TeamId == teamId && tt.TournamentId == tournamentId);

                if (tournamentTeam == null)
                    throw new Exception("Team is not registered in this tournament");

                _dbContext.TournamentTeams.Remove(tournamentTeam);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing team from tournament: {Message}", ex.Message);
                throw;
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

        public async Task DeclareChampion(long tournamentId, long championTeamId)
        {
            var tournament = await _tournamentRepository.Get(tournamentId)
                ?? throw new Exception("Tournament not found");

            try
            {
                // Set tournament status to Finished
                tournament.Status = TournamentStatus.Finished;

                // Find the bracket standing
                var bracketStanding = (await _standingService.GetStandingsAsync(tournamentId))
                    .FirstOrDefault(s => s.StandingType == StandingType.Bracket);

                if (bracketStanding != null)
                {
                    // Mark the champion in BracketEntries
                    var championEntry = await _dbContext.BracketEntries
                        .FirstOrDefaultAsync(b => b.TeamId == championTeamId && b.StandingId == bracketStanding.Id);

                    if (championEntry != null)
                    {
                        championEntry.Status = TeamStatus.Champion;
                        _logger.LogInformation($"Team {championEntry.TeamName} (ID: {championTeamId}) declared champion of tournament {tournament.Name}");
                    }
                }

                await _tournamentRepository.Update(tournament);
                await _tournamentRepository.Save();

                _logger.LogInformation($"Tournament {tournament.Name} completed. Champion: Team {championTeamId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error declaring champion: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<GetTeamResponseDTO?> GetChampion(long tournamentId)
        {
            var tournament = await _tournamentRepository.Get(tournamentId)
                ?? throw new Exception("Tournament not found");

            if (tournament.Status != TournamentStatus.Finished)
            {
                return null;
            }

            try
            {
                var bracketStanding = (await _standingService.GetStandingsAsync(tournamentId))
                    .FirstOrDefault(s => s.StandingType == StandingType.Bracket);

                if (bracketStanding == null)
                {
                    return null;
                }

                var championEntry = await _dbContext.BracketEntries
                    .Include(b => b.Team)
                    .FirstOrDefaultAsync(b => b.StandingId == bracketStanding.Id && b.Status == TeamStatus.Champion);

                if (championEntry == null)
                {
                    return null;
                }

                return _mapper.Map<GetTeamResponseDTO>(championEntry.Team);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting champion: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<List<FinalStandingDTO>> GetFinalStandings(long tournamentId)
        {
            var tournament = await _tournamentRepository.Get(tournamentId)
                ?? throw new Exception("Tournament not found");

            if (tournament.Status != TournamentStatus.Finished)
            {
                return new List<FinalStandingDTO>();
            }

            try
            {
                var bracketStanding = (await _standingService.GetStandingsAsync(tournamentId))
                    .FirstOrDefault(s => s.StandingType == StandingType.Bracket);

                if (bracketStanding == null)
                {
                    return new List<FinalStandingDTO>();
                }

                var bracketEntries = await _dbContext.BracketEntries
                    .Where(b => b.StandingId == bracketStanding.Id)
                    .OrderByDescending(b => b.Status == TeamStatus.Champion ? 1 : 0)
                    .ThenByDescending(b => b.CurrentRound)
                    .ThenBy(b => b.Eliminated ? 1 : 0)
                    .ToListAsync();

                var standings = new List<FinalStandingDTO>();
                int placement = 1;

                foreach (var entry in bracketEntries)
                {
                    standings.Add(new FinalStandingDTO(
                        entry.TeamId,
                        entry.TeamName,
                        placement,
                        entry.Status,
                        entry.CurrentRound
                    ));

                    placement++;

                    if (placement > 8)
                        break;
                }

                return standings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting final standings: {Message}", ex.Message);
                throw;
            }
        }
    }
}
