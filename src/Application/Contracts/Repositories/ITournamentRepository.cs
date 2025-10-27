﻿using Domain.AggregateRoots;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Repositories
{
    public interface ITournamentRepository : IRepository<Tournament>
    {
        Task<Tournament?> GetByNameAsync(string name);
        Task<Tournament?> GetWithStandingsAsync(long id);
        Task<Tournament?> GetWithTeamsAsync(long id);
        Task<IReadOnlyList<Tournament>> GetActiveAsync();
        Task<IReadOnlyList<Tournament>> GetByStatusAsync(TournamentStatus status);
    }
}
