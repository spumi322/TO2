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
                        : null,
                    GroupsTotal = t.Standings.Count(s => s.StandingType == StandingType.Group),
                    GroupsFinished = t.Standings.Count(s => s.StandingType == StandingType.Group && s.IsFinished),
                    // All bracket rounds are created when the bracket starts; a round is open while a match has no winner
                    RoundsTotal = t.Standings.Where(s => s.StandingType == StandingType.Bracket)
                        .SelectMany(s => s.Matches).Max(m => m.Round) ?? 0,
                    RoundsOpen = t.Standings.Where(s => s.StandingType == StandingType.Bracket)
                        .SelectMany(s => s.Matches).Where(m => m.WinnerId == null).Select(m => m.Round).Distinct().Count()
                })
                .ToListAsync();

            return rows
                .Select(r => new TournamentListRow(r.Tournament, r.TeamCount, r.WinnerName,
                    r.GroupsFinished, r.GroupsTotal, r.RoundsTotal - r.RoundsOpen, r.RoundsTotal))
                .ToList();
        }
    }
}
