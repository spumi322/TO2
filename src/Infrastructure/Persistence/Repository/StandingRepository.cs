using Application.Contracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repository
{
    public class StandingRepository : Repository<Standing>, IStandingRepository
    {
        public StandingRepository(TO2DbContext dbContext) : base(dbContext) { }

        public async Task<Standing?> GetBracketWithMatchesAsync(long tournamentId)
        {
            return await _dbSet
                .Where(s => s.TournamentId == tournamentId && s.StandingType == StandingType.Bracket)
                .Include(s => s.Matches)
                .ThenInclude(m => m.Games)  // Nested eager loading
                .FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<Standing>> GetGroupsWithMatchesAsync(long tournamentId)
        {
            return await _dbSet
                .Where(s => s.TournamentId == tournamentId && s.StandingType == StandingType.Group)
                .Include(s => s.Matches)
                .ToListAsync();
        }
    }
}
