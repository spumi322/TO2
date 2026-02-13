using Domain.Entities;

namespace Application.Contracts.Repositories
{
    public interface IGroupRepository : IRepository<Group>
    {
        Task<IReadOnlyList<Group>> GetByStandingIdAsync(long standingId);
        Task<IReadOnlyList<Group>> GetByTournamentIdAsync(long tournamentId);
        Task<Group?> GetByTeamAndTournamentAsync(long teamId, long tournamentId);
        Task<IReadOnlyList<Group>> GetByStandingIdOrderedAsync(long standingId);
    }
}
