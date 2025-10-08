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
        private readonly ITO2DbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<MatchService> _logger;

        public MatchService(IStandingService standingService,
                            ITournamentService tournamentService,
                            IGenericRepository<Match> matchRepository,
                            IGenericRepository<Standing> standingRepository,
                            ITO2DbContext tO2DbContext,
                            IMapper mapper,
                            ILogger<MatchService> logger)
        {
            _standingService = standingService;
            _tournamentService = tournamentService;
            _matchRepository = matchRepository;
            _standingRepository = standingRepository;
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
                await _matchRepository.Save();

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
                bool allMatchesGenerated = true;

                foreach (var team in groups[i])
                {
                    var participant = await _dbContext.GroupEntries
                        .FirstOrDefaultAsync(tp => tp.TeamId == team.Id && tp.TournamentId == tournamentId);

                    if (participant != null)
                    {
                        participant.StandingId = standing.Id;
                        participant.Status = TeamStatus.Competing;
                    }
                }

                for (int j = 0; j < groups[i].Count; j++)
                {
                    for (int k = j + 1; k < groups[i].Count; k++)
                    {
                        try
                        {
                            await GenerateMatch(groups[i][j], groups[i][k], j + 1, k, standing.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error generating match for teams {0} and {1}", groups[i][j].Id, groups[i][k].Id);
                            allMatchesGenerated = false;
                        }
                    }
                }

                if (allMatchesGenerated)
                {
                    standing.IsSeeded = true;
                    seededStandingIds.Add(standing.Id);
                    await _standingRepository.Update(standing);
                    await _standingRepository.Save();
                }
            }

            await _dbContext.SaveChangesAsync();

            return new SeedGroupsResponseDTO("Groups seeded successfully!", true, seededStandingIds);
        }

        public async Task<BracketSeedResponseDTO> SeedBracketAfterGroups(long tournamentId, List<BracketSeedDTO> advancedTeams)
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

                if (allMatchesGenerated)
                {
                    bracket.IsSeeded = true;
                    await _standingRepository.Update(bracket);
                    await _standingRepository.Save();
                }

                await _dbContext.SaveChangesAsync();

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

        public async Task<SeedGroupsResponseDTO> SeedBracketOnly(long tournamentId)
        {
            var seededStandingIds = new List<long>();

            try
            {
                _logger.LogInformation("Seeding bracket only tournament {TournamentId}", tournamentId);

                // Get all teams
                var teamsDTO = await _tournamentService.GetTeamsByTournamentAsync(tournamentId);

                if (teamsDTO.Count < 2)
                {
                    return new SeedGroupsResponseDTO("Need at least 2 teams to start tournament", false, seededStandingIds);
                }

                var teams = _mapper.Map<List<Team>>(teamsDTO);

                // Get bracket standing
                var standings = await _standingService.GetStandingsAsync(tournamentId);
                var bracket = standings.FirstOrDefault(s => s.StandingType == StandingType.Bracket);

                if (bracket == null)
                {
                    return new SeedGroupsResponseDTO("Bracket standing not found", false, seededStandingIds);
                }

                if (bracket.IsSeeded)
                {
                    return new SeedGroupsResponseDTO("Bracket is already seeded", false, seededStandingIds);
                }

                // Shuffle teams randomly and generate round 1 matches
                var shuffledTeams = teams.OrderBy(t => Guid.NewGuid()).ToList();
                int seed = 1;
                bool allMatchesGenerated = true;

                for (int i = 0; i < shuffledTeams.Count; i += 2)
                {
                    if (i + 1 < shuffledTeams.Count)
                    {
                        try
                        {
                            var teamA = shuffledTeams[i];
                            var teamB = shuffledTeams[i + 1];
                            await GenerateMatch(teamA, teamB, round: 1, seed, bracket.Id);
                            seed++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error generating bracket match");
                            allMatchesGenerated = false;
                        }
                    }
                }

                if (allMatchesGenerated)
                {
                    bracket.IsSeeded = true;
                    seededStandingIds.Add(bracket.Id);
                    await _standingRepository.Update(bracket);
                    await _standingRepository.Save();
                }

                await _dbContext.SaveChangesAsync();

                return new SeedGroupsResponseDTO("Bracket seeded successfully", true, seededStandingIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding bracket only tournament {TournamentId}", tournamentId);
                return new SeedGroupsResponseDTO($"Error: {ex.Message}", false, seededStandingIds);
            }
        }
    }
}
