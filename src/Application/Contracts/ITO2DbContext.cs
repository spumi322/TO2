using Domain.AggregateRoots;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        DbSet<TournamentParticipants> TeamsTournaments { get; }


        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
