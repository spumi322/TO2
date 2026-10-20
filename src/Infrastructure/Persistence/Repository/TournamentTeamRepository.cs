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

        public async Task<IReadOnlyList<string>> GetTeamNamesAsync(long tournamentId)
            => await _dbSet
                .Where(tt => tt.TournamentId == tournamentId)
                .Select(tt => tt.Team.Name)
                .ToListAsync();

        public async Task<int> GetCountByTournamentAsync(long tournamentId)
            => await _dbSet.CountAsync(tt => tt.TournamentId == tournamentId);

        public async Task<IReadOnlyList<TournamentTeam>> GetByTournamentIdAsync(long tournamentId)
            => await _dbSet.Where(tt => tt.TournamentId == tournamentId).ToListAsync();
    }
}
