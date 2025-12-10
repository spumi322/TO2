using Application.Contracts.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repository
{
    public class TournamentTeamRepository : Repository<TournamentTeam>, ITournamentTeamRepository
    {
        public TournamentTeamRepository(TO2DbContext dbContext) : base(dbContext) { }

        public async Task<TournamentTeam?> GetByTeamAndTournamentAsync(long teamId, long tournamentId)
            => await _dbSet.FirstOrDefaultAsync(tt => tt.TeamId == teamId && tt.TournamentId == tournamentId);

        public async Task<bool> ExistsInTournamentAsync(long teamId, long tournamentId)
            => await _dbSet.AnyAsync(tt => tt.TeamId == teamId && tt.TournamentId == tournamentId);

        public async Task<bool> HasTeamWithNameAsync(long tournamentId, string teamName)
        {
            var lowerName = teamName.ToLower();
            return await _dbSet
                .Where(tt => tt.TournamentId == tournamentId)
                .Join(_dbContext.Teams, tt => tt.TeamId, t => t.Id, (tt, t) => t)
                .AnyAsync(t => t.Name.ToLower() == lowerName);
        }

        public async Task<int> GetCountByTournamentAsync(long tournamentId)
            => await _dbSet.CountAsync(tt => tt.TournamentId == tournamentId);

        public async Task<IReadOnlyList<TournamentTeam>> GetByTournamentIdAsync(long tournamentId)
            => await _dbSet.Where(tt => tt.TournamentId == tournamentId).ToListAsync();
    }
}
