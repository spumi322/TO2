using Application.DTOs.Team;

namespace Application.Contracts
{
    public interface ITeamService
    {
        Task RemoveTeamFromTournamentAsync(long teamId, long tournamentId);
        Task<UpdateTeamResponseDTO> UpdateTeamAsync(long teamId, UpdateTeamRequestDTO request);
        Task DeleteTeamAsync(long teamId);
    }
}
