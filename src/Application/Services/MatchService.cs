using Application.Contracts;
using Application.DTOs.Standing;
using Application.DTOs.Team;
using AutoMapper;
using Domain.AggregateRoots;
using Domain.Common;
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
    public class MatchService : IMatchService
    {
        private readonly IStandingService _standingService;
        private readonly ITournamentService _tournamentService;
        private readonly IGenericRepository<Match> _matchRepository;
        private readonly IGenericRepository<Standing> _standingRepository;
        private readonly IGenericRepository<Bracket> _bracketRepository;
        private readonly ITO2DbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<MatchService> _logger;

        public MatchService(IStandingService standingService,
                            ITournamentService tournamentService,
                            IGenericRepository<Match> matchRepository,
                            IGenericRepository<Standing> standingRepository,
                            IGenericRepository<Bracket> bracketRepository,
                            ITO2DbContext tO2DbContext,
                            IMapper mapper,
                            ILogger<MatchService> logger)
        {
            _standingService = standingService;
            _tournamentService = tournamentService;
            _matchRepository = matchRepository;
            _standingRepository = standingRepository;
            _bracketRepository = bracketRepository;
            _dbContext = tO2DbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Match> GetMatchAsync(long id)
        {
            return await _matchRepository.Get(id);
        }

        public async Task<List<Match>> GetMatchesAsync(long standingId)
        {
            try
            {
                var matches = await _matchRepository.GetAllByFK("StandingId", standingId);

                return matches.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting matches: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<long> GenerateMatch(Team teamA, Team teamB, int round, int seed, long standingId)
        {
            try
            {
                var match = new Match(teamA, teamB, BestOf.Bo3);
                match.Round = round;
                match.Seed = seed;
                match.StandingId = standingId;

                await _matchRepository.Add(match);
                // Changes are tracked by EF Core and will be saved by the caller

                return match.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating match: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<SeedGroupsResponseDTO> SeedGroups(long tournamentId)
        {
            var standings = await _standingService.GetStandingsAsync(tournamentId);
            List<long> seededStandingIds = new List<long>();

            if (standings.Any(standing => standing.IsSeeded))
            {
                return new SeedGroupsResponseDTO("Groups are already seeded!", false, seededStandingIds);
            }

            var groupsCount = standings.Count(s => s.StandingType is StandingType.Group);
            List<GetTeamResponseDTO> teamsDTO = await _tournamentService.GetTeamsByTournamentAsync(tournamentId);
            List<Team> teams = _mapper.Map<List<Team>>(teamsDTO);

            if (teams.Count < groupsCount)
            {
                return new SeedGroupsResponseDTO("There are not enough teams to seed the groups!", false, seededStandingIds);
            }

            teams = teams.OrderBy(t => Guid.NewGuid()).ToList();

            int teamsPerGroup = teams.Count / groupsCount;
            int remainingTeams = teams.Count % groupsCount;
            List<List<Team>> groups = new List<List<Team>>();

            int teamIndex = 0;
            for (int i = 0; i < groupsCount; i++)
            {
                int groupSize = teamsPerGroup + (i < remainingTeams ? 1 : 0);
                groups.Add(teams.GetRange(teamIndex, groupSize));
                teamIndex += groupSize;
            }

            for (int i = 0; i < groupsCount; i++)
            {
                var standing = standings.FirstOrDefault(s => s.Name == $"Group {i + 1}");

                if (standing == null)
                {
                    _logger.LogError($"Standing 'Group {i + 1}' not found!");
                    continue;
                }

                bool allMatchesGenerated = true;

                foreach (var team in groups[i])
                {
                    try
                    {
                        var participant = await _dbContext.GroupEntries
                            .FirstOrDefaultAsync(tp => tp.TeamId == team.Id && tp.TournamentId == tournamentId);

                        if (participant != null)
                        {
                            // Update existing entry
                            participant.StandingId = standing.Id;
                            participant.Status = TeamStatus.Competing;
                            _logger.LogInformation($"Updated GroupEntry for team {team.Name} (ID: {team.Id}) in {standing.Name} (ID: {standing.Id})");
                        }
                        else
                        {
                            // Create new GroupEntry for this team (using TeamId/Name to avoid EF tracking conflicts)
                            var groupEntry = new Group(tournamentId, standing.Id, team.Id, team.Name);
                            groupEntry.Status = TeamStatus.Competing;
                            await _dbContext.GroupEntries.AddAsync(groupEntry);

                            _logger.LogInformation($"Created GroupEntry for team {team.Name} (ID: {team.Id}) in {standing.Name} (ID: {standing.Id})");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error creating/updating GroupEntry for team {team.Id} in standing {standing.Id}: {ex.Message}");
                        // Continue with match generation even if GroupEntry creation fails
                    }
                }

                _logger.LogInformation($"Generating matches for {standing.Name} with {groups[i].Count} teams");

                for (int j = 0; j < groups[i].Count; j++)
                {
                    for (int k = j + 1; k < groups[i].Count; k++)
                    {
                        try
                        {
                            _logger.LogInformation($"Generating match: {groups[i][j].Name} vs {groups[i][k].Name} (Round {j + 1}, Seed {k})");
                            await GenerateMatch(groups[i][j], groups[i][k], j + 1, k, standing.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error generating match for teams {0} and {1}: {2}", groups[i][j].Id, groups[i][k].Id, ex.Message);
                            allMatchesGenerated = false;
                        }
                    }
                }

                if (allMatchesGenerated)
                {
                    standing.IsSeeded = true;
                    seededStandingIds.Add(standing.Id);
                    await _standingRepository.Update(standing);
                    await _standingRepository.Save(); // Save standing changes to _standingRepository's DbContext
                }
            }

            // Save matches and group entries to _dbContext
            await _dbContext.SaveChangesAsync();

            return new SeedGroupsResponseDTO("Groups seeded successfully!", true, seededStandingIds);
        }

        public async Task<BracketSeedResponseDTO> SeedBracket(long tournamentId, List<BracketSeedDTO> advancedTeams)
        {
            // Get standings and validate bracket hasn't been seeded yet
            var standings = await _standingService.GetStandingsAsync(tournamentId);
            var bracket = standings.FirstOrDefault(s => s.StandingType == StandingType.Bracket);

            if (bracket == null)
                return new BracketSeedResponseDTO("Bracket standing not found!", false);

            if (bracket.IsSeeded)
                return new BracketSeedResponseDTO("Bracket is already seeded!", false);

            // Validate we have enough teams
            if (advancedTeams.Count < 2)
                return new BracketSeedResponseDTO("Not enough teams to seed the bracket!", false);

            try
            {
                var remainingTeams = new List<BracketSeedDTO>(advancedTeams);
                bool allMatchesGenerated = true;
                int currentSeed = 1;

                while (remainingTeams.Count >= 2)
                {
                    // Get highest placed team
                    var teamADTO = remainingTeams
                        .OrderBy(t => t.Placement)
                        .FirstOrDefault();

                    if (teamADTO == null) break;

                    // Get lowest placed team from different group
                    var teamBDTO = remainingTeams
                        .Where(t => t.GroupId != teamADTO.GroupId)
                        .OrderByDescending(t => t.Placement)
                        .FirstOrDefault();

                    if (teamBDTO == null) break;

                    // Remove selected teams from pool
                    remainingTeams.Remove(teamADTO);
                    remainingTeams.Remove(teamBDTO);

                    // Get actual Team entities
                    var teamA = await _dbContext.Teams.FindAsync(teamADTO.TeamId);
                    var teamB = await _dbContext.Teams.FindAsync(teamBDTO.TeamId);

                    if (teamA == null || teamB == null)
                    {
                        _logger.LogError("Teams not found: {0}, {1}", teamADTO.TeamId, teamBDTO.TeamId);
                        allMatchesGenerated = false;
                        continue;
                    }

                    try
                    {
                        // Generate match with current seed
                        await GenerateMatch(
                            teamA,
                            teamB,
                            round: 1,
                            seed: currentSeed,
                            standingId: bracket.Id
                        );

                        currentSeed++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating match for teams {0} and {1}", teamA.Id, teamB.Id);
                        allMatchesGenerated = false;
                    }
                }

                // Create BracketEntry records for all advancing teams
                foreach (var advancedTeam in advancedTeams)
                {
                    var team = await _dbContext.Teams.FindAsync(advancedTeam.TeamId);
                    if (team != null)
                    {
                        var bracketEntry = new Bracket(tournamentId, bracket.Id, team);
                        bracketEntry.Status = TeamStatus.Competing;
                        bracketEntry.CurrentRound = 1;

                        await _bracketRepository.Add(bracketEntry);
                        _logger.LogInformation($"Created BracketEntry for {team.Name} starting in Round 1");
                    }
                }

                if (allMatchesGenerated)
                {
                    bracket.IsSeeded = true;
                    await _standingRepository.Update(bracket);
                }

                if (remainingTeams.Any())
                {
                    return new BracketSeedResponseDTO(
                        $"Bracket seeded but {remainingTeams.Count} teams couldn't be paired!",
                        allMatchesGenerated);
                }

                return new BracketSeedResponseDTO("Bracket seeded successfully!", true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding bracket");
                return new BracketSeedResponseDTO($"Error seeding bracket: {ex.Message}", false);
            }
        }

        public async Task CheckAndGenerateNextRound(long tournamentId, long standingId, int currentRound)
        {
            try
            {
                // Get all matches in current round
                var currentRoundMatches = (await GetMatchesAsync(standingId))
                    .Where(m => m.Round == currentRound)
                    .ToList();

                if (currentRoundMatches.Count == 0)
                {
                    _logger.LogWarning($"No matches found for round {currentRound} in standing {standingId}");
                    return;
                }

                // Check if all matches in current round are complete
                bool allMatchesComplete = currentRoundMatches.All(m => m.WinnerId.HasValue && m.LoserId.HasValue);

                if (!allMatchesComplete)
                {
                    _logger.LogInformation($"Not all matches complete in round {currentRound}. Waiting for completion.");
                    return;
                }

                _logger.LogInformation($"All matches in round {currentRound} are complete. Checking for next round or championship.");

                // Check if this is the final match (only 1 match in the round means finals)
                if (currentRoundMatches.Count == 1)
                {
                    var finalMatch = currentRoundMatches.First();
                    _logger.LogInformation($"Final match complete. Winner: Team {finalMatch.WinnerId}");

                    // This was the finals, declare champion
                    await _tournamentService.DeclareChampion(tournamentId, finalMatch.WinnerId.Value);
                    return;
                }

                // Generate next round matches
                _logger.LogInformation($"Generating round {currentRound + 1} matches");

                var winners = currentRoundMatches
                    .OrderBy(m => m.Seed)
                    .Select(m => m.WinnerId.Value)
                    .ToList();

                int nextRoundSeed = 1;
                for (int i = 0; i < winners.Count; i += 2)
                {
                    if (i + 1 < winners.Count)
                    {
                        var teamA = await _dbContext.Teams.FindAsync(winners[i]);
                        var teamB = await _dbContext.Teams.FindAsync(winners[i + 1]);

                        if (teamA != null && teamB != null)
                        {
                            await GenerateMatch(
                                teamA,
                                teamB,
                                round: currentRound + 1,
                                seed: nextRoundSeed,
                                standingId: standingId
                            );

                            _logger.LogInformation($"Created Round {currentRound + 1} match {nextRoundSeed}: {teamA.Name} vs {teamB.Name}");
                            nextRoundSeed++;
                        }
                    }
                }

                // Reset advancing teams' status from Advanced back to Competing for the new round
                var advancingBracketEntries = await _dbContext.BracketEntries
                    .Where(b => b.StandingId == standingId && winners.Contains(b.TeamId))
                    .ToListAsync();

                foreach (var entry in advancingBracketEntries)
                {
                    entry.Status = TeamStatus.Competing;
                    entry.CurrentRound = currentRound + 1;
                    _logger.LogInformation($"Advanced {entry.TeamName} to Round {currentRound + 1}");
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Round {currentRound + 1} generation complete");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckAndGenerateNextRound");
                throw;
            }
        }
    }
}
