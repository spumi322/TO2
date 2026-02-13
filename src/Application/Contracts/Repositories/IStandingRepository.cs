using Domain.Entities;

namespace Application.Contracts.Repositories
{
    public interface IStandingRepository : IRepository<Standing>
    {
        Task<IReadOnlyList<Standing>> GetGroupsWithMatchesAsync(long tournamentId);
        Task<Standing?> GetBracketWithMatchesAsync(long tournamentId);
    }
}

