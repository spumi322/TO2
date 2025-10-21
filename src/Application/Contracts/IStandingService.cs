using Application.DTOs.Standing;
using Domain.AggregateRoots;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts
{
    public interface IStandingService
    {
        Task GenerateStanding(long tournamentId, string name, StandingType type, int? teamsPerStanding);
        Task<List<Standing>> GetStandingsAsync(long tournamentId);
        /// <summary>
        /// Checks if any standings finished and marks them. Returns true if a standing was just marked finished.
        /// </summary>
        Task<bool> CheckAndMarkStandingAsFinished(long standingId);
        /// <summary>
        /// Checks if all groups are finished. Returns true if all groups finished.
        /// </summary>
        Task<bool> CheckAllGroupsAreFinished(long tournamentId);
        Task<List<BracketSeedDTO>> PrepareTeamsForBracket(long tournamentId);
    }
}
