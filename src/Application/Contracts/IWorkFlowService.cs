using Application.DTOs.Game;
using Application.DTOs.Orchestration;
using Application.DTOs.Tournament;

namespace Application.Contracts
{
    public interface IWorkFlowService
    {
        Task<GameProcessResultDTO> ProcessGameResult(SetGameResultDTO result);
        Task<StartGroupsResponseDTO> StartGroups(long tournamentId);
    }
}
