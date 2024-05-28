using Domain.AggregateRoots;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts
{
    public interface IMatchService
    {
        public Task<Match> GetMatchAsync(long id);
        public Task<List<Match>> GetMatchesAsync(long standingId);
        public Task<long> GenerateMatch(Team teamA, Team teamB, int round, int seed, long standingId);
        public Task SeedGroups(long tournamentId);
        public Task SeedBracket(long tournamentId, List<Team> teams);
    }
}
