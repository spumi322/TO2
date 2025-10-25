using Application.DTOs.Game;
using Application.DTOs.Orchestration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Pipelines.GameResult.Contracts
{
    public interface IGameResultPipeline
    {
        Task<GameProcessResultDTO> ExecuteAsync(SetGameResultDTO gameResult);
    }
}
