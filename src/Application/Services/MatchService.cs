using Application.Contracts;
using Application.DTOs.Match;
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
        private readonly IGenericRepository<Match> _matchRepository;
        private readonly ILogger<MatchService> _logger;
        private readonly Func<IGameService> _gameServiceFactory;

        public MatchService(IGenericRepository<Match> matchRepository,
                            ILogger<MatchService> logger,
                            Func<IGameService> gameServiceFactory)
        {
            _matchRepository = matchRepository;
            _logger = logger;
            _gameServiceFactory = gameServiceFactory;
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

        public async Task<Match> GenerateMatch(Team? teamA, Team? teamB, int round, int seed, long standingId)
        {
            var gameService = _gameServiceFactory();

            Match match;

            if (teamA != null && teamB != null)
            {
                // Real teams: use constructor
                match = new Match(teamA, teamB, BestOf.Bo3);
                match.Round = round;
                match.Seed = seed;
                match.StandingId = standingId;
            }
            else
            {
                // TBD teams: use object initialization
                match = new Match
                {
                    StandingId = standingId,
                    Round = round,
                    Seed = seed,
                    TeamAId = teamA?.Id ?? 0,
                    TeamBId = teamB?.Id ?? 0,
                    BestOf = BestOf.Bo3
                };
            }

            await _matchRepository.Add(match);
            await _matchRepository.Save();

            return match;
                
        }
    }
}
