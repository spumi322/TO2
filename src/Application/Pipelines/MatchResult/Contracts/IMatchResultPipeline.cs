using Application.DTOs.Match;

namespace Application.Pipelines.MatchResult.Contracts
{
    public interface IMatchResultPipeline
    {
        Task<MatchResultResponseDTO> ExecuteAsync(SetMatchResultDTO request);
    }
}
