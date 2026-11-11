namespace Application.Contracts
{
    public interface IWorkFlowService
    {
        Task StartGroups(long tournamentId);
        Task StartBracket(long tournamentId);
    }
}
