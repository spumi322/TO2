
using Application.Contracts;
using Application.DTOs.Game;
using Application.DTOs.Match;
using Application.DTOs.Orchestration;
using Application.Pipelines.GameResult;
using Application.Pipelines.GameResult.Contracts;
using AutoMapper;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class GameService : IGameService
    {
        private readonly IGenericRepository<Game> _gameRepository;
        private readonly IGenericRepository<Match> _matchRepository;
        private readonly ILogger<GameService> _logger;


        public GameService(IGenericRepository<Game> gameRepository,
                           IGenericRepository<Match> matchRepository,
                           ILogger<GameService> logger)
        {
            _gameRepository = gameRepository;
            _matchRepository = matchRepository;
            _logger = logger;
        }

        public async Task<GenerateGamesDTO> GenerateGames(Match match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            var gamesAlreadyGenerated = await _gameRepository.GetAllByFK("MatchId", match.Id);

            if(gamesAlreadyGenerated.Count > 0)
            {
                _logger.LogWarning("Attempted to generate games for match {MatchId}, but games already exist.", match.Id);
                throw new Exception("Games already generated for this match");
            }

            var games = new List<Game>();
            var matchesToPlay = match.BestOf switch
            {
                BestOf.Bo1 => 1,
                BestOf.Bo3 => 3,
                BestOf.Bo5 => 5,
                _ => 1
            };

            for (int i = 0; i < matchesToPlay; i++)
            {
                var game = new Game(match, match.TeamAId, match.TeamBId);
                games.Add(game);
            }

            await _gameRepository.AddRange(games);
            await _gameRepository.Save();

            return new GenerateGamesDTO(true, $"{games.Count} games generated for match {match.Id}");
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

        public async Task SetGameResult(long gameId,long winnerId, int? teamAScore, int? teamBScore)
        {
            var existingGame = await _gameRepository.Get(gameId) ?? throw new Exception("Game not found");
            var match = await _matchRepository.Get(existingGame.MatchId) ?? throw new Exception("Match not found");

            if (match.WinnerId.HasValue)
                throw new Exception("Match already has a winner");

            var teamAId = existingGame.TeamAId;
            var teamBId = existingGame.TeamBId;


            if (teamAScore >= 0 || teamBScore >= 0)
            {
                existingGame.TeamAScore = teamAScore;
                existingGame.TeamBScore = teamBScore;
            }

            existingGame.WinnerId = winnerId;

            await _gameRepository.Update(existingGame);
            await _gameRepository.Save();
        }

        public async Task<MatchWinner?> SetMatchWinner(long matchId)
        {
            var match = await _matchRepository.Get(matchId) ?? throw new Exception("Match not found");
            var games = await _gameRepository.GetAllByFK("MatchId", matchId);

            var gamesToWin = match.BestOf switch
            {
                BestOf.Bo1 => 1,
                BestOf.Bo3 => 2,
                BestOf.Bo5 => 3,
                _ => 1
            };

            var winnerId = games
                .Where(g => g.WinnerId.HasValue)
                .GroupBy(g => g.WinnerId.Value)
                .Select(g => new { TeamId = g.Key, Wins = g.Count() })
                .FirstOrDefault(g => g.Wins >= gamesToWin)
                ?.TeamId;

            if (winnerId == null)
                return null;

            var loserId = winnerId == match.TeamAId ? match.TeamBId : match.TeamAId;
            if (loserId is null) throw new Exception("Missing team Id!");

            match.WinnerId = winnerId;
            match.LoserId = loserId;

            await _matchRepository.Update(match);
            await _matchRepository.Save();

            return new MatchWinner(winnerId.Value, loserId.Value);
        }

        public async Task UpdateGamesTeamIds(long matchId, long? teamAId, long? teamBId)
        {
            var games = await _gameRepository.GetAllByFK("MatchId", matchId);

            foreach (var game in games)
            {
                game.TeamAId = teamAId;
                game.TeamBId = teamBId;
                await _gameRepository.Update(game);
            }

            await _gameRepository.Save();

            _logger.LogInformation($"Updated {games.Count()} games for match {matchId} with TeamA={teamAId}, TeamB={teamBId}");
        }

    }
}
