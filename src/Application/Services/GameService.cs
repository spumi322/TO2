
using Application.Contracts;
using Application.DTOs.Game;
using Application.DTOs.Match;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class GameService : IGameService
    {
        private readonly IRepository<Game> _gameRepository;
        private readonly IRepository<Match> _matchRepository;
        private readonly ILogger<GameService> _logger;

        public GameService(IRepository<Game> gameRepository,
                           IRepository<Match> matchRepository,
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

            var gamesAlreadyGenerated = await _gameRepository.FindAllAsync(g => g.MatchId == match.Id);

            if (gamesAlreadyGenerated.Count > 0)
            {
                _logger.LogWarning("Attempted to generate games for match {MatchId}, but games already exist.", match.Id);
                throw new ConflictException("Games already generated for this match");
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

            await _gameRepository.AddRangeAsync(games);

            return new GenerateGamesDTO(true, $"{games.Count} games generated for match {match.Id}");
        }

        public async Task<Game> GetGameAsync(long gameId)
        {
            var game = await _gameRepository.GetByIdAsync(gameId)
                ?? throw new NotFoundException("Game", gameId);

            return game;
        }

        public async Task<List<Game>> GetAllGamesByMatch(long matchId)
        {
            var games = await _gameRepository.FindAllAsync(g => g.MatchId == matchId);

            return games.ToList();
        }

        public async Task SetGameResult(long gameId, long winnerId, int? teamAScore, int? teamBScore)
        {
            var existingGame = await _gameRepository.GetByIdAsync(gameId)
                ?? throw new NotFoundException("Game", gameId);
            var match = await _matchRepository.GetByIdAsync(existingGame.MatchId)
                ?? throw new NotFoundException("Match", existingGame.MatchId);

            if (match.WinnerId.HasValue)
                throw new ConflictException("Match already has a winner");

            var teamAId = existingGame.TeamAId;
            var teamBId = existingGame.TeamBId;


            if (teamAScore >= 0 || teamBScore >= 0)
            {
                existingGame.TeamAScore = teamAScore;
                existingGame.TeamBScore = teamBScore;
            }

            existingGame.WinnerId = winnerId;

            await _gameRepository.UpdateAsync(existingGame);
        }

        public async Task<MatchWinner?> SetMatchWinner(long matchId)
        {
            var match = await _matchRepository.GetByIdAsync(matchId)
                ?? throw new NotFoundException("Match", matchId);
            var games = await _gameRepository.FindAllAsync(g => g.MatchId == matchId);

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
            if (loserId is null)
                throw new ValidationException("Missing team Id!");

            match.WinnerId = winnerId;
            match.LoserId = loserId;

            await _matchRepository.UpdateAsync(match);

            return new MatchWinner(winnerId.Value, loserId.Value);
        }

        public async Task UpdateGamesTeamIds(long matchId, long? teamAId, long? teamBId)
        {
            var games = await _gameRepository.FindAllAsync(g => g.MatchId == matchId);

            foreach (var game in games)
            {
                game.TeamAId = teamAId;
                game.TeamBId = teamBId;
                await _gameRepository.UpdateAsync(game);
            }

            _logger.LogInformation($"Updated {games.Count()} games for match {matchId} with TeamA={teamAId}, TeamB={teamBId}");
        }
    }
}
