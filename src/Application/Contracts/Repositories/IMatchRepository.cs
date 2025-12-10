using Domain.AggregateRoots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Repositories
{
    public interface IMatchRepository : IRepository<Match>
    {
        Task<IReadOnlyList<Match>> GetByStandingIdWithGamesAsync(long standingId);
    }
}
