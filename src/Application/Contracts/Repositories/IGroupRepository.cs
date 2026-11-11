using Domain.Entities;

namespace Application.Contracts.Repositories
{
    public interface IGroupRepository : IRepository<GroupEntry>
    {
        Task<IReadOnlyList<GroupEntry>> GetByStandingIdAsync(long standingId);
        Task<IReadOnlyList<GroupEntry>> GetByTournamentIdAsync(long tournamentId);
    }
}
