using Application.DTOs.SignalR;

namespace Application.Contracts
{
    /// <summary>
    /// The latest draft score of each match, kept in server memory so a page that joins later (first load, refresh,
    /// reconnect) still sees it. Never saved: lost on restart, removed on Confirm, expires when abandoned.
    /// </summary>
    public interface IMatchDraftStore
    {
        // A 0-0 draft removes the match's draft (the page shows the saved score again)
        void Set(MatchDraftUpdatedEvent draft);
        void Remove(long tournamentId, long matchId);
        IReadOnlyList<MatchDraftUpdatedEvent> GetAll(long tournamentId);
    }
}
