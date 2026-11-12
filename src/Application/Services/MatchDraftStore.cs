using System.Collections.Concurrent;
using Application.Contracts;
using Application.DTOs.SignalR;

namespace Application.Services
{
    /// <summary>
    /// In-memory draft store (one instance per server, registered as a singleton). Fine for a single API instance;
    /// several instances would need a shared store.
    /// </summary>
    public class MatchDraftStore : IMatchDraftStore
    {
        // Drafts of pages that were closed without Confirm
        public static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(30);

        private readonly TimeProvider _time;
        private readonly ConcurrentDictionary<(long TournamentId, long MatchId), (MatchDraftUpdatedEvent Draft, DateTimeOffset At)> _drafts = new();

        public MatchDraftStore(TimeProvider time)
        {
            _time = time;
        }

        public void Set(MatchDraftUpdatedEvent draft)
        {
            var key = (draft.TournamentId, draft.MatchId);
            if (draft.TeamAWins == 0 && draft.TeamBWins == 0)
                _drafts.TryRemove(key, out _);
            else
                _drafts[key] = (draft, _time.GetUtcNow());

            RemoveExpired();
        }

        public void Remove(long tournamentId, long matchId) => _drafts.TryRemove((tournamentId, matchId), out _);

        public IReadOnlyList<MatchDraftUpdatedEvent> GetAll(long tournamentId)
        {
            RemoveExpired();
            return _drafts.Where(entry => entry.Key.TournamentId == tournamentId).Select(entry => entry.Value.Draft).ToList();
        }

        private void RemoveExpired()
        {
            var oldest = _time.GetUtcNow() - MaxAge;
            foreach (var entry in _drafts.Where(entry => entry.Value.At < oldest))
                _drafts.TryRemove(entry.Key, out _);
        }
    }
}
