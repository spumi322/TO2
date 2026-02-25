using Application.Contracts.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repository
{
    public class GroupRepository : Repository<GroupEntry>, IGroupRepository
    {
        public GroupRepository(TO2DbContext dbContext) : base(dbContext) { }

        public async Task<IReadOnlyList<GroupEntry>> GetByStandingIdAsync(long standingId)
            => await _dbSet.Where(g => g.StandingId == standingId).ToListAsync();

        public async Task<IReadOnlyList<GroupEntry>> GetByTournamentIdAsync(long tournamentId)
            => await _dbSet.Where(g => g.TournamentId == tournamentId).ToListAsync();

        public async Task<GroupEntry?> GetByTeamAndTournamentAsync(long teamId, long tournamentId)
            => await _dbSet.FirstOrDefaultAsync(g => g.TeamId == teamId && g.TournamentId == tournamentId);

        public async Task<IReadOnlyList<GroupEntry>> GetByStandingIdOrderedAsync(long standingId)
            => await _dbSet
                .Where(g => g.StandingId == standingId)
                .OrderByDescending(g => g.Points)
                .ThenByDescending(g => g.Wins)
                .ThenBy(g => g.Losses)
                .ToListAsync();
    }
}
