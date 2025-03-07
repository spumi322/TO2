﻿using Application.DTOs.Team;
using Domain.AggregateRoots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts
{
    public interface ITeamService
    {
        Task<CreateTeamResponseDTO> CreateTeamAsync(CreateTeamRequestDTO request);
        Task<AddTeamToTournamentResponseDTO> AddTeamToTournamentAsync(AddTeamToTournamentRequestDTO request);
        Task<List<GetAllTeamsResponseDTO>> GetAllTeamsAsync();
        Task<List<GetTeamWithStatsResponseDTO>> GetTeamsWithStatsAsync(long standingId);
        Task<GetTeamResponseDTO> GetTeamAsync(long teamId);
        Task<UpdateTeamResponseDTO> UpdateTeamAsync(long teamId, UpdateTeamRequestDTO request);
        Task DeleteTeamAsync(long teamId);
    }
}
