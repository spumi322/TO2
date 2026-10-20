using Application.DTOs.Game;
using Application.DTOs.Match;
using Domain.AggregateRoots;
using Domain.Entities;

namespace Application.Contracts
{
    public interface IGameService
    {
        Task<GenerateGamesDTO> GenerateGames(Match match);
        Task SetGameResult(long gameId, long winnerId, int? teamAScore, int? teamBScore);
        Task<MatchWinner?> SetMatchWinner(long matchId);
        // TODO: remove — not called anywhere
        Task UpdateGamesTeamIds(long matchId, long? teamAId, long? teamBId);
    }
}



