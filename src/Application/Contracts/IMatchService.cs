using Application.DTOs.Match;
using Application.DTOs.Standing;
using Application.DTOs.Team;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Application.Services.MatchService;

namespace Application.Contracts
{
    public interface IMatchService
    {
        Task<List<Match>> GetMatchesAsync(long standingId);
        Task<Match> GenerateMatch(Team teamA, Team teamB, int round, int seed, long standingId);
    }
}
