using Application.DTOs.Game;
using Application.DTOs.Orchestration;

namespace Application.Pipelines.GameResult.Contracts
{
    public interface IGameResultPipeline
    {
        Task<GameProcessResultDTO> ExecuteAsync(SetGameResultDTO gameResult);
    }
}
