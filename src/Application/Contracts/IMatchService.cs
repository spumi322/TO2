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
        public Task GenerateMatch(Team teamA, Team teamB, int round, int seed, long standingId);
        public Task SeedGroups(long tournamentId);
    }
}
