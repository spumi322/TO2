using Application.Contracts;
using Application.DTOs.Game;
using Application.DTOs.Orchestration;
using Application.DTOs.Tournament;
using Application.Pipelines.GameResult.Contracts;
using Application.Pipelines.StartGroups.Contracts;

namespace Application.Services
{
    public class WorkFlowService : IWorkFlowService
    {
        private readonly IGameResultPipeline _gameResultPipeline;
        private readonly IStartGroupsPipeline _startGroupsPipeline;

        public WorkFlowService(
            IGameResultPipeline gameResultPipeline,
            IStartGroupsPipeline startGroupsPipeline)
        {
            _gameResultPipeline = gameResultPipeline;
            _startGroupsPipeline = startGroupsPipeline;
        }

        public async Task<GameProcessResultDTO> ProcessGameResult(SetGameResultDTO gameResult)
        {
            return await _gameResultPipeline.ExecuteAsync(gameResult);
        }

        public async Task<StartGroupsResponseDTO> StartGroups(long tournamentId)
        {
            return await _startGroupsPipeline.ExecuteAsync(tournamentId);
        }
    }
}
