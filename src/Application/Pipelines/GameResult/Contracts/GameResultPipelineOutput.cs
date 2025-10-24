using Application.DTOs.Orchestration;

namespace Application.Pipelines.GameResult.Contracts
{
    /// <summary>
    /// Output wrapper for the game result pipeline.
    /// Contains the final GameProcessResultDTO to be returned to the caller.
    /// </summary>
    public class GameResultPipelineOutput
    {
        public GameProcessResultDTO Result { get; set; } = null!;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
