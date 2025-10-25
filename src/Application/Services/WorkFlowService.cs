using Application.Contracts;
using Application.DTOs.Game;
using Application.DTOs.Orchestration;
using Application.Pipelines.GameResult.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class WorkFlowService : IWorkFlowService
    {
        private readonly IGameResultPipeline _gameResultPipeline;

        public WorkFlowService(IGameResultPipeline gameResultPipeline)
        {
            _gameResultPipeline = gameResultPipeline;
        }

        public async Task<GameProcessResultDTO> ProcessGameResult(SetGameResultDTO gameResult)
        {
            return await _gameResultPipeline.ExecuteAsync(gameResult);
        }
    }
}
