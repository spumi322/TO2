using Application.Contracts;
using Application.DTOs.Team;
using AutoMapper;
using Domain.AggregateRoots;
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
        private readonly IMapper _mapper;
        private readonly ILogger<MatchService> _logger;

        public MatchService(IStandingService standingService,
                            ITournamentService tournamentService,
                            ITeamService teamService,
                            IGenericRepository<Match> matchRepository,
                            IMapper mapper,
                            ILogger<MatchService> logger)
        {
            _standingService = standingService;
            _tournamentService = tournamentService;
            _teamService = teamService;
            _matchRepository = matchRepository;
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
                var standing = await _matchRepository.GetAllByFK("StandingId", standingId);
                var teamsA = standing.Select(m => m.TeamAId).ToList();
                var teamsB = standing.Select(m => m.TeamBId).ToList();
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

        public async Task SeedGroups(long tournamentId)
        {
            // Get standings and teams
            var standings = await _standingService.GetStandingsAsync(tournamentId);
            var groupsCount = standings.Count(s => s.Type is StandingType.Group);
            List<GetTeamResponseDTO> teamsDTO = await _tournamentService.GetTeamsByTournamentAsync(tournamentId);
            List<Team> teams = _mapper.Map<List<Team>>(teamsDTO);

            // Randomize teams
            teams = teams.OrderBy(t => Guid.NewGuid()).ToList();

            // Split teams equally into groupsCount number of groups
            List<List<Team>> groups = new List<List<Team>>();
            for (int i = 0; i < groupsCount; i++)
            {
                groups.Add(new List<Team>());
            }

            for (int i = 0; i < teams.Count; i++)
            {
                groups[i % groupsCount].Add(teams[i]);
            }

            // Seed groups with teams and generate matches
            for (int i = 0; i < groupsCount; i++)
            {
                var standing = standings.FirstOrDefault(s => s.Name == $"Group {i + 1}");
                for (int j = 0; j < groups[i].Count; j++)
                {
                    for (int k = j + 1; k < groups[i].Count; k++)
                    {
                        await GenerateMatch(groups[i][j], groups[i][k], j+1, k, standing.Id);
                    }
                }
            }
        }

        public async Task SeedBracket(long tournamentId, List<Team> playOffTeams)
        {
            // Get standings and teams
            var standings = await _standingService.GetStandingsAsync(tournamentId);
            var bracket = standings.FirstOrDefault(s => s.Type is StandingType.Bracket);
          
            // Generate matches
            for (int i = 0; i < playOffTeams.Count; i++)
            {
                for (int j = i + 1; j < playOffTeams.Count; j++)
                {
                    await GenerateMatch(playOffTeams[i], playOffTeams[j], i+1, j, bracket.Id);
                }
            }
        }
    }
}
