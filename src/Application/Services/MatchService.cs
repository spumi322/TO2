using Application.Contracts;
using Application.DTOs.Team;
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
    public class MatchService : IMatchService
    {
        private readonly IStandingService _standingService;
        private readonly ITournamentService _tournamentService;
        private readonly IGenericRepository<Match> _matchRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<MatchService> _logger;

        public MatchService(IStandingService standingService, ITournamentService tournamentService,IGenericRepository<Match> matchRepository, IMapper mapper, ILogger<MatchService> logger)
        {
            _standingService = standingService;
            _tournamentService = tournamentService;
            _matchRepository = matchRepository;
            _mapper = mapper;
            _logger = logger;

        }

        public async Task GenerateMatch(Team teamA, Team teamB, int round, int seed, long standingId)
        {
            try
            {
                var match = new Match(teamA, teamB, BestOf.Bo3);
                match.Round = round;
                match.Seed = seed;
                match.StandingId = standingId;

                await _matchRepository.Add(match);
                await _matchRepository.Save();
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
                        await GenerateMatch(groups[i][j], groups[i][k], i, j, standing.Id);
                    }
                }
            }
        }
    }
}
