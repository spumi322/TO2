using Application.Contracts.Repositories;
using Domain.AggregateRoots;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repository
{
    public class TournamentRepository : Repository<Tournament>, ITournamentRepository
    {
        public TournamentRepository(TO2DbContext dbContext) : base(dbContext) { }

        public async Task<Tournament?> GetWithStandingsAsync(long id)
            => await _dbSet
                .Include(t => t.Standings)
                .FirstOrDefaultAsync(t => t.Id == id);

        public async Task<Tournament?> GetWithTeamsAsync(long id)
            => await _dbSet
                .Include(t => t.TournamentTeams)
                    .ThenInclude(tt => tt.Team)
                        .ThenInclude(t => t.TournamentParticipations)
                .FirstOrDefaultAsync(t => t.Id == id);

        public async Task<IReadOnlyList<Tournament>> GetActiveAsync()
            => await _dbSet
                .Where(t => t.Status != TournamentStatus.Finished
                         && t.Status != TournamentStatus.Cancelled)
                .ToListAsync();

        public async Task<IReadOnlyList<Tournament>> GetByStatusAsync(TournamentStatus status)
            => await _dbSet
            .Where(t => t.Status == status)
            .ToListAsync();
        public async Task<IReadOnlyList<TournamentListRow>> GetListAsync()
        {
            var rows = await _dbSet
                .AsNoTracking()
                .Select(t => new
                {
                    Tournament = t,
                    TeamCount = t.TournamentTeams.Count(),
                    WinnerName = t.TournamentTeams.Count(tt => tt.FinalPlacement == 1) == 1
                        ? t.TournamentTeams.Where(tt => tt.FinalPlacement == 1).Select(tt => tt.Team.Name).FirstOrDefault()
                        : null
                })
                .ToListAsync();

            return rows.Select(r => new TournamentListRow(r.Tournament, r.TeamCount, r.WinnerName)).ToList();
        }
    }
}
