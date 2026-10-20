using Domain.AggregateRoots;

namespace Application.Contracts
{
    public interface IMatchService
    {
        Task<Match> GenerateMatch(Team? teamA, Team? teamB, int round, int seed, long standingId);
    }
}
