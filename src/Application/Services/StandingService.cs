using Application.Contracts;
using Application.DTOs.Standing;
using Application.DTOs.Team;
using Application.DTOs.Tournament;
using Domain.AggregateRoots;
//using Domain.DomainEvents; // STEP 1 FIX: No longer needed
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class StandingService : IStandingService
    {
        private readonly IGenericRepository<Standing> _standingRepository;
        private readonly IGenericRepository<Match> _matchRepository;
        private readonly IGenericRepository<Tournament> _tournamentRepository;
        private readonly IGenericRepository<Group> _groupRepository;
        private readonly IGenericRepository<Team> _teamRepository;
        private readonly IGenericRepository<TournamentTeam> _tournamentTeamRepository;
        private readonly IGameService _gameService;
        private readonly ILogger<StandingService> _logger;

        public StandingService(IGenericRepository<Standing> standingRepository,
                               IGenericRepository<Match> matchRepository,
                               IGenericRepository<Tournament> tournamentRepository,
                               IGenericRepository<Group> groupRepository,
                               IGenericRepository<Team> teamRepository,
                               IGenericRepository<TournamentTeam> tournamentTeamRepository,
                               IGameService gameService,
                               ILogger<StandingService> logger)
        {
            _standingRepository = standingRepository;
            _matchRepository = matchRepository;
            _tournamentRepository = tournamentRepository;
            _groupRepository = groupRepository;
            _teamRepository = teamRepository;
            _tournamentTeamRepository = tournamentTeamRepository;
            _gameService = gameService;
            _logger = logger;
        }

        public async Task GenerateStanding(long tournamentId, string name, StandingType type, int? teamsPerStanding)
        {
            try
            {
                var standing = new Standing(name, teamsPerStanding, type);
                standing.TournamentId = tournamentId;

                await _standingRepository.Add(standing);
                await _standingRepository.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving standing: {Message}", ex.Message);

                throw;
            }
        }

        public async Task<List<Standing>> GetStandingsAsync(long tournamentId)
        {
            try
            {
                var standings = await _standingRepository.GetAllByFK("TournamentId", tournamentId);

                return standings.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting standings: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<bool> IsGroupFinished(long standingId)
        {
            var standing = await _standingRepository.Get(standingId)
                ?? throw new Exception($"Standing with Id: {standingId} was not found!");
            var matches = await _matchRepository.GetAllByFK("StandingId", standingId)
                ?? throw new Exception($"Matches for the standing Id : {standingId} were not found!");
            bool standingFinished = false;

            if (matches.All(m => m.WinnerId is not null && m.LoserId is not null))
            {
                standingFinished = true;
            }

            return standingFinished;
        }

        public async Task MarkGroupAsFinished(long standingId)
        {
            var standing = await _standingRepository.Get(standingId)
                ?? throw new Exception($"Standing with Id: {standingId} was not found!");

            standing.IsFinished = true;
            await _standingRepository.Update(standing);
            await _standingRepository.Save();
        }

        public async Task<bool> CheckAllGroupsAreFinished(long tournamentId)
        {
            var allStandings = await _standingRepository.GetAllByFK("TournamentId", tournamentId)
                ?? throw new Exception($"Standings with tournamentId: {tournamentId} was not found!");
            var allGroups = allStandings.Where(s => s.StandingType == StandingType.Group);

            bool allGroupsFinished = allGroups.Any() && allGroups.All(ag => ag.IsFinished);

            if (allGroupsFinished)
            {
                _logger.LogInformation($"✓ ALL GROUPS FINISHED for tournament {tournamentId}!");
            }
            else
            {
                _logger.LogInformation($"Groups not all finished yet for tournament {tournamentId}.");
            }

            return allGroupsFinished;
        }

        public async Task<List<Team>> GetTeamsForBracket(long tournamentId)
        {
            var advancingTeams = new List<Team>();
            var standings = await GetStandingsAsync(tournamentId);
            var groups = standings.Where(s => s.StandingType == StandingType.Group).ToList();
            var bracket = standings.FirstOrDefault(s => s.StandingType == StandingType.Bracket);

            if (bracket == null)
                throw new Exception("Bracket standing not found");

            if (groups.Count == 0)
                throw new Exception("No groups found");

            // Calculate how many teams advance per group
            int teamsAdvancingPerGroup = bracket.MaxTeams / groups.Count;

            _logger.LogInformation($"Teams advancing per group: {teamsAdvancingPerGroup} (Bracket: {bracket.MaxTeams}, Groups: {groups.Count})");

            foreach (var group in groups)
            {
                // Get group entries using repository and sort in memory
                var allGroupEntries = await _groupRepository.GetAllByFK("StandingId", group.Id);
                var groupEntries = allGroupEntries
                    .OrderByDescending(g => g.Points)
                    .ThenByDescending(g => g.Wins)
                    .ThenBy(g => g.Losses)
                    .ToList();

                // Top X teams advance
                var advancing = groupEntries.Take(teamsAdvancingPerGroup).ToList();
                var eliminated = groupEntries.Skip(teamsAdvancingPerGroup).ToList();

                foreach (var groupEntry in advancing)
                {
                    groupEntry.Status = TeamStatus.Advanced;
                    await _groupRepository.Update(groupEntry);

                    // Fetch Team entity using repository
                    var team = await _teamRepository.Get(groupEntry.TeamId);

                    if (team != null)
                    {
                        advancingTeams.Add(team);
                        _logger.LogInformation($"Team {team.Name} advanced from {group.Name}");
                    }
                }

                foreach (var groupEntry in eliminated)
                {
                    groupEntry.Status = TeamStatus.Eliminated;
                    groupEntry.Eliminated = true;
                    await _groupRepository.Update(groupEntry);
                    _logger.LogInformation($"Team {groupEntry.TeamName} eliminated from {group.Name}");
                }
            }

            // Save GroupEntry status changes using repository
            await _groupRepository.Save();

            return advancingTeams;
        }


        /// <summary>
        /// Advances the match winner to the next round by populating the appropriate team slot.
        /// </summary>
        public async Task AdvanceWinnerToNextRound(long finishedMatchId, long winnerId, long standingId)
        {
            // 1. Get the finished match
            var finishedMatch = await _matchRepository.Get(finishedMatchId)
                ?? throw new Exception($"Match {finishedMatchId} not found");

            if (!finishedMatch.Round.HasValue || !finishedMatch.Seed.HasValue)
            {
                throw new Exception($"Match {finishedMatchId} missing Round or Seed information");
            }

            int currentRound = finishedMatch.Round.Value;
            int currentSeed = finishedMatch.Seed.Value;

            // 2. Calculate next round match position
            int nextRound = currentRound + 1;
            int nextSeed = (int)Math.Ceiling(currentSeed / 2.0);

            _logger.LogInformation($"Match R{currentRound}S{currentSeed} finished. Winner {winnerId} advances to R{nextRound}S{nextSeed}");

            // 3. Find the next round match
            var allMatches = await _matchRepository.GetAllByFK("StandingId", standingId);
            var nextMatch = allMatches.FirstOrDefault(m =>
                m.Round == nextRound &&
                m.Seed == nextSeed);

            if (nextMatch == null)
            {
                _logger.LogInformation($"No next round match found (final match completed)");
                return; // This was the final match
            }

            // 4. Determine which team slot (A or B) based on seed parity
            // Odd seeds (1, 3, 5...) → TeamA
            // Even seeds (2, 4, 6...) → TeamB
            if (currentSeed % 2 == 1)
            {
                nextMatch.TeamAId = winnerId;
                _logger.LogInformation($"Set R{nextRound}S{nextSeed} TeamA = {winnerId}");
            }
            else
            {
                nextMatch.TeamBId = winnerId;
                _logger.LogInformation($"Set R{nextRound}S{nextSeed} TeamB = {winnerId}");
            }

            // 5. Update all games in the next match with the new team IDs
            await _gameService.UpdateGamesTeamIds(nextMatch.Id, nextMatch.TeamAId, nextMatch.TeamBId);

            // 6. Save the updated match
            await _matchRepository.Update(nextMatch);
            await _matchRepository.Save();
        }

        public async Task<List<TeamPlacementDTO>> GetFinalResults(long tournamentId)
        {
            var tournament = await _tournamentRepository.Get(tournamentId)
                ?? throw new Exception("Tournament not found");

            if (tournament.Status != TournamentStatus.Finished)
            {
                return new List<TeamPlacementDTO>();
            }

            try
            {
                var bracketStanding = (await GetStandingsAsync(tournamentId))
                    .FirstOrDefault(s => s.StandingType == StandingType.Bracket);

                if (bracketStanding == null)
                {
                    return new List<TeamPlacementDTO>();
                }

                // Get all bracket matches using repository
                var allMatches = await _matchRepository.GetAllByFK("StandingId", bracketStanding.Id);
                if (!allMatches.Any())
                {
                    return new List<TeamPlacementDTO>();
                }

                // Extract unique team IDs from matches
                var teamIds = allMatches
                    .SelectMany(m => new[] { m.TeamAId, m.TeamBId })
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .Distinct()
                    .ToHashSet();

                // Get all teams from repository and filter to bracket participants
                var allTeams = await _teamRepository.GetAll();
                var bracketTeams = allTeams.Where(t => teamIds.Contains(t.Id)).ToList();
                var teamNameLookup = bracketTeams.ToDictionary(t => t.Id, t => t.Name);

                // Calculate total rounds
                int totalRounds = allMatches.Max(m => m.Round ?? 0);

                // Find the final match (last round, seed 1)
                var finalMatch = allMatches.FirstOrDefault(m => m.Round == totalRounds && m.Seed == 1);
                if (finalMatch == null || !finalMatch.WinnerId.HasValue)
                {
                    return new List<TeamPlacementDTO>();
                }

                // Build standings by determining elimination round for each team
                var teamPlacements = new List<(long TeamId, string TeamName, int EliminationRound, TeamStatus Status)>();

                foreach (var teamId in teamIds)
                {
                    // Find the match where this team lost (if any)
                    var lossMatch = allMatches.FirstOrDefault(m => m.LoserId == teamId);

                    var teamName = teamNameLookup.ContainsKey(teamId) ? teamNameLookup[teamId] : "Unknown Team";

                    if (lossMatch == null)
                    {
                        // No loss match = Champion
                        teamPlacements.Add((teamId, teamName, totalRounds + 1, TeamStatus.Champion));
                    }
                    else
                    {
                        // Team was eliminated in the round they lost
                        int eliminationRound = lossMatch.Round ?? 0;
                        teamPlacements.Add((teamId, teamName, eliminationRound, TeamStatus.Eliminated));
                    }
                }

                // Sort by elimination round (higher = better placement)
                // Then by TeamId for tie-breaking within same round
                var sortedTeams = teamPlacements
                    .OrderByDescending(t => t.EliminationRound)
                    .ThenBy(t => t.TeamId)
                    .ToList();

                // Assign placements with tied ranks
                var standings = new List<TeamPlacementDTO>();
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

                    standings.Add(new TeamPlacementDTO(
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

        /// <summary>
        /// Calculates final placements for all teams based on bracket match results.
        /// Uses single-elimination logic: placement determined by elimination round.
        /// </summary>
        public async Task<List<(long TeamId, int Placement, int? EliminatedInRound)>> CalculateFinalPlacements(long standingId)
        {
            _logger.LogInformation($"=== Calculating final placements for standing {standingId} ===");

            var placements = new List<(long TeamId, int Placement, int? EliminatedInRound)>();

            // Get all matches for this bracket
            var allMatches = await _matchRepository.GetAllByFK("StandingId", standingId);
            var matches = allMatches.OrderByDescending(m => m.Round).ThenBy(m => m.Seed).ToList();

            if (!matches.Any())
            {
                _logger.LogWarning("No matches found for standing");
                return placements;
            }

            // Find max round (final)
            int maxRound = matches.Max(m => m.Round ?? 0);
            _logger.LogInformation($"Max round: {maxRound}");

            // Calculate placement for each round, starting from final
            int currentPlacement = 1;

            for (int round = maxRound; round >= 1; round--)
            {
                var roundMatches = matches.Where(m => m.Round == round).OrderBy(m => m.Seed).ToList();
                int teamsEliminatedThisRound = roundMatches.Count;

                _logger.LogInformation($"Round {round}: {teamsEliminatedThisRound} matches");

                foreach (var match in roundMatches)
                {
                    if (round == maxRound && match.Seed == 1)
                    {
                        // Final match - special handling
                        if (match.WinnerId.HasValue)
                        {
                            // Champion (1st place)
                            placements.Add((match.WinnerId.Value, 1, null));
                            _logger.LogInformation($"  Team {match.WinnerId} = 1st place (Champion)");
                        }

                        if (match.LoserId.HasValue)
                        {
                            // Runner-up (2nd place)
                            placements.Add((match.LoserId.Value, 2, maxRound));
                            _logger.LogInformation($"  Team {match.LoserId} = 2nd place (Runner-up, eliminated R{maxRound})");
                        }

                        currentPlacement = 3; // Next placements start from 3rd
                    }
                    else
                    {
                        // Non-final matches - losers get placed based on elimination round
                        if (match.LoserId.HasValue)
                        {
                            placements.Add((match.LoserId.Value, currentPlacement, round));
                            _logger.LogInformation($"  Team {match.LoserId} = {currentPlacement} place (eliminated R{round})");
                            currentPlacement++;
                        }
                    }
                }
            }

            _logger.LogInformation($"✓ Calculated {placements.Count} final placements");
            return placements;
        }

        /// <summary>
        /// Persists final results to TournamentTeam table.
        /// Updates FinalPlacement, EliminatedInRound, and ResultFinalizedAt for all teams.
        /// </summary>
        public async Task SetFinalResults(long tournamentId, List<(long TeamId, int Placement, int? EliminatedInRound)> placements)
        {
            _logger.LogInformation($"=== Storing final results for tournament {tournamentId} ===");

            var now = DateTime.UtcNow;

            foreach (var placement in placements)
            {
                {
                    // Get TournamentTeam record
                    var tournamentTeams = await _tournamentTeamRepository.GetAllByFK("TournamentId", tournamentId);
                    var tournamentTeam = tournamentTeams.FirstOrDefault(tt => tt.TeamId == placement.TeamId);

                    if (tournamentTeam == null)
                    {
                        _logger.LogWarning($"TournamentTeam not found for Team {placement.TeamId} in Tournament {tournamentId}");
                        continue;
                    }

                    // Update final results
                    tournamentTeam.FinalPlacement = placement.Placement;
                    tournamentTeam.EliminatedInRound = placement.EliminatedInRound;
                    tournamentTeam.ResultFinalizedAt = now;

                    await _tournamentTeamRepository.Update(tournamentTeam);
                    _logger.LogInformation($"  Team {placement.TeamId}: Placement {placement}, Eliminated Round {placement.EliminatedInRound.ToString() ?? "N/A"}");
                }

                await _tournamentTeamRepository.Save();
                _logger.LogInformation($"✓ Stored {placements.Count} final results");
            }
        }
    }
}
