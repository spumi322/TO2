using Application.DTOs.Standing;
using Application.DTOs.Team;
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
        Task<Match?> GetMatchAsync(long id);
        Task<List<Match>> GetMatchesAsync(long standingId);
        Task<List<Team>> GetTeamsAsync(long standingId);
        Task<long> GenerateMatch(Team teamA, Team teamB, int round, int seed, long standingId);
        Task<SeedGroupsResponseDTO> SeedGroups(long tournamentId);
        Task SeedBracket(long tournamentId, List<Team> teams);
        Task IsStandingFinished(long standingId);
    }
}
