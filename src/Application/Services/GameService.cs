using Application.Contracts;
using Application.DTOs.Game;
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
        private readonly IGenericRepository<Match> _matchRepository;
        private readonly IMatchService _matchService;
        private readonly ILogger<GameService> _logger;


        public GameService(IGenericRepository<Game> gameRepository,
                           IGenericRepository<Match> matchRepository,
                           IMatchService matchService,
                           ILogger<GameService> logger)
        {
            _gameRepository = gameRepository;
            _matchRepository = matchRepository;
            _matchService = matchService;
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

        public async Task SetGameResult(long gameId, SetGameResultDTO request)
        {
            var existingGame = await _gameRepository.Get(gameId) ?? throw new Exception("Game not found");
            var match = await _matchService.GetMatchAsync(existingGame.MatchId) ?? throw new Exception("Match not found");

            if (match.WinnerId.HasValue)
                throw new Exception("Match already has a winner");

            var teamAId = existingGame.TeamAId;
            var teamBId = existingGame.TeamBId;

            try
            {
                if(request.TeamAScore.HasValue || request.TeamBScore.HasValue)
                {
                    existingGame.TeamAScore = request.TeamAScore;
                    existingGame.TeamBScore = request.TeamBScore;
                }

                existingGame.WinnerId = request.WinnerId == teamAId ? teamAId 
                                      : request.WinnerId == teamBId ? teamBId
                                      : throw new Exception("Invalid winner id");

                await _gameRepository.Update(existingGame);
                await _gameRepository.Save();

                await DetermineMatchWinner(match.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error setting game result: {0}, Inner Exception: {1}", ex, ex.InnerException);

                throw new Exception(ex.Message);
            }
        }

        public async Task DetermineMatchWinner(long matchId)
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
                var games = await _gameRepository.GetAllByFK("MatchId", matchId);

                var winner = games.GroupBy(g => g.WinnerId)
                                  .Where(g => g.Count() == gamesToWin)
                                  .Select(g => g.Key)
                                  .FirstOrDefault();

                if (winner.HasValue)
                {
                    var loser = winner.Value == match.TeamAId ? match.TeamBId : match.TeamAId;

                    match.WinnerId = winner.Value;
                    match.LoserId = loser;
                    
                    await _matchRepository.Update(match);
                    await _matchRepository.Save();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error determining match winner: {0}, Inner Exception: {1}", ex, ex.InnerException);

                throw new Exception(ex.Message);
            }
        }

    }
}
