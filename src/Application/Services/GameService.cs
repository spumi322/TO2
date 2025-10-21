
using Application.Contracts;
using Application.DTOs.Game;
using Application.DTOs.Match;
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
        private readonly IGenericRepository<Standing> _standingrepository;
        private readonly IGenericRepository<Group> _groupRepository;
        private readonly IGenericRepository<Bracket> _bracketRepository;
        private readonly ITO2DbContext _dbContext;
        private readonly ILogger<GameService> _logger;


        public GameService(IGenericRepository<Game> gameRepository,
                           IGenericRepository<Match> matchRepository,
                           IGenericRepository<Standing> standingRepository,
                           IGenericRepository<Group> groupRepository,
                           IGenericRepository<Bracket> bracketRepository,
                           ITO2DbContext dbContext,
                           ILogger<GameService> logger)
        {
            _gameRepository = gameRepository;
            _matchRepository = matchRepository;
            _standingrepository = standingRepository;
            _groupRepository = groupRepository;
            _bracketRepository = bracketRepository;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<GenerateGamesDTO> GenerateGames(long matchId)
        {
            var existingMatch = await _dbContext.Matches.FindAsync(matchId) ?? throw new Exception("Match not found");
            var gamesAlreadyGenerated = await _gameRepository.GetAllByFK("MatchId", matchId);

            if(gamesAlreadyGenerated.Count > 0)
            {
                _logger.LogWarning("Attempted to generate games for match {MatchId}, but games already exist.", matchId);
                throw new Exception("Games already generated for this match");
            }

            var games = new List<Game>();
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
                games.Add(game);
            }

            await _gameRepository.AddRange(games);

            return new GenerateGamesDTO(true, $"{games} games generated for match {matchId}");
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

        //public async Task<MatchResultDTO?> SetGameResult(SetGameResultDTO request)
        //{
        //    var existingGame = await _gameRepository.Get(request.gameId) ?? throw new Exception("Game not found");
        //    var match = await _dbContext.Matches.FindAsync(existingGame.MatchId) ?? throw new Exception("Match not found");

        //    if (match.WinnerId.HasValue)
        //        throw new Exception("Match already has a winner");

        //    var teamAId = existingGame.TeamAId;
        //    var teamBId = existingGame.TeamBId;

        //    try
        //    {
        //        if (request.TeamAScore.HasValue || request.TeamBScore.HasValue)
        //        {
        //            existingGame.TeamAScore = request.TeamAScore;
        //            existingGame.TeamBScore = request.TeamBScore;
        //        }

        //        existingGame.WinnerId = request.WinnerId == teamAId ? teamAId
        //                              : request.WinnerId == teamBId ? teamBId
        //                              : throw new Exception("Invalid winner id");

        //        await _gameRepository.Update(existingGame);
        //        await _gameRepository.Save();

        //        var result = await DetermineMatchWinner(match.Id);

        //        if (result is not null)
        //        {
        //            var standing = await _standingrepository.Get(match.StandingId);
        //            var orchestrationService = _orchestrationServiceFactory();

        //            return await orchestrationService.OnMatchCompleted(
        //                match.Id,
        //                result.WinnerId,
        //                result.LoserId,
        //                standing.TournamentId
        //            );
        //        }

        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error setting game result: {Message}", ex.Message);
        //        throw;
        //    }
        //}

        public async Task SetGameResult(long gameId,long winnerId, int? teamAScore, int? teamBScore)
        {
            var existingGame = await _gameRepository.Get(gameId) ?? throw new Exception("Game not found");
            var match = await _dbContext.Matches.FindAsync(existingGame.MatchId) ?? throw new Exception("Match not found");

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

        //public async Task<MatchResult?> DetermineMatchWinner(long matchId)
        //{
        //    var match = await _dbContext.Matches.FindAsync(matchId) ?? throw new Exception("Match not found");

        //    var gamesToWin = match.BestOf switch
        //    {
        //        BestOf.Bo1 => 1,
        //        BestOf.Bo3 => 2,
        //        BestOf.Bo5 => 3,
        //        _ => 1
        //    };

        //    try
        //    {
        //        var games = await _gameRepository.GetAllByFK("MatchId", matchId);

        //        var winner = games.GroupBy(g => g.WinnerId)
        //                          .Where(g => g.Count() == gamesToWin)
        //                          .Select(g => g.Key)
        //                          .FirstOrDefault();

        //        if (winner.HasValue)
        //        {
        //            var loser = winner.Value == match.TeamAId ? match.TeamBId : match.TeamAId;

        //            match.WinnerId = winner.Value;
        //            match.LoserId = loser;

        //            await _matchRepository.Update(match);
        //            await _matchRepository.Save();

        //            await UpdateStandingAfterMatch(match);

        //            return new MatchResult(winner.Value, loser);
        //        }

        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error determining match winner: {Message}", ex.Message);
        //        throw;
        //    }
        //}

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

            match.WinnerId = winnerId;
            match.LoserId = loserId;

            await _matchRepository.Update(match);
            await _matchRepository.Save();

            return new MatchWinner(winnerId.Value, loserId);
        }

        public async Task<StandingType> UpdateStandingEntries(long standingId, long winnerId, long loserId)
        {
            var standing = await _standingrepository.Get(standingId) ?? throw new Exception("Standing not found");

            switch (standing.StandingType)
            {
                case StandingType.Group:
                    {
                        // Load group entries using repository
                        var groupEntries = await _groupRepository.GetAllByFK("StandingId", standingId);

                        var winner = groupEntries.FirstOrDefault(tp => tp.TeamId == winnerId);
                        var loser = groupEntries.FirstOrDefault(tp => tp.TeamId == loserId);

                        if (winner == null || loser == null)
                            throw new Exception("Teams not found in group standings");

                        winner.Wins += 1;
                        winner.Points += 3;
                        loser.Losses += 1;

                        await _groupRepository.Update(winner);
                        await _groupRepository.Update(loser);
                        await _groupRepository.Save();

                        break;
                    }

                case StandingType.Bracket:
                    {
                        var bracketEntries = await _bracketRepository.GetAllByFK("StandingId", standingId);

                        var winner = bracketEntries.FirstOrDefault(b => b.TeamId == winnerId);
                        var loser = bracketEntries.FirstOrDefault(b => b.TeamId == loserId);

                        if (winner == null || loser == null)
                            throw new Exception("Teams not found in bracket standings");

                        winner.Status = TeamStatus.Advanced;
                        loser.Status = TeamStatus.Eliminated;
                        loser.Eliminated = true;

                        await _bracketRepository.Update(winner);
                        await _bracketRepository.Update(loser);
                        await _bracketRepository.Save();

                        break;
                    }

                default:
                    throw new Exception("Unsupported standing type");
            }

            return standing.StandingType;
        }


        //public async Task UpdateStandingAfterMatch(Match match)
        //{
        //    var standing = await _standingrepository.Get(match.StandingId)
        //        ?? throw new Exception("Standing not found");

        //    if (standing.StandingType == StandingType.Group)
        //    {
        //        // Update Group standings
        //        var teamA = await _dbContext.GroupEntries
        //            .FirstOrDefaultAsync(tp => tp.TeamId == match.TeamAId && tp.StandingId == standing.Id);

        //        var teamB = await _dbContext.GroupEntries
        //            .FirstOrDefaultAsync(tp => tp.TeamId == match.TeamBId && tp.StandingId == standing.Id);

        //        if (teamA == null || teamB == null)
        //            throw new Exception("Teams not found in group standings");

        //        if (match.WinnerId == teamA.TeamId)
        //        {
        //            teamA.Wins += 1;
        //            teamA.Points += 3;
        //            teamB.Losses += 1;
        //        }
        //        else if (match.WinnerId == teamB.TeamId)
        //        {
        //            teamB.Wins += 1;
        //            teamB.Points += 3;
        //            teamA.Losses += 1;
        //        }

        //        await _dbContext.SaveChangesAsync();
        //    }
        //    else if (standing.StandingType == StandingType.Bracket)
        //    {
        //        // Update Bracket standings
        //        var winner = await _dbContext.BracketEntries
        //            .FirstOrDefaultAsync(b => b.TeamId == match.WinnerId && b.StandingId == standing.Id);

        //        var loser = await _dbContext.BracketEntries
        //            .FirstOrDefaultAsync(b => b.TeamId == match.LoserId && b.StandingId == standing.Id);

        //        if (winner != null)
        //        {
        //            winner.Status = TeamStatus.Advanced;
        //        }

        //        if (loser != null)
        //        {
        //            loser.Status = TeamStatus.Eliminated;
        //            loser.Eliminated = true;
        //        }

        //        await _dbContext.SaveChangesAsync();
        //    }
        //}

    }
}
