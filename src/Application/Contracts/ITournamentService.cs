using Application.DTOs.Team;
using Application.DTOs.Tournament;
using Domain.AggregateRoots;
using Domain.Enums;
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
        Task SetTournamentStatusAsync(long id, TournamentStatus status);
        Task<List<GetTeamResponseDTO>> GetTeamsByTournamentAsync(long tournamentId);
        //Task<AddTeamToTournamentResponseDTO> AddTeamToTournamentAsync(long teamId, long tournamentId);
        Task RemoveTeamFromTournamentAsync(long teamId, long tournamentId);
        Task<StartTournamentDTO> StartTournament(long tournamentId);
        Task<IsNameUniqueResponseDTO> CheckNameIsUniqueAsync(string name);
    }
}
