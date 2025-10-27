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
        /// Required for single elimination brackets.
        /// </summary>
        public static bool IsPowerOfTwo(int n)
        {
            return n > 0 && (n & (n - 1)) == 0;
        }

        /// <summary>
        /// Generates the seeding order for a single elimination bracket.
        /// Creates bracket where higher seeds face lower seeds.
        /// Example for 8 teams: [1, 8, 4, 5, 2, 7, 3, 6]
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
        /// Creates single elimination pairs from a ranked list of teams.
        /// Pairs teams according to standard seeding (1 vs 8, 4 vs 5, etc.)
        /// </summary>
        public static List<(Team teamA, Team teamB)> CreateSingleEliminationPairs(
            List<Team> teams,
            ILogger? logger = null)
        {
            var pairs = new List<(Team, Team)>();
            int teamCount = teams.Count;

            // Get seeding order (ensures #1 vs #8, #4 vs #5, etc.)
            var seedingOrder = GenerateSeedingOrder(teamCount);

            for (int i = 0; i < seedingOrder.Length; i += 2)
            {
                var teamA = teams[seedingOrder[i]];
                var teamB = teams[seedingOrder[i + 1]];
                pairs.Add((teamA, teamB));

                logger?.LogInformation("Pair created: {TeamA} (rank {RankA}) vs {TeamB} (rank {RankB})",
                    teamA.Name, seedingOrder[i] + 1, teamB.Name, seedingOrder[i + 1] + 1);
            }

            return pairs;
        }
    }
}
