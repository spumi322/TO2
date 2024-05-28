using Application.Contracts;
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

        public GameService(IGenericRepository<Game> gameRepository, IMatchService matchService)
        {
            _gameRepository = gameRepository;
            _matchService = matchService;
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

        public async Task SetGameScore(long gameId, long? teamAId, long? teamBId, BestOf bestOf, TimeSpan? duration)
        {
            var existingGame = await _gameRepository.Get(gameId) ?? throw new Exception("Game not found");
            var matchesToWin = bestOf switch
            {
                BestOf.Bo1 => 1,
                BestOf.Bo3 => 2,
                BestOf.Bo5 => 3,
                _ => 1
            };

            if (teamAId.HasValue)
            {
                existingGame.TeamAScore++;

               if(duration.HasValue)
               {
                    existingGame.Duration = duration;
               }

                if (existingGame.TeamAScore == matchesToWin)
                {
                    existingGame.WinnerId = teamAId;
                }
            }
            else
            if (teamBId.HasValue)
            {
                existingGame.TeamBScore++;

                if (duration.HasValue)
                {
                    existingGame.Duration = duration;
                }

                if (existingGame.TeamBScore == matchesToWin)
                {
                    existingGame.WinnerId = teamBId;
                }
            }

            await _gameRepository.Update(existingGame);
            await _gameRepository.Save();
        }
    }
}
