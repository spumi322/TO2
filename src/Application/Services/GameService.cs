using Application.Contracts;
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
    public class GameService : IGameService
    {
        private readonly IGenericRepository<Game> _gameRepository;
        private readonly IMatchService _matchService;
        private readonly IMapper _mapper;
        private readonly ILogger<GameService> _logger;


        public GameService(IGenericRepository<Game> gameRepository, IMatchService matchService, IMapper mapper, ILogger<GameService> logger)
        {
            _gameRepository = gameRepository;
            _matchService = matchService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task GenerateGames(long matchId)
        {
            var existingMatch = await _matchService.GetMatchAsync(matchId) ?? throw new Exception("Match not found");

            var matchesToPlay = existingMatch.BestOf switch
            {
                BestOf.Bo1 => 1,
                BestOf.Bo3 => 3,
                BestOf.Bo5 => 5,
                _ => 1
            };

            for (int i = 0; i < matchesToPlay; i++)
            {
                var game = new Game(existingMatch, existingMatch.TeamAId, existingMatch.TeamBId);
                await _gameRepository.Add(game);
                await _gameRepository.Save();
            }
        }

        public async Task<Game> GetGameAsync(long gameId)
        {
            var game = await _gameRepository.Get(gameId) ?? throw new Exception("Game not found");

            return game;
        }

        public async Task<List<Game>> GetAllGamesByMatch(long matchId)
        {
            var games = await _gameRepository.GetAllByFK("MatchId", matchId) ?? throw new Exception($"Games for {matchId} was not found");

            return games.ToList();
        }

        public async Task SetGameResult(long gameId, int? TeamAScore, int? TeamBScore, TimeSpan? duration, long winnerId)
        {
            var existingGame = await _gameRepository.Get(gameId) ?? throw new Exception("Game not found");
            var teamAId = existingGame.TeamAId;
            var teamBId = existingGame.TeamBId;

            try
            {
                if(TeamAScore.HasValue || TeamBScore.HasValue || duration is not null)
                {
                    existingGame.TeamAScore = TeamAScore;
                    existingGame.TeamBScore = TeamBScore;
                    existingGame.Duration = duration;
                }

                existingGame.WinnerId = winnerId == teamAId ? teamAId : teamBId;

                await _gameRepository.Update(existingGame);
                await _gameRepository.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error setting game result: {0}, Inner Exception: {1}", ex, ex.InnerException);

                throw new Exception(ex.Message);
            }
        }

        public async Task<long?> DetermineMatchWinner(long matchId)
        {
            var match = await _matchService.GetMatchAsync(matchId) ?? throw new Exception("Match not found");

            var gamesToWin = match.BestOf switch
            {
                BestOf.Bo1 => 1,
                BestOf.Bo3 => 2,
                BestOf.Bo5 => 3,
                _ => 1
            };

            try
            {
                var games = await _gameRepository.GetAllByFK("matchId", matchId);

                return games.GroupBy(g => g.WinnerId).Where(g => g.Count() >= gamesToWin).FirstOrDefault().Key ?? null;

            }
            catch (Exception ex)
            {
                _logger.LogError("Error determining match winner: {0}, Inner Exception: {1}", ex, ex.InnerException);

                throw new Exception(ex.Message);
            }
        }
    }
}
