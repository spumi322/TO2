using Application.Contracts.Repositories;
using Domain.AggregateRoots;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repository
{
    public class MatchRepository : Repository<Match>, IMatchRepository
    {
        public MatchRepository(TO2DbContext dbContext) : base(dbContext) { }
        public async Task<IReadOnlyList<Match>> GetByStandingIdWithGamesAsync(long standingId)
        {
            return await _dbSet
                .Where(m => m.StandingId == standingId)
                .Include(m => m.Games)  // Eager load - eliminates N+1
                .OrderBy(m => m.Round)
                .ThenBy(m => m.Seed)
                .ToListAsync();
        }
    }
}
