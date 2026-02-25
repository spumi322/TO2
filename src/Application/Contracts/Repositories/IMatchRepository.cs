using Domain.AggregateRoots;

namespace Application.Contracts.Repositories
{
    public interface IMatchRepository : IRepository<Match>
    {
        Task<IReadOnlyList<Match>> GetByStandingIdWithGamesAsync(long standingId);
    }
}
