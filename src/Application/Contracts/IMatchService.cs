using Domain.AggregateRoots;

namespace Application.Contracts
{
    public interface IMatchService
    {
        Task<List<Match>> GetMatchesAsync(long standingId);
        Task<Match> GenerateMatch(Team teamA, Team teamB, int round, int seed, long standingId);
    }
}
