using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Repositories
{
    public interface ITournamentTeamRepository : IRepository<TournamentTeam>
    {
        Task<TournamentTeam?> GetByTeamAndTournamentAsync(long teamId, long tournamentId);
        Task<bool> ExistsInTournamentAsync(long teamId, long tournamentId);
        Task<bool> HasTeamWithNameAsync(long tournamentId, string teamName);
        Task<int> GetCountByTournamentAsync(long tournamentId);
        Task<IReadOnlyList<TournamentTeam>> GetByTournamentIdAsync(long tournamentId);
    }
}
