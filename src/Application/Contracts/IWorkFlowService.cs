using Application.DTOs.Game;
using Application.DTOs.Orchestration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts
{
    public interface IWorkFlowService
    {
        Task<GameProcessResultDTO> ProcessGameResult(SetGameResultDTO result);
    }
}
