using Application.DTOs.Team;
using Application.DTOs.Tournament;
using Domain.Enums;

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
        Task<StartTournamentDTO> StartTournament(long tournamentId);
        Task<TournamentStateDTO> GetTournamentStateAsync(long tournamentId);
    }
}
