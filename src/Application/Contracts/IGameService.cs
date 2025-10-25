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
        Task<GenerateGamesDTO> GenerateGames(Match match);
        Task<Game> GetGameAsync(long gameId);
        Task<List<Game>> GetAllGamesByMatch(long matchId);
        Task SetGameResult(long gameId, long winnerId, int? teamAScore, int? teamBScore);
        //Task<MatchResult?> DetermineMatchWinner(long matchId);
        Task<MatchWinner?> SetMatchWinner(long matchId);
        //Task<StandingType> UpdateStandingEntries(long standingId, long winnerId, long loserId);
        Task UpdateGamesTeamIds(long matchId, long? teamAId, long? teamBId);
    }
}



