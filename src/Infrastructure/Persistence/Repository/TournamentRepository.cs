using Application.Contracts.Repositories;
using Domain.AggregateRoots;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repository
{
    public class TournamentRepository : Repository<Tournament>, ITournamentRepository
    {
        public TournamentRepository(TO2DbContext dbContext) : base(dbContext) { }

        public async Task<Tournament?> GetByNameAsync(string name)
            => await _dbSet.FirstOrDefaultAsync(t => t.Name == name);

        public async Task<Tournament?> GetWithStandingsAsync(long id)
            => await _dbSet
                .Include(t => t.Standings)
                .FirstOrDefaultAsync(t => t.Id == id);

        public async Task<Tournament?> GetWithTeamsAsync(long id)
            => await _dbSet
                .Include(t => t.TournamentTeams)
                    .ThenInclude(tt => tt.Team)
                .FirstOrDefaultAsync(t => t.Id == id);

        public async Task<IReadOnlyList<Tournament>> GetActiveAsync()
            => await _dbSet
                .Where(t => t.Status != TournamentStatus.Finished
                         && t.Status != TournamentStatus.Cancelled)
                .ToListAsync();

        public async Task<IReadOnlyList<Tournament>> GetByStatusAsync(TournamentStatus status)
            => await _dbSet.Where(t => t.Status == status).ToListAsync();
    }
}
