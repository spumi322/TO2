using Domain.Entities;

namespace Application.Utilities
{
    /// <summary>
    /// The one ranking rule for a group, used by the group table, bracket advancement and final placements:
    /// 1. points, 2. game difference (a 2-0 win beats a 2-1 win), 3. team id, so equal teams always come out in the same order.
    /// </summary>
    public static class GroupRanking
    {
        public static List<GroupEntry> Rank(IEnumerable<GroupEntry> entries) =>
            entries
                .OrderByDescending(e => e.Points)
                .ThenByDescending(e => e.GameWins - e.GameLosses)
                .ThenBy(e => e.TeamId)
                .ToList();
    }
}
