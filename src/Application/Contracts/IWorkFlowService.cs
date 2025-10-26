using Application.DTOs.Game;
using Application.DTOs.Orchestration;

namespace Application.Contracts
{
    public interface IWorkFlowService
    {
        Task<GameProcessResultDTO> ProcessGameResult(SetGameResultDTO result);
    }
}
