using Domain.AggregateRoots;
using Microsoft.Extensions.Logging;

namespace Application.Pipelines.StartBracket.Utilities
{
    /// <summary>
    /// Utility class for bracket seeding calculations.
    /// Contains helper methods for single elimination bracket generation.
    /// </summary>
    public static class BracketSeedingUtility
    {
        /// <summary>
        /// Checks if a number is a power of 2.
        /// </summary>
        public static bool IsPowerOfTwo(int n)
        {
            return n > 0 && (n & (n - 1)) == 0;
        }

        /// <summary>
        /// Returns the smallest power of 2 >= n.
        /// </summary>
        public static int NextPowerOfTwo(int n)
        {
            if (n <= 1) return 2;
            int p = 1;
            while (p < n) p <<= 1;
            return p;
        }

        /// <summary>
        /// Pads the team list with nulls (BYEs) to the next power of 2.
        /// </summary>
        public static List<Team?> PadToPowerOfTwo(List<Team> teams)
        {
            int target = NextPowerOfTwo(teams.Count);
            var padded = teams.Cast<Team?>().ToList();
            while (padded.Count < target)
                padded.Add(null);
            return padded;
        }

        /// <summary>
        /// Generates the seeding order for a single elimination bracket (0-based seed indexes).
        /// Consecutive entries form round 1 pairs; each pair is seed i vs seed N+1-i, so higher seeds face lower seeds.
        /// Example for 8 slots (1-based): [1, 8, 3, 6, 2, 7, 4, 5]
        /// </summary>
        public static int[] GenerateSeedingOrder(int teamCount)
        {
            var rounds = (int)Math.Log2(teamCount);
            var order = new int[teamCount];

            order[0] = 0;  // Best team
            order[1] = teamCount - 1;  // Worst team

            int filled = 2;
            for (int round = 1; round < rounds; round++)
            {
                int step = teamCount / (int)Math.Pow(2, round + 1);
                int currentFilled = filled; // Capture value before loop
                for (int i = 0; i < currentFilled; i += 2)
                {
                    order[filled++] = order[i] + step;
                    order[filled++] = order[i + 1] - step;
                }
            }

            return order;
        }

        /// <summary>
        /// Creates single elimination pairs from a ranked list of teams (list index = seed, with BYE support).
        /// Pairs teams in GenerateSeedingOrder order (8 slots: 1 vs 8, 3 vs 6, 2 vs 7, 4 vs 5).
        /// Null entries represent BYE slots; teamA is always the higher seed.
        /// </summary>
        public static List<(Team? teamA, Team? teamB)> CreateSingleEliminationPairs(
            List<Team?> teams,
            ILogger? logger = null)
        {
            var pairs = new List<(Team?, Team?)>();
            int teamCount = teams.Count;

            var seedingOrder = GenerateSeedingOrder(teamCount);

            for (int i = 0; i < seedingOrder.Length; i += 2)
            {
                var teamA = teams[seedingOrder[i]];
                var teamB = teams[seedingOrder[i + 1]];
                pairs.Add((teamA, teamB));

                logger?.LogInformation("Pair created: {TeamA} (rank {RankA}) vs {TeamB} (rank {RankB})",
                    teamA?.Name ?? "BYE", seedingOrder[i] + 1, teamB?.Name ?? "BYE", seedingOrder[i + 1] + 1);
            }

            return pairs;
        }

        /// <summary>
        /// Prevents round 1 matches between teams of the same group.
        /// For each same-group match, swaps its lower-seeded team (teamB) with the lower-seeded team
        /// of another real match, if the swap creates no new same-group match.
        /// Only lower-seeded teams move, so top seeds keep their slots (and their BYEs).
        /// Modifies the list in place; returns the number of same-group matches that could not be separated.
        /// </summary>
        /// <param name="pairs">Round 1 pairs; a null team is a BYE</param>
        /// <param name="groupOf">Group id by team id</param>
        public static int SeparateSameGroupPairs(
            List<(Team? teamA, Team? teamB)> pairs,
            IReadOnlyDictionary<long, long> groupOf)
        {
            bool SameGroup(Team a, Team b) => groupOf[a.Id] == groupOf[b.Id];

            for (int i = 0; i < pairs.Count; i++)
            {
                var (topA, bottomA) = pairs[i];
                if (topA == null || bottomA == null || !SameGroup(topA, bottomA)) continue;

                for (int j = 0; j < pairs.Count; j++)
                {
                    var (topB, bottomB) = pairs[j];
                    if (j == i || topB == null || bottomB == null) continue;

                    if (!SameGroup(topA, bottomB) && !SameGroup(topB, bottomA))
                    {
                        pairs[i] = (topA, bottomB);
                        pairs[j] = (topB, bottomA);
                        break;
                    }
                }
            }

            return pairs.Count(p => p.teamA != null && p.teamB != null && SameGroup(p.teamA, p.teamB));
        }
    }
}
