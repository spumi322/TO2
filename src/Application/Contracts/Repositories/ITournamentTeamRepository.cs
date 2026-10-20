using Domain.Entities;

namespace Application.Contracts.Repositories
{
    public interface ITournamentTeamRepository : IRepository<TournamentTeam>
    {
        Task<TournamentTeam?> GetByTeamAndTournamentAsync(long teamId, long tournamentId);
        Task<IReadOnlyList<string>> GetTeamNamesAsync(long tournamentId);
        Task<int> GetCountByTournamentAsync(long tournamentId);
        Task<IReadOnlyList<TournamentTeam>> GetByTournamentIdAsync(long tournamentId);
    }
}
