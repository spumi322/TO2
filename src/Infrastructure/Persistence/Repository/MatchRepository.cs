using Application.Contracts.Repositories;
using Domain.AggregateRoots;

namespace Infrastructure.Persistence.Repository
{
    public class MatchRepository : Repository<Match>, IMatchRepository
    {
        public MatchRepository(TO2DbContext dbContext) : base(dbContext) { }
    }
}
