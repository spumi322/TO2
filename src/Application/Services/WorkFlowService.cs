using Application.Contracts;
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

        public Task StartGroups(long tournamentId) => _startGroupsPipeline.ExecuteAsync(tournamentId);

        public Task StartBracket(long tournamentId) => _startBracketPipeline.ExecuteAsync(tournamentId);
    }
}
