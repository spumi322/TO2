using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Repositories
{
    public interface IStandingRepository : IRepository<Standing>
    {
        Task<IReadOnlyList<Standing>> GetGroupsWithMatchesAsync(long tournamentId);
    }
}

