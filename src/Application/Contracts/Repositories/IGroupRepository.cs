using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Repositories
{
    public interface IGroupRepository : IRepository<Group>
    {
        Task<IReadOnlyList<Group>> GetByStandingIdAsync(long standingId);
        Task<IReadOnlyList<Group>> GetByTournamentIdAsync(long tournamentId);
        Task<Group?> GetByTeamAndTournamentAsync(long teamId, long tournamentId);
    }
}
