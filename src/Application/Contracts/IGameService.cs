using Domain.AggregateRoots;
using Domain.Entities;

namespace Application.Contracts
{
    public interface IGameService
    {
        Task GenerateGames(Match match);
    }
}



