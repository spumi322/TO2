using Domain.AggregateRoots;
using Domain.Enums;

namespace Application.Contracts.Repositories
{
    public interface ITournamentRepository : IRepository<Tournament>
    {
        Task<Tournament?> GetWithStandingsAsync(long id);
        Task<Tournament?> GetWithTeamsAsync(long id);
        Task<IReadOnlyList<Tournament>> GetActiveAsync();
        Task<IReadOnlyList<Tournament>> GetByStatusAsync(TournamentStatus status);
        Task<IReadOnlyList<(Tournament Tournament, int TeamCount)>> GetAllWithTeamCountAsync();
    }
}
