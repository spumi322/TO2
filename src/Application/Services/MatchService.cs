using Application.Contracts;
using Application.DTOs.Standing;
using Application.DTOs.Team;
using AutoMapper;
using Domain.AggregateRoots;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
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
        private readonly ITeamService _teamService;
        private readonly IGenericRepository<Match> _matchRepository;
        private readonly IGenericRepository<Standing> _standingRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<MatchService> _logger;

        public MatchService(IStandingService standingService,
                            ITournamentService tournamentService,
                            ITeamService teamService,
                            IGenericRepository<Match> matchRepository,
                            IGenericRepository<Standing> standingRepository,
                            IMapper mapper,
                            ILogger<MatchService> logger)
        {
            _standingService = standingService;
            _tournamentService = tournamentService;
            _teamService = teamService;
            _matchRepository = matchRepository;
            _standingRepository = standingRepository;

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
                _logger.LogError("Error getting matches: {0}, Inner Exception: {1}", ex, ex.InnerException);

                throw new Exception(ex.Message);
            }
        }

        public async Task<List<Team>> GetTeamsAsync(long standingId)
        {
            try
            {
                var matchesByStanding = await _matchRepository.GetAllByFK("StandingId", standingId);
                var teamsA = matchesByStanding.Select(m => m.TeamAId).ToList();
                var teamsB = matchesByStanding.Select(m => m.TeamBId).ToList();
                var teamsById = teamsA.Concat(teamsB).Distinct().Order().ToList();
                var teams = new List<Team>();

                foreach (var teamId in teamsById)
                {
                    var response = await _teamService.GetTeamAsync(teamId);
                    teams.Add(_mapper.Map<Team>(response));
                }

                return teams;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting teams: {0}, Inner Exception: {1}", ex, ex.InnerException);

                throw new Exception(ex.Message);
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
                _logger.LogError("Error generating match: {0}, Inner Exception: {1}", ex, ex.InnerException);

                throw new Exception(ex.Message);
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

            var groupsCount = standings.Count(s => s.Type is StandingType.Group);
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

            return new SeedGroupsResponseDTO("Groups seeded successfully!", true, seededStandingIds);
        }

        public async Task SeedBracket(long tournamentId, List<Team> playOffTeams)
        {
            var standings = await _standingService.GetStandingsAsync(tournamentId);
            var bracket = standings.FirstOrDefault(s => s.Type is StandingType.Bracket);
          
            for (int i = 0; i < playOffTeams.Count; i++)
            {
                for (int j = i + 1; j < playOffTeams.Count; j++)
                {
                    await GenerateMatch(playOffTeams[i], playOffTeams[j], i+1, j, bracket.Id);
                }
            }
        }

        public async Task IsStandingFinished(long standingId)
        {
            var matches = await _matchRepository.GetAllByFK("StandingId", standingId);
            bool allMatchesFinished = matches.All(m => m.WinnerId.HasValue && m.LoserId.HasValue);

            if (allMatchesFinished)
            {
                var standing = await _standingRepository.Get(standingId);
                standing.IsFinished = true;
                await _standingRepository.Update(standing);
                await _standingRepository.Save();
            }
        }
    }
}
