using Application.DTOs.Team;

namespace Application.Contracts
{
    public interface ITeamService
    {
        Task<CreateTeamResponseDTO> CreateTeamAsync(CreateTeamRequestDTO request);
        Task<AddTeamToTournamentResponseDTO> AddTeamToTournamentAsync(AddTeamToTournamentRequestDTO request);
        Task RemoveTeamFromTournamentAsync(long teamId, long tournamentId);
        Task<List<GetAllTeamsResponseDTO>> GetAllTeamsAsync();
        Task<GetTeamResponseDTO> GetTeamAsync(long teamId);
        Task<UpdateTeamResponseDTO> UpdateTeamAsync(long teamId, UpdateTeamRequestDTO request);
        Task DeleteTeamAsync(long teamId);
    }
}
