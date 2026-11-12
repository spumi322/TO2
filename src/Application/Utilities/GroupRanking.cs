using Domain.AggregateRoots;
using Domain.Entities;

namespace Application.Utilities
{
    /// <summary>
    /// The one ranking rule for a group, used by the group table, bracket advancement and final placements.
    /// An ordered chain of criteria: each one only decides between teams that are equal on all criteria before it.
    /// </summary>
    public static class GroupRanking
    {
        /// <param name="matches">The group's matches, for the head-to-head tiebreaker.</param>
        public static List<GroupEntry> Rank(IEnumerable<GroupEntry> entries, IEnumerable<Match> matches)
        {
            var ordered = entries
                .OrderByDescending(e => e.Wins)                         // 1. most match wins
                .ThenByDescending(e => e.GameWins - e.GameLosses)       // 2. best game difference (a 2-0 win beats a 2-1 win)
                .ThenBy(e => e.TeamId)                                  // 4. fixed fallback (registration order), so a tie always comes out the same
                .ToList();

            // 3. head-to-head: two teams equal on 1 and 2 -> the winner of their match first (3+ tied teams keep the fallback)
            var ranked = new List<GroupEntry>();
            foreach (var tie in ordered.GroupBy(e => (e.Wins, e.GameWins - e.GameLosses)))
            {
                var teams = tie.ToList();
                if (teams.Count == 2 && HeadToHeadWinner(teams[0].TeamId, teams[1].TeamId, matches) == teams[1].TeamId)
                    teams.Reverse();
                ranked.AddRange(teams);
            }
            return ranked;
        }

        private static long? HeadToHeadWinner(long teamA, long teamB, IEnumerable<Match> matches) =>
            matches.FirstOrDefault(m =>
                (m.TeamAId == teamA && m.TeamBId == teamB) || (m.TeamAId == teamB && m.TeamBId == teamA))?.WinnerId;
    }
}
