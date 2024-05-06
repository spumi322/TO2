﻿using Application.DTOs.Tournament;
using Domain.AggregateRoots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts
{
    public interface ITournamentService
    {
        Task<CreateTournamentResponseDTO> CreateTournamentAsync(CreateTournamentRequestDTO request);
        Task<GetTournamentResponseDTO> GetTournamentAsync(long id);
        Task<List<GetAllTournamentsResponseDTO>> GetAllTournamentsAsync();
        Task<UpdateTournamentResponseDTO> UpdateTournamentAsync(long id, UpdateTournamentRequestDTO request);
        Task SoftDeleteTournamentAsync(long id);
    }
}
