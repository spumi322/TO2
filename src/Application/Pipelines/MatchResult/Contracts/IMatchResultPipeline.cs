using Application.DTOs.Match;
using Application.DTOs.Orchestration;

namespace Application.Pipelines.MatchResult.Contracts
{
    public interface IMatchResultPipeline
    {
        Task<GameProcessResultDTO> ExecuteAsync(SetMatchResultDTO request);
    }
}
