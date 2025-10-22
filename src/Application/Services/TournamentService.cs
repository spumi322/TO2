using Application.Contracts;
using Application.DTOs.Team;
using Application.DTOs.Tournament;
using AutoMapper;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using Domain.StateMachine;
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
        private readonly IGenericRepository<Match> _matchRepository;
        private readonly IGenericRepository<Bracket> _bracketRepository;
        private readonly ITeamService _teamService;
        private readonly ITO2DbContext _dbContext;
        private readonly IStandingService _standingService;
        private readonly IMapper _mapper;
        private readonly ILogger<TournamentService> _logger;
        private readonly ITournamentStateMachine _stateMachine;
        private readonly IOrchestrationService _orchestrationService;

        public TournamentService(IGenericRepository<Tournament> tournamentRepository,
                                 IGenericRepository<Match> matchRepository,
                                 IGenericRepository<Bracket> bracketRepository,
                                 ITeamService teamService,
                                 ITO2DbContext tO2DbContext,
                                 IStandingService standingService,
                                 IMapper mapper,
                                 ILogger<TournamentService> logger,
                                 ITournamentStateMachine stateMachine,
                                 IOrchestrationService orchestrationService)
        {
            _tournamentRepository = tournamentRepository;
            _matchRepository = matchRepository;
            _bracketRepository = bracketRepository;
            _teamService = teamService;
            _dbContext = tO2DbContext;
            _standingService = standingService;
            _mapper = mapper;
            _logger = logger;
            _stateMachine = stateMachine;
            _orchestrationService = orchestrationService;
        }

        public async Task<TournamentStateDTO> GetTournamentState(long tournamentId)
        {
            var tournament = await _tournamentRepository.Get(tournamentId)
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

        //public async Task<StartBracketResponseDTO> StartBracket(long tournamentId)
        //{
        //    var tournament = await _tournamentRepository.Get(tournamentId)
        //        ?? throw new Exception("Tournament not found");

        //    try
        //    {
        //        // Validate state
        //        if (tournament.Status != TournamentStatus.GroupsCompleted)
        //        {
        //            return new StartBracketResponseDTO(
        //                Success: false,
        //                Message: $"Cannot start bracket from {tournament.Status}. Groups must be completed first.",
        //                TournamentStatus: tournament.Status
        //            );
        //        }

        //        // 1. Validate and transition to SeedingBracket
        //        _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.SeedingBracket);
        //        tournament.Status = TournamentStatus.SeedingBracket;
        //        await _tournamentRepository.Update(tournament);
        //        await _tournamentRepository.Save();

        //        // 2. Seed bracket
        //        var seedResult = await _orchestrationService.SeedBracketIfReady(tournamentId);
        //        if (!seedResult.Success)
        //        {
        //            return new StartBracketResponseDTO(false, seedResult.Message, tournament.Status);
        //        }

        //        // 3. Validate and transition to BracketInProgress
        //        _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.BracketInProgress);
        //        tournament.Status = TournamentStatus.BracketInProgress;
        //        await _tournamentRepository.Update(tournament);
        //        await _tournamentRepository.Save();

        //        _logger.LogInformation($"Tournament {tournamentId} bracket started.");

        //        return new StartBracketResponseDTO(
        //            Success: true,
        //            Message: "Bracket started successfully!",
        //            TournamentStatus: tournament.Status
        //        );
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        _logger.LogWarning($"Invalid state transition: {ex.Message}");
        //        return new StartBracketResponseDTO(false, ex.Message, tournament.Status);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error starting bracket: {Message}", ex.Message);
        //        throw;
        //    }
        //}



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
                // Validate transition before setting
                _stateMachine.ValidateTransition(existingTournament.Status, TournamentStatus.Cancelled);
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
                // Validate transition before setting
                _stateMachine.ValidateTransition(existingTournament.Status, status);
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

        /// <summary>
        /// Manual method to declare champion and finish tournament.
        /// Kept as override option for edge cases (cancelled mid-bracket, manual corrections).
        /// Note: Champion is normally declared automatically when final match finishes.
        /// </summary>
        public async Task DeclareChampion(long tournamentId, long championTeamId)
        {
            var tournament = await _tournamentRepository.Get(tournamentId)
                ?? throw new Exception("Tournament not found");

            try
            {
                // Validate transition and set tournament status to Finished
                _stateMachine.ValidateTransition(tournament.Status, TournamentStatus.Finished);
                tournament.Status = TournamentStatus.Finished;

                await _tournamentRepository.Update(tournament);
                await _tournamentRepository.Save();

                _logger.LogInformation($"Tournament {tournament.Name} manually completed. Champion: Team {championTeamId}");
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

                // Get all bracket matches using repository
                var allMatches = await _matchRepository.GetAllByFK("StandingId", bracketStanding.Id);
                if (!allMatches.Any())
                {
                    return new List<FinalStandingDTO>();
                }

                // Get all bracket entries (for team names)
                var bracketEntries = await _bracketRepository.GetAllByFK("StandingId", bracketStanding.Id);

                // Calculate total rounds
                int totalRounds = allMatches.Max(m => m.Round ?? 0);

                // Find the final match (last round, seed 1)
                var finalMatch = allMatches.FirstOrDefault(m => m.Round == totalRounds && m.Seed == 1);
                if (finalMatch == null || !finalMatch.WinnerId.HasValue)
                {
                    return new List<FinalStandingDTO>();
                }

                // Build standings by determining elimination round for each team
                var teamPlacements = new List<(long TeamId, string TeamName, int EliminationRound, TeamStatus Status)>();

                foreach (var entry in bracketEntries)
                {
                    // Find the match where this team lost (if any)
                    var lossMatch = allMatches.FirstOrDefault(m => m.LoserId == entry.TeamId);

                    if (lossMatch == null)
                    {
                        // No loss match = Champion
                        teamPlacements.Add((entry.TeamId, entry.TeamName, totalRounds + 1, TeamStatus.Champion));
                    }
                    else
                    {
                        // Team was eliminated in the round they lost
                        int eliminationRound = lossMatch.Round ?? 0;
                        teamPlacements.Add((entry.TeamId, entry.TeamName, eliminationRound, TeamStatus.Eliminated));
                    }
                }

                // Sort by elimination round (higher = better placement)
                // Then by TeamId for tie-breaking within same round
                var sortedTeams = teamPlacements
                    .OrderByDescending(t => t.EliminationRound)
                    .ThenBy(t => t.TeamId)
                    .ToList();

                // Assign placements with tied ranks
                var standings = new List<FinalStandingDTO>();
                int currentPlacement = 1;
                int? lastEliminationRound = null;
                int teamsAtCurrentRank = 0;

                foreach (var team in sortedTeams)
                {
                    if (lastEliminationRound.HasValue && team.EliminationRound < lastEliminationRound.Value)
                    {
                        // New elimination round = new placement (skip tied ranks)
                        currentPlacement += teamsAtCurrentRank;
                        teamsAtCurrentRank = 0;
                    }

                    standings.Add(new FinalStandingDTO(
                        team.TeamId,
                        team.TeamName,
                        currentPlacement,
                        team.Status,
                        team.EliminationRound
                    ));

                    lastEliminationRound = team.EliminationRound;
                    teamsAtCurrentRank++;
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
