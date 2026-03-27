using Application.DTOs.Game;
using Application.DTOs.Match;
using Domain.AggregateRoots;
using Domain.Entities;

namespace Application.Contracts
{
    public interface IGameService
    {
        Task<GenerateGamesDTO> GenerateGames(Match match);
        Task<List<Game>> GetAllGamesByMatch(long matchId);
        Task SetGameResult(long gameId, long winnerId, int? teamAScore, int? teamBScore);
        Task<MatchWinner?> SetMatchWinner(long matchId);
        // TODO: cleanup — no longer called after lazy game generation refactor
        Task UpdateGamesTeamIds(long matchId, long? teamAId, long? teamBId);
    }
}



