using Application.Contracts;
using Application.DTOs.Tournament;
using Application.Pipelines.StartBracket.Contracts;
using Application.Pipelines.StartGroups.Contracts;

namespace Application.Services
{
    public class WorkFlowService : IWorkFlowService
    {
        private readonly IStartGroupsPipeline _startGroupsPipeline;
        private readonly IStartBracketPipeline _startBracketPipeline;

        public WorkFlowService(
            IStartGroupsPipeline startGroupsPipeline,
            IStartBracketPipeline startBracketPipeline)
        {
            _startGroupsPipeline = startGroupsPipeline;
            _startBracketPipeline = startBracketPipeline;
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
