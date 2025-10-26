using Domain.AggregateRoots;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Contracts
{
    public interface ITO2DbContext
    {
        DbSet<Tournament> Tournaments { get; }
        DbSet<Team> Teams { get; }
        DbSet<Match> Matches { get; }
        DbSet<Player> Players { get; }
        DbSet<Standing> Standings { get; }
        DbSet<Game> Games { get; }
        DbSet<Group> GroupEntries { get; }
        DbSet<TournamentTeam> TournamentTeams { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
