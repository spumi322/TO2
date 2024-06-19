using Application.DTOs.Game;
using Application.DTOs.Match;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts
{
    public interface IGameService
    {
        Task GenerateGames(long matchId);
        Task<Game> GetGameAsync(long gameId);
        Task<List<Game>> GetAllGamesByMatch(long matchId);
        Task<MatchResultDTO?> SetGameResult(long gameId, SetGameResultDTO request);
        Task<MatchResult?> DetermineMatchWinner(long matchId);
    }
}
