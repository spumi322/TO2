using Domain.AggregateRoots;
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
        Task SetGameScore(long gameId, long? teamAId, long? teamBId, BestOf bestOf, TimeSpan? duration);
    }
}
