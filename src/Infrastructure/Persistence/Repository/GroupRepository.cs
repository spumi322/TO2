using Application.Contracts.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repository
{
    public class GroupRepository : Repository<Group>, IGroupRepository
    {
        public GroupRepository(TO2DbContext dbContext) : base(dbContext) { }

        public async Task<IReadOnlyList<Group>> GetByStandingIdAsync(long standingId)
            => await _dbSet.Where(g => g.StandingId == standingId).ToListAsync();

        public async Task<IReadOnlyList<Group>> GetByTournamentIdAsync(long tournamentId)
            => await _dbSet.Where(g => g.TournamentId == tournamentId).ToListAsync();

        public async Task<Group?> GetByTeamAndTournamentAsync(long teamId, long tournamentId)
            => await _dbSet.FirstOrDefaultAsync(g => g.TeamId == teamId && g.TournamentId == tournamentId);
    }
}
