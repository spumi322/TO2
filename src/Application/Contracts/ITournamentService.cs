using Application.DTOs.Team;
using Application.DTOs.Tournament;

namespace Application.Contracts
{
    public interface ITournamentService
    {
        Task<CreateTournamentResponseDTO> CreateTournamentAsync(CreateTournamentRequestDTO request);
        Task<GetTournamentResponseDTO> GetTournamentAsync(long id);
        Task<List<GetTournamentListResponseDTO>> GetTournamentListAsync();
        Task<UpdateTournamentResponseDTO> UpdateTournamentAsync(long id, UpdateTournamentRequestDTO request);
        Task CancelTournamentAsync(long id, CancelTournamentRequestDTO request);
        Task DeleteTournamentAsync(long id);
        Task<List<GetTeamResponseDTO>> GetTeamsByTournamentAsync(long tournamentId);
        Task<TournamentStateDTO> GetTournamentStateAsync(long tournamentId);
    }
}
