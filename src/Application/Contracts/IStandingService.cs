﻿using Domain.AggregateRoots;
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
        Task CheckAndMarkStandingAsFinishedAsync(long tournamentId);
        Task CheckAndMarkAllGroupsAreFinishedAsync(long standingId);
        //Task<int> TopX(long tournamentId);
    }
}
