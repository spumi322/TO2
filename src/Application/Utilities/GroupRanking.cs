using Domain.Entities;

namespace Application.Utilities
{
    /// <summary>
    /// The one ranking rule for a group, used by the group table, bracket advancement and final placements.
    /// An ordered chain of criteria: each one only decides between teams that are equal on all criteria before it.
    /// </summary>
    public static class GroupRanking
    {
        public static List<GroupEntry> Rank(IEnumerable<GroupEntry> entries) =>
            entries
                .OrderByDescending(e => e.Wins)                         // 1. most match wins
                .ThenByDescending(e => e.GameWins - e.GameLosses)       // 2. best game difference (a 2-0 win beats a 2-1 win)
                .ThenBy(e => e.TeamId)                                  // 3. fixed fallback, so a full tie always comes out in the same order
                .ToList();
    }
}
