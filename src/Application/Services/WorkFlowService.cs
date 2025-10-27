using Application.Contracts;
using Application.DTOs.Game;
using Application.DTOs.Orchestration;
using Application.DTOs.Tournament;
using Application.Pipelines.GameResult.Contracts;
using Application.Pipelines.StartBracket.Contracts;
using Application.Pipelines.StartGroups.Contracts;

namespace Application.Services
{
    public class WorkFlowService : IWorkFlowService
    {
        private readonly IGameResultPipeline _gameResultPipeline;
        private readonly IStartGroupsPipeline _startGroupsPipeline;
        private readonly IStartBracketPipeline _startBracketPipeline;

        public WorkFlowService(
            IGameResultPipeline gameResultPipeline,
            IStartGroupsPipeline startGroupsPipeline,
            IStartBracketPipeline startBracketPipeline)
        {
            _gameResultPipeline = gameResultPipeline;
            _startGroupsPipeline = startGroupsPipeline;
            _startBracketPipeline = startBracketPipeline;
        }

        public async Task<GameProcessResultDTO> ProcessGameResult(SetGameResultDTO gameResult)
        {
            return await _gameResultPipeline.ExecuteAsync(gameResult);
        }

        public async Task<StartGroupsResponseDTO> StartGroups(long tournamentId)
        {
            return await _startGroupsPipeline.ExecuteAsync(tournamentId);
        }

        public async Task<StartBracketResponseDTO> StartBracket(long tournamentId)
        {
            return await _startBracketPipeline.ExecuteAsync(tournamentId);
        }
    }
}
