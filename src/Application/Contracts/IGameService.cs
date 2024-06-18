using Application.DTOs.Game;
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
        Task SetGameResult(long gameId, SetGameResultDTO request);
        Task DetermineMatchWinner(long matchId);
    }
}
