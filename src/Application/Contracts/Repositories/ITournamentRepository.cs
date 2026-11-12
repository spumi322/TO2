using Domain.AggregateRoots;

namespace Application.Contracts.Repositories
{
    public interface ITournamentRepository : IRepository<Tournament>
    {
        Task<IReadOnlyList<TournamentListRow>> GetListAsync();
    }

    // WinnerName: the only team with final placement 1, null when there is none or several (GroupsOnly with several groups)
    // Groups/RoundsFinished: finished group standings and bracket rounds (BYE matches count as played)
    public record TournamentListRow(Tournament Tournament, int TeamCount, string? WinnerName,
        int GroupsFinished, int GroupsTotal, int RoundsFinished, int RoundsTotal);
}
